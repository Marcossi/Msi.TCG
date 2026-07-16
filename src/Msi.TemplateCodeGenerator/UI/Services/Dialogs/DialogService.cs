using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.Shell;

namespace Msi.TemplateCodeGenerator.UI.Services.Dialogs;

/// <summary>
/// Implementación de IDialogService usando Avalonia.
/// Muestra diálogos de confirmación y otras interacciones con el usuario.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly MainWindow _ownerWindow;
    private readonly ILogger<DialogService> _logger;

    public DialogService(MainWindow ownerWindow, ILogger<DialogService> logger)
    {
        _ownerWindow = ownerWindow;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SaveConfirmationResult> ShowSaveConfirmationAsync(string fileName)
    {
        _logger.LogInformation("[UI] DialogService: Showing save confirmation for '{FileName}'", fileName);
        SaveConfirmationDialog dialog = new(fileName);
        await dialog.ShowDialog(_ownerWindow);
        _logger.LogInformation("[UI] DialogService: Result = {Result}", dialog.Result);
        return dialog.Result;
    }

    /// <inheritdoc/>
    public async Task ShowInfoAsync(string message, string title)
    {
        _logger.LogInformation("[UI] DialogService: Showing info dialog '{Title}': {Message}", title, message);
        await ShowMessageDialogAsync(message, title, MessageKind.Info);
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string message, string title)
    {
        _logger.LogError("[UI] DialogService: Showing error dialog '{Title}': {Message}", title, message);
        await ShowMessageDialogAsync(message, title, MessageKind.Error);
    }

    /// <inheritdoc/>
    public async Task ShowWarningAsync(string message, string title)
    {
        _logger.LogWarning("[UI] DialogService: Showing warning dialog '{Title}': {Message}", title, message);
        await ShowMessageDialogAsync(message, title, MessageKind.Warning);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string message, string title)
    {
        _logger.LogInformation("[UI] DialogService: Showing confirmation dialog '{Title}': {Message}", title, message);

        Window dialog = new()
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        StackPanel panel = new() { Spacing = 20, Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        Button yesButton = new() { Content = "Sí", MinWidth = 80, IsDefault = true };
        yesButton.Click += (_, _) => dialog.Close(true);
        buttonPanel.Children.Add(yesButton);

        Button noButton = new() { Content = "No", MinWidth = 80, IsCancel = true };
        noButton.Click += (_, _) => dialog.Close(false);
        buttonPanel.Children.Add(noButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        bool result = await dialog.ShowDialog<bool>(_ownerWindow);
        _logger.LogInformation("[UI] DialogService: Confirmation result = {Result}", result);
        return result;
    }

    private async Task ShowMessageDialogAsync(string message, string title, MessageKind kind)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        IBrush foreground = kind switch
        {
            MessageKind.Error => Brushes.Red,
            MessageKind.Warning => Brushes.Orange,
            _ => Brushes.Black
        };

        StackPanel panel = new() { Spacing = 20, Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = foreground
        });

        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        Button okButton = new() { Content = "OK", MinWidth = 80, IsDefault = true };
        okButton.Click += (_, _) => dialog.Close();
        buttonPanel.Children.Add(okButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(_ownerWindow);
    }

    private enum MessageKind
    {
        Info,
        Warning,
        Error
    }
}
