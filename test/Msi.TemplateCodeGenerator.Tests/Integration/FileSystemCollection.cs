namespace Msi.TemplateCodeGenerator.Tests.Integration;

/// <summary>
/// Colección de xUnit para serializar tests que usan filesystem real.
/// Evita interferencias entre tests que crean/eliminan directorios temporales.
/// </summary>
[CollectionDefinition("FileSystem")]
public class FileSystemCollection : ICollectionFixture<FileSystemFixture>
{
}

/// <summary>
/// Fixture para tests que usan filesystem real.
/// Proporciona un directorio temporal único para cada test.
/// </summary>
public class FileSystemFixture : IDisposable
{
    public string TempDirectory { get; }

    public FileSystemFixture()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), $"tcg-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(TempDirectory))
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
                // Ignorar errores de limpieza
            }
        }
    }
}
