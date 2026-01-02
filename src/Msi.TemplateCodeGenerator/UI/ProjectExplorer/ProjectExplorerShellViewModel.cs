using CommunityToolkit.Mvvm.ComponentModel;

namespace Msi.TemplateCodeGenerator.UI.ProjectExplorer;

internal partial class ProjectExplorerShellViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _statusMessage = "Hello World! From: Project Explorer Shell View Model";
}
