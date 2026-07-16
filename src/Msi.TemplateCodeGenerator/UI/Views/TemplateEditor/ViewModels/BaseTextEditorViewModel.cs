using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Shared;

namespace Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

/// <summary>
/// ViewModel base abstracto para editores de texto.
/// Gestiona el path del fichero, el contenido editable y el mensaje de estado.
/// Las clases derivadas especializan el comportamiento al cambiar el contenido.
/// Implementa ICloseAware para controlar el cierre seguro con confirmación si hay cambios pendientes.
/// </summary>
internal abstract partial class BaseTextEditorViewModel(
    IFileSystem fileSystem,
    IDialogService dialogService,
    ILogger<BaseTextEditorViewModel> logger)
    : BaseViewModel, ICloseAware, ICommandRoute
{
    protected readonly ILogger<BaseTextEditorViewModel> _logger = logger;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _filePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    /// <summary>
    /// Nombre del fichero derivado del path para mostrar en la pestaña del editor.
    /// Devuelve "Nueva Plantilla" si no hay fichero asociado.
    /// La vista es responsable de representar visualmente el estado IsDirty (asterisco, icono, negrita...).
    /// </summary>
    public string TabTitle => string.IsNullOrEmpty(FilePath)
        ? "Nueva Plantilla"
        : Path.GetFileName(FilePath);

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Flag interno para suprimir el tracking de cambios durante la carga de un fichero.</summary>
    private bool _isLoading;

    /// <summary>
    /// Método parcial generado por CommunityToolkit.Mvvm que se ejecuta cuando cambia Content.
    /// Marca el fichero como modificado (salvo durante la carga) y delega en OnContentChangedCore.
    /// </summary>
    partial void OnContentChanged(string value)
    {
        if (!_isLoading)
            IsDirty = true;
        OnContentChangedCore(value);
    }

    /// <summary>
    /// Punto de extensión invocado cuando el contenido del editor cambia.
    /// Las clases derivadas deben sobrescribirlo para implementar su lógica específica.
    /// </summary>
    protected virtual void OnContentChangedCore(string value) { }

    /// <summary>
    /// Carga un fichero en el editor: actualiza el path y lee el contenido desde disco.
    /// Resetea IsDirty al finalizar la carga.
    /// </summary>
    public async Task LoadFileAsync(string filePath)
    {
        _logger.LogInformation("[UI] Editor: Cargando '{FilePath}'", filePath);
        _isLoading = true;
        try
        {
            FilePath = filePath;
            Content = await fileSystem.ReadTextAsync(filePath);
            IsDirty = false;
            _logger.LogDebug("Fichero cargado: '{FilePath}' ({ContentLen} chars)", filePath, Content.Length);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Guarda el contenido actual en disco y limpia el estado de cambios pendientes.
    /// Solo ejecutable cuando hay cambios sin guardar y el fichero tiene path asignado.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogInformation("[UI] Editor: Guardando '{FilePath}'", FilePath);
            await fileSystem.WriteTextAsync(FilePath, Content);
            MarkAsSaved();
            _logger.LogInformation("[UI] Editor: Fichero guardado '{FilePath}'", FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar '{FilePath}'", FilePath);
            StatusMessage = $"Error al guardar: {ex.Message}";
        }
    }

    private bool CanSave() => IsDirty && !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Marca el fichero como guardado, limpiando el estado de cambios pendientes.
    /// </summary>
    public void MarkAsSaved() => IsDirty = false;

    /// <inheritdoc/>
    public async Task<bool> CanCloseAsync()
    {
        // Sin cambios pendientes, cierre seguro
        if (!IsDirty)
        {
            _logger.LogDebug("Cierre de editor sin cambios pendientes: '{TabTitle}'", TabTitle);
            return true;
        }

        _logger.LogInformation("[UI] Editor: Confirmación de guardado para '{TabTitle}'", TabTitle);

        // Con cambios pendientes, mostrar diálogo de confirmación
        SaveConfirmationResult result = await dialogService.ShowSaveConfirmationAsync(TabTitle);

        _logger.LogDebug("Resultado confirmación cierre: {Result}", result);

        return result switch
        {
            SaveConfirmationResult.Save => await TrySaveAsync(),
            SaveConfirmationResult.DontSave => true,
            SaveConfirmationResult.Cancel => false,
            _ => false
        };
    }

    /// <summary>
    /// Intenta guardar el fichero. Devuelve true si el guardado fue exitoso.
    /// </summary>
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await SaveCommand.ExecuteAsync(null);
            return !IsDirty;  // true si se limpió IsDirty tras guardar
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en TrySaveAsync para '{FilePath}'", FilePath);
            return false;
        }
    }

    public virtual bool CanExecute(string commandName) => commandName switch
    {
        "Save" => CanSave(),
        _ => false
    };

    public virtual async Task ExecuteAsync(string commandName)
    {
        switch (commandName)
        {
            case "Save":
                await SaveAsync();
                break;
            default:
                throw new InvalidOperationException($"Comando no soportado: {commandName}");
        }
    }
}
