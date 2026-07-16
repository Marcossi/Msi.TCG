using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Project;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services.Project;

/// <summary>
/// Tests de ProjectService que verifican la interacción con IFileSystem
/// para las operaciones CRUD de archivos y directorios.
/// Usa directorios temporales reales para verificar el flujo completo.
/// </summary>
public class ProjectServiceFileOperationsTests : IDisposable
{
    private readonly string _testProjectPath;
    private readonly string _testProjectFolder;
    private bool _disposed;

    public ProjectServiceFileOperationsTests()
    {
        // Crear directorio temporal para el proyecto de prueba
        _testProjectFolder = Path.Combine(Path.GetTempPath(), $"tcg-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testProjectFolder);
        _testProjectPath = Path.Combine(_testProjectFolder, "test.scribanproj");
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // Limpiar directorio temporal
        if (Directory.Exists(_testProjectFolder))
        {
            try { Directory.Delete(_testProjectFolder, recursive: true); }
            catch { /* Ignorar errores de limpieza */ }
        }
        _disposed = true;
    }

    private static ILogger<ProjectService> Logger => NullLogger<ProjectService>.Instance;

    private static IFileWatcherService CreateFileWatcher()
    {
        return NSubstitute.Substitute.For<IFileWatcherService>();
    }

    private ProjectService CreateServiceWithRealContext(
        IFileSystem? fileSystem = null,
        bool projectOpen = true)
    {
        var context = new ProjectContext();
        if (projectOpen)
        {
            ((IProjectContextMutator)context).SetProject(new ProjectModel 
            { 
                Name = "Test",
                FolderPath = _testProjectFolder
            }, _testProjectPath);
        }

        return new ProjectService(
            context,
            context,
            NSubstitute.Substitute.For<IProjectSerializer>(),
            NSubstitute.Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            fileSystem ?? new FileSystem(NullLogger<FileSystem>.Instance),
            NSubstitute.Substitute.For<IProjectExplorerStateService>(),
            WeakReferenceMessenger.Default,
            Logger);
    }

    #region CreateFileAsync Tests

    [Fact]
    public async Task CreateFileAsync_CreatesFileOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act
        await service.CreateFileAsync("folder", "test.scriban");

        // Assert
        string expectedPath = Path.Combine(_testProjectFolder, "folder", "test.scriban");
        Assert.True(File.Exists(expectedPath), $"El fichero no se creó en: {expectedPath}");
    }

    [Fact]
    public async Task CreateFileAsync_WithEmptyParentPath_CreatesInRoot()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act
        await service.CreateFileAsync("", "root-file.json");

        // Assert
        string expectedPath = Path.Combine(_testProjectFolder, "root-file.json");
        Assert.True(File.Exists(expectedPath), $"El fichero no se creó en: {expectedPath}");
    }

    [Fact]
    public async Task CreateFileAsync_ThrowsWhenFileAlreadyExists()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateFileAsync("", "existing.scriban");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateFileAsync("", "existing.scriban"));
    }

    #endregion

    #region CreateDirectoryAsync Tests

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectoryOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act
        await service.CreateDirectoryAsync("parent", "new-folder");

        // Assert
        string expectedPath = Path.Combine(_testProjectFolder, "parent", "new-folder");
        Assert.True(Directory.Exists(expectedPath), $"El directorio no se creó en: {expectedPath}");
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithEmptyParentPath_CreatesInRoot()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act
        await service.CreateDirectoryAsync("", "root-folder");

        // Assert
        string expectedPath = Path.Combine(_testProjectFolder, "root-folder");
        Assert.True(Directory.Exists(expectedPath), $"El directorio no se creó en: {expectedPath}");
    }

    [Fact]
    public async Task CreateDirectoryAsync_ThrowsWhenDirectoryAlreadyExists()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateDirectoryAsync("", "existing-folder");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateDirectoryAsync("", "existing-folder"));
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_File_RenamesFileOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateFileAsync("", "old-name.scriban");

        // Act
        await service.RenameAsync("old-name.scriban", "new-name.scriban");

        // Assert
        string oldPath = Path.Combine(_testProjectFolder, "old-name.scriban");
        string newPath = Path.Combine(_testProjectFolder, "new-name.scriban");
        Assert.False(File.Exists(oldPath), $"El fichero antiguo no se eliminó: {oldPath}");
        Assert.True(File.Exists(newPath), $"El fichero nuevo no se creó: {newPath}");
    }

    [Fact]
    public async Task RenameAsync_Directory_RenamesDirectoryOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateDirectoryAsync("", "old-folder");

        // Act
        await service.RenameAsync("old-folder", "new-folder");

        // Assert
        string oldPath = Path.Combine(_testProjectFolder, "old-folder");
        string newPath = Path.Combine(_testProjectFolder, "new-folder");
        Assert.False(Directory.Exists(oldPath), $"El directorio antiguo no se eliminó: {oldPath}");
        Assert.True(Directory.Exists(newPath), $"El directorio nuevo no se creó: {newPath}");
    }

    [Fact]
    public async Task RenameAsync_ThrowsWhenTargetAlreadyExists()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateFileAsync("", "old.scriban");
        await service.CreateFileAsync("", "existing.scriban");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RenameAsync("old.scriban", "existing.scriban"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_File_DeletesFileFromDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateFileAsync("", "file-to-delete.scriban");
        string filePath = Path.Combine(_testProjectFolder, "file-to-delete.scriban");
        Assert.True(File.Exists(filePath));

        // Act
        await service.DeleteAsync("file-to-delete.scriban");

        // Assert
        Assert.False(File.Exists(filePath), $"El fichero no se eliminó: {filePath}");
    }

    [Fact]
    public async Task DeleteAsync_Directory_DeletesDirectoryFromDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateDirectoryAsync("", "folder-to-delete");
        string dirPath = Path.Combine(_testProjectFolder, "folder-to-delete");
        Assert.True(Directory.Exists(dirPath));

        // Act
        await service.DeleteAsync("folder-to-delete");

        // Assert
        Assert.False(Directory.Exists(dirPath), $"El directorio no se eliminó: {dirPath}");
    }

    #endregion

    #region DuplicateAsync Tests

    [Fact]
    public async Task DuplicateAsync_File_CopiesFileOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateFileAsync("", "original.scriban");
        string originalPath = Path.Combine(_testProjectFolder, "original.scriban");

        // Act
        await service.DuplicateAsync("original.scriban");

        // Assert
        Assert.True(File.Exists(originalPath), $"El fichero original no existe: {originalPath}");
        string[] files = Directory.GetFiles(_testProjectFolder, "*_copy*");
        Assert.True(files.Length > 0, "No se encontró ningún fichero duplicado");
    }

    #endregion

    #region MoveAsync Tests

    [Fact]
    public async Task MoveAsync_File_MovesFileOnDisk()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateDirectoryAsync("", "source");
        await service.CreateDirectoryAsync("", "target");
        await service.CreateFileAsync("source", "file.scriban");
        string sourcePath = Path.Combine(_testProjectFolder, "source", "file.scriban");
        Assert.True(File.Exists(sourcePath));

        // Act
        await service.MoveAsync("source/file.scriban", "target");

        // Assert
        string targetPath = Path.Combine(_testProjectFolder, "target", "file.scriban");
        Assert.False(File.Exists(sourcePath), $"El fichero no se eliminó del origen: {sourcePath}");
        Assert.True(File.Exists(targetPath), $"El fichero no se creó en el destino: {targetPath}");
    }

    [Fact]
    public async Task MoveAsync_ThrowsWhenTargetAlreadyExists()
    {
        // Arrange
        var service = CreateServiceWithRealContext();
        await service.CreateDirectoryAsync("", "source");
        await service.CreateDirectoryAsync("", "target");
        await service.CreateFileAsync("source", "file.scriban");
        await service.CreateFileAsync("target", "file.scriban");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MoveAsync("source/file.scriban", "target"));
    }

    #endregion
}
