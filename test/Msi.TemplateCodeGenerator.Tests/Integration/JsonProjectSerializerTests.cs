using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Project;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services.Project;

public class JsonProjectSerializerTests
{
    private static ILogger<JsonProjectSerializer> Logger => NullLogger<JsonProjectSerializer>.Instance;
    private static IFileSystem FileSystem => new FileSystem(NullLogger<FileSystem>.Instance);

    [Fact]
    public void Constructor_Injects_Logger()
    {
        // Arrange & Act
        var serializer = new JsonProjectSerializer(Logger, FileSystem);

        // Assert
        Assert.NotNull(serializer);
    }

    [Fact]
    public async Task SaveAsync_Throws_OnNullProject()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid()}.json");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => serializer.SaveAsync(null!, tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsync_Throws_OnEmptyPath()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        ProjectModel project = new() { Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => serializer.SaveAsync(project, string.Empty));
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_RoundTrip()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        ProjectModel original = new()
        {
            Name = "ProyectoDePrueba",
            FolderPath = "C:\\Test\\Folder"
        };
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_roundtrip_{Guid.NewGuid()}.json");

        try
        {
            // Act
            await serializer.SaveAsync(original, tempFile);
            ProjectModel loaded = await serializer.LoadAsync(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(original.Name, loaded.Name);
            Assert.Equal(original.FolderPath, loaded.FolderPath);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_Throws_OnNonExistentFile()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => serializer.LoadAsync(nonExistent));
    }

    [Fact]
    public async Task LoadAsync_Throws_OnEmptyPath()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => serializer.LoadAsync(string.Empty));
    }

    [Fact]
    public async Task LoadAsync_Throws_OnCorruptJson()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        string tempFile = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid()}.json");

        try
        {
            await File.WriteAllTextAsync(tempFile, "{ this is not valid json !!!");

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                () => serializer.LoadAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_SupportsJsonComments()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        string tempFile = Path.Combine(Path.GetTempPath(), $"comments_{Guid.NewGuid()}.json");

        try
        {
            string jsonWithComments = """
            {
              // Este es un comentario
              "fileFormatVersion": 1,
              /* Otro comentario
                 multilinea */
              "project": {
                "name": "ProyectoConComentarios"
              }
            }
            """;
            await File.WriteAllTextAsync(tempFile, jsonWithComments);

            // Act
            ProjectModel loaded = await serializer.LoadAsync(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal("ProyectoConComentarios", loaded.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_Throws_OnUnsupportedFutureVersion()
    {
        // Arrange
        var serializer = new JsonProjectSerializer(Logger, FileSystem);
        string tempFile = Path.Combine(Path.GetTempPath(), $"future_{Guid.NewGuid()}.json");

        try
        {
            string json = """
            {
              "fileFormatVersion": 999,
              "project": {
                "name": "Futuro"
              }
            }
            """;
            await File.WriteAllTextAsync(tempFile, json);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => serializer.LoadAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
