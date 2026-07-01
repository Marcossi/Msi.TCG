using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Services.Templates;

internal sealed class TemplatesService(ILogger<TemplatesService> logger) : ITemplatesService
{
    private readonly ILogger<TemplatesService> _logger = logger;
    public async Task<TemplateResult> ProcessTemplateAsync(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            _logger.LogDebug("ProcessTemplateAsync: contenido vacío, retornando éxito");
            return TemplateResult.Success(string.Empty);
        }

        _logger.LogDebug("Procesando plantilla ({CharCount} chars)", templateContent.Length);

        try
        {
            // 1. Parsear y validar la plantilla
            (bool IsSuccess, Template? Template, TemplateResult ErrorResult) parseResult = ParseTemplate(templateContent);
            if (!parseResult.IsSuccess)
            {
                _logger.LogWarning(parseResult.ErrorResult.ErrorMessage, "Errores de sintaxis al parsear plantilla");
                return parseResult.ErrorResult;
            }

            Template template = parseResult.Template!;

            // 2. Obtener el modelo de datos (Dummy por ahora)
            object model = GetDummyModel();

            // 3. Renderizar la plantilla con el modelo
            TemplateResult renderResult = await RenderTemplateAsync(template, model);

            if (renderResult.IsSuccess)
            {
                _logger.LogDebug("Plantilla renderizada exitosamente ({ResultLen} chars)", renderResult.Result?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning(renderResult.ErrorMessage, "Error al renderizar plantilla");
            }

            return renderResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la plantilla");
            // Cualquier otro error inesperado a nivel general
            return TemplateResult.Failure($"Error inesperado al procesar la plantilla:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Parsea el contenido de la plantilla y valida errores de sintaxis.
    /// </summary>
    private (bool IsSuccess, Template? Template, TemplateResult ErrorResult) ParseTemplate(string templateContent)
    {
        _logger.LogDebug("Parseando plantilla ({CharCount} chars)", templateContent.Length);

        // Opciones de parseo personalizadas
        ParserOptions parserOptions = new()
        {
            // Aqu� podremos a�adir opciones en el futuro, por ejemplo:
            // ExpressionDepthLimit = 100,
            // LiquidTagOnly = false
        };

        LexerOptions lexerOptions = new()
        {
            // Opciones del analizador l�xico
            // Mode = ScriptMode.Default
        };

        // Compilamos el string a un AST interno (Abstract Syntax Tree).
        Template template = Template.Parse(templateContent, sourceFilePath: null, parserOptions, lexerOptions);

        // Comprobamos errores de sintaxis
        if (template.HasErrors)
        {
            string errors = string.Join(Environment.NewLine, template.Messages.Select(m => m.ToString()));
            _logger.LogDebug("Errores de sintaxis detectados: {Errors}", errors);
            return (false, null, TemplateResult.Failure($"Errores de sintaxis en la plantilla:\n{errors}"));
        }

        _logger.LogDebug("Plantilla parseada exitosamente");
        return (true, template, null!);
    }

    /// <summary>
    /// Renderiza la plantilla AST utilizando el modelo de datos proporcionado.
    /// </summary>
    private async Task<TemplateResult> RenderTemplateAsync(Template template, object model)
    {
        try
        {
            _logger.LogDebug("Renderizando plantilla con modelo de tipo '{ModelType}'", model.GetType().Name);

            TemplateContext context = new();
            
            // IMPORTANTE 1: Mantener nombres originales (PascalCase en lugar de snake_case)
            context.MemberRenamer = member => member.Name; 
            
            // IMPORTANTE 2: Permitir acceso a miembros en objetos CLR anidados devueltos
            // por m�todos (p. ej. propiedades de DummyElement retornado por GetElementByName).
            context.MemberFilter = member => true;
            
            // Construimos un ScriptObject que expone tanto propiedades como m�todos de
            // instancia del modelo. Scriban solo registra m�todos est�ticos al usar
            // Import(tipo); los m�todos de instancia deben a�adirse como delegados Func<>.
            ScriptObject scriptObject = new();
            scriptObject.Add("Model", BuildScriptObject(model));
            
            context.PushGlobal(scriptObject);

            // Evaluar el AST con el contexto configurado
            string result = await template.RenderAsync(context);

            _logger.LogDebug("Renderizado completado ({ResultLen} chars)", result.Length);
            return TemplateResult.Success(result);
        }
        catch (Scriban.Syntax.ScriptRuntimeException ex)
        {
            _logger.LogWarning(ex, "Error de ejecuci�n en la plantilla");
            // Errores de ejecuci�n (ej. intentar acceder a una propiedad que no existe en el modelo)
            return TemplateResult.Failure($"Error de ejecuci�n en la plantilla:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al renderizar la plantilla");
            // Cualquier otro error inesperado durante el renderizado
            return TemplateResult.Failure($"Error inesperado al renderizar la plantilla:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Construye un ScriptObject a partir de un objeto CLR, exponiendo sus propiedades
    /// de instancia y registrando expl�citamente los m�todos requeridos.
    /// </summary>
    private static ScriptObject BuildScriptObject(object model)
    {
        //return model as ScriptObject;
        ScriptObject scriptObject = new();

        // Importar propiedades de instancia con nombres originales (PascalCase).
        // Import(instancia) en Scriban solo expone propiedades, nunca m�todos.
        scriptObject.Import(model, renamer: m => m.Name);
        scriptObject.Add("this", model);

        // Registramos el m�todo Test que ahora recibe el modelo como par�metro
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
                    Description = "Representa un producto en el cat�logo",
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
}
