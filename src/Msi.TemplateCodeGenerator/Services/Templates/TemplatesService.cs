using System.Text.RegularExpressions;
using Msi.TemplateCodeGenerator.Interfaces;

namespace Msi.TemplateCodeGenerator.Services.Templates;

public class TemplatesService : ITemplatesService
{
    public async Task<TemplateResult> ProcessTemplateAsync(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return TemplateResult.Success(string.Empty);
        }

        try
        {
            // Simulamos asincronía (como si fuera una llamada a un servicio pesado)
            return await Task.Run(() =>
            {
                // Lógica dummy: Reemplazar {texto} por TEXTO
                var result = Regex.Replace(template, @"\{([^}]+)\}", match =>
                {
                    // Retornamos el contenido del grupo capturado en mayúsculas
                    return match.Groups[1].Value.ToUpper();
                });

                return TemplateResult.Success(result);
            });
        }
        catch (Exception ex)
        {
            return TemplateResult.Failure(ex.Message);
        }
    }
}
