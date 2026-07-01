using Avalonia.Controls;
using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.UI.Services.Dialogs;

/// <summary>
/// Diálogo de confirmación de guardado al intentar cerrar un documento con cambios sin guardar.
/// </summary>
internal partial class SaveConfirmationDialog : Window
{
    public SaveConfirmationResult Result { get; private set; } = SaveConfirmationResult.Cancel;

    public SaveConfirmationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor con parámetro para establecer el mensaje del diálogo.
    /// </summary>
    public SaveConfirmationDialog(string fileName) : this()
    {
        TextBlock? messageText = this.FindControl<TextBlock>("MessageText");
        if (messageText != null)
            messageText.Text = $"¿Deseas guardar los cambios en '{fileName}'?";
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = SaveConfirmationResult.Save;
        Close();
    }

    private void OnDontSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = SaveConfirmationResult.DontSave;
        Close();
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = SaveConfirmationResult.Cancel;
        Close();
    }
}
