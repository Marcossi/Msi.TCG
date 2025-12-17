using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Msi.TemplateCodeGenerator.UI.Settings;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator.UI;

// Heredamos de ObservableRecipient porque este es el "Shell" u Orquestador.
// Esto le da la capacidad de recibir mensajes (Messenger) de otros ViewModels hijos
// para coordinar la navegación o acciones globales.
internal partial class MainShellViewModel(TemplateEditorShellViewModel templateEditorShellViewModel,
                                          SettingsShellViewModel settingsShellViewModel)
    : ObservableRecipient
{
    [ObservableProperty]
    private object? _currentViewModel = templateEditorShellViewModel;

    protected override void OnActivated()
    {
        // Aquí nos suscribiríamos a mensajes globales si los hubiera.
        // Messenger.Register<MainShellViewModel, NavigationMessage>(this, (r, m) => r.Receive(m));
    }

    [RelayCommand]
    private void NavigateToTemplateEditor()
    {
        CurrentViewModel = templateEditorShellViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentViewModel = settingsShellViewModel;
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
