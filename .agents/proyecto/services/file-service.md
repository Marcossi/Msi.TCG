# FileService

> Descripción detallada de FileService. Adaptador fino sobre `System.IO.File` para operaciones de lectura y escritura de ficheros de texto. No contiene lógica de dominio.

## Ubicación

- **Carpeta**: `Services/`
- **Fichero**: `FileService.cs`

## Dependencias

Ninguna. Es un adaptador directo sobre APIs del framework.

## Implementación

```csharp
internal sealed class FileService : IFileService
{
    public Task<string> ReadTextAsync(string filePath)
        => File.ReadAllTextAsync(filePath);

    public Task WriteTextAsync(string filePath, string content)
        => File.WriteAllTextAsync(filePath, content);
}
```

## Métodos

### ReadTextAsync(string filePath)

Lee el contenido completo de un fichero de texto.

Implementación: `File.ReadAllTextAsync(filePath)`

### WriteTextAsync(string filePath, string content)

Escribe contenido de texto en un fichero, sobrescribiendo si existe.

Implementación: `File.WriteAllTextAsync(filePath, content)`

## Registro en DI

- `IFileService` → `FileService` → Singleton

Nota: Aunque es un adaptador sin estado, se registra como Singleton para mantener consistencia con el patrón de servicios.
