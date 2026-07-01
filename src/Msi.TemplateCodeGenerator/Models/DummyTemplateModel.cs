using Scriban.Runtime;

namespace Msi.TemplateCodeGenerator.Models;

/// <summary>
/// Modelo de datos de prueba para plantillas Scriban.
/// Simula la estructura real de un proyecto con elementos y campos.
/// </summary>
internal sealed class DummyTemplateModel
{
    public string ProjectName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<DummyElement> Elements { get; set; } = new();

    /// <summary>
    /// M�todo est�tico invocable desde Scriban que recibe el modelo.
    /// </summary>
    public static string Test(ScriptObject model)
    {
        DummyTemplateModel? @this = (DummyTemplateModel?)model["this"];
        return $"soy el metodo Test() ejecutado sobre el proyecto: {@this?.ProjectName}";
    }
}
