using CommunityToolkit.Mvvm.ComponentModel;
using Msi.TemplateCodeGenerator.UI.Views;

namespace Msi.TemplateCodeGenerator.UI.Settings;

internal partial class SettingsShellViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _statusMessage = "Hello World! From: Settings Shell View Model";
}
