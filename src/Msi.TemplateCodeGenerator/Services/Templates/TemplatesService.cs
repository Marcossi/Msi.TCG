using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services.Templates;

public class TemplatesService : ITemplatesService
{
    public async Task<TemplateResult> ProcessTemplateAsync(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            return TemplateResult.Success(string.Empty);
        }

        try
        {
            // 1. Parsear y validar la plantilla
            var parseResult = ParseTemplate(templateContent);
            if (!parseResult.IsSuccess)
                return parseResult.ErrorResult;

            var template = parseResult.Template!;

            // 2. Obtener el modelo de datos (Dummy por ahora)
            var model = GetDummyModel();

            // 3. Renderizar la plantilla con el modelo
            var renderResult = await RenderTemplateAsync(template, model);
            
            return renderResult;
        }
        catch (Exception ex)
        {
            // Cualquier otro error inesperado a nivel general
            return TemplateResult.Failure($"Error inesperado al procesar la plantilla:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Parsea el contenido de la plantilla y valida errores de sintaxis.
    /// </summary>
    private (bool IsSuccess, Template? Template, TemplateResult ErrorResult) ParseTemplate(string templateContent)
    {
        // Opciones de parseo personalizadas
        var parserOptions = new ParserOptions
        {
            // Aquí podremos añadir opciones en el futuro, por ejemplo:
            // ExpressionDepthLimit = 100,
            // LiquidTagOnly = false
        };

        var lexerOptions = new LexerOptions
        {
            // Opciones del analizador léxico
            // Mode = ScriptMode.Default
        };

        // Compilamos el string a un AST interno (Abstract Syntax Tree).
        var template = Template.Parse(templateContent, sourceFilePath: null, parserOptions, lexerOptions);

        // Comprobamos errores de sintaxis
        if (template.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, template.Messages.Select(m => m.ToString()));
            return (false, null, TemplateResult.Failure($"Errores de sintaxis en la plantilla:\n{errors}"));
        }

        return (true, template, null!);
    }

    /// <summary>
    /// Renderiza la plantilla AST utilizando el modelo de datos proporcionado.
    /// </summary>
    private async Task<TemplateResult> RenderTemplateAsync(Template template, object model)
    {
        try
        {
            var context = new Scriban.TemplateContext();
            
            // IMPORTANTE 1: Mantener nombres originales (PascalCase en lugar de snake_case)
            context.MemberRenamer = member => member.Name; 
            
            // IMPORTANTE 2: Permitir acceso a miembros en objetos CLR anidados devueltos
            // por métodos (p. ej. propiedades de DummyElement retornado por GetElementByName).
            context.MemberFilter = member => true;
            
            // Construimos un ScriptObject que expone tanto propiedades como métodos de
            // instancia del modelo. Scriban solo registra métodos estáticos al usar
            // Import(tipo); los métodos de instancia deben añadirse como delegados Func<>.
            var scriptObject = new ScriptObject();
            scriptObject.Add("Model", BuildScriptObject(model));
            
            context.PushGlobal(scriptObject);

            // Evaluar el AST con el contexto configurado
            var result = await template.RenderAsync(context);

            return TemplateResult.Success(result);
        }
        catch (Scriban.Syntax.ScriptRuntimeException ex)
        {
            // Errores de ejecución (ej. intentar acceder a una propiedad que no existe en el modelo)
            return TemplateResult.Failure($"Error de ejecución en la plantilla:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            // Cualquier otro error inesperado durante el renderizado
            return TemplateResult.Failure($"Error inesperado al renderizar la plantilla:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Construye un ScriptObject a partir de un objeto CLR, exponiendo sus propiedades
    /// de instancia y registrando explícitamente los métodos requeridos.
    /// </summary>
    private static ScriptObject BuildScriptObject(object model)
    {
        //return model as ScriptObject;
        var scriptObject = new ScriptObject();

        // Importar propiedades de instancia con nombres originales (PascalCase).
        // Import(instancia) en Scriban solo expone propiedades, nunca métodos.
        scriptObject.Import(model, renamer: m => m.Name);
        scriptObject.Add("this", model);

        // Registramos el método Test que ahora recibe el modelo como parámetro
        scriptObject.Import(nameof(DummyTemplateModel.Test), DummyTemplateModel.Test);

        //scriptObject.Import(typeof(DummyTemplateModel));

        return scriptObject;
    }

    /// <summary>
    /// Genera un modelo de datos de prueba para inyectar en la plantilla.
    /// </summary>
    private object GetDummyModel()
    {
        // Modelo fuertemente tipado de prueba que simula la estructura real
        return new DummyTemplateModel
        {
            ProjectName = "Msi.TemplateCodeGenerator",
            Author = "Developer",
            Elements = new List<DummyElement>
            {
                new DummyElement 
                { 
                    Id = "E001",
                    Name = "User", 
                    BaseClass = "BaseEntity",
                    Description = "Representa un usuario del sistema",
                    Fields = new List<DummyField>
                    {
                        new DummyField { Name = "Id", Type = "int", Accessibility = "public" },
                        new DummyField { Name = "Username", Type = "string", Accessibility = "public" },
                        new DummyField { Name = "PasswordHash", Type = "string", Accessibility = "private" }
                    }
                },
                new DummyElement 
                { 
                    Id = "E002",
                    Name = "Product", 
                    BaseClass = null,
                    Description = "Representa un producto en el catálogo",
                    Fields = new List<DummyField>
                    {
                        new DummyField { Name = "Id", Type = "Guid", Accessibility = "public" },
                        new DummyField { Name = "Title", Type = "string", Accessibility = "public" },
                        new DummyField { Name = "Price", Type = "decimal", Accessibility = "public" }
                    }
                }
            }
        };
    }

    // Clases Dummy para el modelo de datos
    public class DummyTemplateModel
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<DummyElement> Elements { get; set; } = new();

        // Método estático invocable desde Scriban que recibe el modelo
        public static string Test(ScriptObject model)
        {
            var @this = (DummyTemplateModel)model["this"];
            return $"soy el metodo Test() ejecutado sobre el proyecto: {@this.ProjectName}";
        }
    }

    public class DummyElement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? BaseClass { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<DummyField> Fields { get; set; } = new();
    }

    public class DummyField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Accessibility { get; set; } = "private";
    }
}
