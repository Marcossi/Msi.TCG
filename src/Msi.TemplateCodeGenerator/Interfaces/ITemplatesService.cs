using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.Interfaces;

public interface ITemplatesService
{
    Task<TemplateResult> ProcessTemplateAsync(string template);
}
