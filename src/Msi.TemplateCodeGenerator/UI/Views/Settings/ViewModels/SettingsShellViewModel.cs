using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.Settings.ViewModels;

internal partial class SettingsShellViewModel(ILogger<SettingsShellViewModel> logger) : BaseViewModel
{
    private readonly ILogger<SettingsShellViewModel> _logger = logger;

    [ObservableProperty]
    private string _statusMessage = "Hello World! From: Settings Shell View Model";
}
