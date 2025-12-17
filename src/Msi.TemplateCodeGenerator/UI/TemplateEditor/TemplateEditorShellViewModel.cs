using CommunityToolkit.Mvvm.ComponentModel;

namespace Msi.TemplateCodeGenerator.UI.TemplateEditor;

internal partial class TemplateEditorShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Hello World! From: Template Editor ShellViewModel!";
}
