using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Representa un item de menú contextual.
/// </summary>
public sealed class ContextMenuItem
{
    /// <summary>
    /// Texto del menú.
    /// </summary>
    public string Header { get; init; } = string.Empty;

    /// <summary>
    /// Acción a ejecutar al hacer clic.
    /// </summary>
    public Action? Command { get; init; }

    /// <summary>
    /// Indica si es un separador.
    /// </summary>
    public bool IsSeparator { get; init; }

    /// <summary>
    /// Crea un item de menú normal.
    /// </summary>
    public static ContextMenuItem Item(string header, Action command) => new()
    {
        Header = header,
        Command = command
    };

    /// <summary>
    /// Crea un separador.
    /// </summary>
    public static ContextMenuItem Separator() => new()
    {
        IsSeparator = true
    };
}

/// <summary>
/// Interfaz para exponer los comandos del ProjectExplorerShellViewModel al servicio de context menu.
/// </summary>
internal interface IProjectExplorerCommands
{
    void ExecuteCreateFile(FileEntryViewModel? parent);
    void ExecuteCreateDirectory(FileEntryViewModel? parent);
    void ExecuteRename(FileEntryViewModel? entry);
    void ExecuteDelete(FileEntryViewModel? entry);
    void ExecuteDuplicate(FileEntryViewModel? entry);
    void ExecuteRefresh();
}

/// <summary>
/// Servicio para generar menús contextuales según el tipo de entrada.
/// </summary>
internal interface IContextMenuService
{
    /// <summary>
    /// Genera los items del menú contextual para una entrada.
    /// </summary>
    /// <param name="entry">Entrada seleccionada.</param>
    /// <param name="viewModel">ViewModel del explorador.</param>
    /// <returns>Lista de items del menú.</returns>
    IReadOnlyList<ContextMenuItem> GetContextMenuItems(FileEntryViewModel entry, IProjectExplorerCommands viewModel);
}
