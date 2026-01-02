namespace Msi.TemplateCodeGenerator.Services.Templates;

public class TemplateResult
{
    public bool IsSuccess { get; set; }
    public string Result { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public static TemplateResult Success(string result) => new() { IsSuccess = true, Result = result };
    public static TemplateResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
