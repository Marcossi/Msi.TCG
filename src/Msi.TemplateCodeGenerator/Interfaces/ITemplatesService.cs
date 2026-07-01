using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.Interfaces;

internal interface ITemplatesService
{
    Task<TemplateResult> ProcessTemplateAsync(string template);
}
