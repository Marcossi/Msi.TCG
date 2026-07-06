using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Servicio de procesamiento de metadata JSON.
/// Encargado de parsear, cargar defaults y aplicar merge.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Parsea el JSON, carga defaults si existen, aplica merge y devuelve el resultado formateado.
    /// </summary>
    /// <param name="jsonContent">Contenido JSON del editor.</param>
    /// <param name="editorFilePath">Path del fichero del editor (para resolver defaults relativos).</param>
    /// <returns>Resultado del procesamiento con el preview formateado y flag de error.</returns>
    Task<MetadataPreviewResult> ProcessPreviewAsync(string jsonContent, string editorFilePath);
}
