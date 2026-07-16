namespace Msi.TemplateCodeGenerator.Constants;

/// <summary>
/// Constantes relacionadas con rutas de disco, nombres de archivos y extensiones del proyecto.
/// Centraliza la configuración para facilitar cambios futuros.
/// </summary>
internal static class ProjectDirectoryConstants
{
    /// <summary>
    /// Nombre de la carpeta temporal del editor (TemplateCodeGenerator).
    /// </summary>
    public const string EditorFolderName = ".tcg";

    /// <summary>
    /// Nombre de la subcarpeta para archivos de estado.
    /// </summary>
    public const string StateFolderName = "state";

    /// <summary>
    /// Nombre del archivo de estado del ProjectExplorer.
    /// </summary>
    public const string ExplorerStateFileName = "explorerstate.json";

    /// <summary>
    /// Nombre de la subcarpeta para archivos de compilación.
    /// </summary>
    public const string BuildFolderName = "build";

    /// <summary>
    /// Nombre de la subcarpeta para caché.
    /// </summary>
    public const string CacheFolderName = "cache";

    /// <summary>
    /// Nombre de la subcarpeta para logs específicos de proyecto.
    /// </summary>
    public const string LogsFolderName = "logs";

    /// <summary>
    /// Ruta relativa completa al archivo de estado del ProjectExplorer.
    /// </summary>
    public static string ExplorerStateRelativePath => 
        Path.Combine(EditorFolderName, StateFolderName, ExplorerStateFileName);

    /// <summary>
    /// Ruta relativa completa a la carpeta de estado.
    /// </summary>
    public static string StateFolderPath => 
        Path.Combine(EditorFolderName, StateFolderName);

    /// <summary>
    /// Ruta relativa completa a la carpeta de compilación.
    /// </summary>
    public static string BuildFolderPath => 
        Path.Combine(EditorFolderName, BuildFolderName);

    /// <summary>
    /// Ruta relativa completa a la carpeta de caché.
    /// </summary>
    public static string CacheFolderPath => 
        Path.Combine(EditorFolderName, CacheFolderName);

    /// <summary>
    /// Ruta relativa completa a la carpeta de logs.
    /// </summary>
    public static string LogsFolderPath => 
        Path.Combine(EditorFolderName, LogsFolderName);
}
