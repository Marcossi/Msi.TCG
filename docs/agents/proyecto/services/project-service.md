# ProjectService

> Descripción detallada de ProjectService. Gestiona operaciones de proyecto: abrir, cerrar, guardar, guardar como y crear. Actualiza el `IProjectContext` y notifica cambios mediante el sistema de mensajería.

## Ubicación

- **Carpeta**: `Services/Project/`
- **Fichero principal**: `ProjectService.cs`
- **Ficheros parciales**:
  - `ProjectService.Files.cs` → Operaciones de archivos
  - `ProjectService.FileWatcher.cs` → FileWatcher (esqueleto, no implementado)

## Dependencias

- `IProjectContext` → Constructor → Estado del proyecto activo (solo lectura)
- `IProjectSerializer` → Constructor → Serialización de proyectos
- `IMessenger` → Constructor → Notificación de cambios

## Métodos

### OpenProjectAsync(string projectPath)

Abre un proyecto desde la ruta especificada.

Flujo:
1. Valida que la ruta no esté vacía
2. Carga proyecto desde disco usando `IProjectSerializer.LoadAsync()`
3. Establece `project.FolderPath` con `Path.GetDirectoryName(projectPath)`
4. Actualiza `IProjectContext.CurrentProject = project`
5. Actualiza `IProjectContext.CurrentProjectPath = projectPath`
6. Envía `ProjectOpenedMessage(projectPath)`

Excepciones:
- `ArgumentException` si `projectPath` está vacío

TODO: Iniciar FileWatcher

### CloseProjectAsync()

Cierra el proyecto activo.

Flujo:
1. Actualiza `IProjectContext.CurrentProject = null`
2. Actualiza `IProjectContext.CurrentProjectPath = null`
3. Envía `ProjectClosedMessage()`

TODO: Detener FileWatcher, limpiar recursos

### SaveProjectAsync()

Guarda el proyecto actual en disco.

Flujo:
1. Valida que hay un proyecto abierto (`IsProjectOpen`)
2. Valida que `CurrentProjectPath` está establecido
3. Llama a `IProjectSerializer.SaveAsync(project, path)`
4. Envía `ProjectSavedMessage(path)`

Excepciones:
- `InvalidOperationException` si no hay proyecto abierto
- `InvalidOperationException` si `CurrentProjectPath` está vacío

### SaveProjectAsAsync(string newProjectPath)

Guarda el proyecto actual en una nueva ubicación.

Flujo:
1. Valida que hay un proyecto abierto (`IsProjectOpen`)
2. Valida que `newProjectPath` no está vacío
3. Llama a `IProjectSerializer.SaveAsync(project, newProjectPath)`
4. Actualiza `IProjectContext.CurrentProjectPath = newProjectPath`
5. Envía `ProjectSavedMessage(newProjectPath)`

Excepciones:
- `InvalidOperationException` si no hay proyecto abierto
- `ArgumentException` si `newProjectPath` está vacío

### CreateNewProjectAsync(string projectPath, string projectName)

Crea un nuevo proyecto en la ruta especificada.

Flujo:
1. Valida que `projectPath` y `projectName` no están vacíos
2. Crea nuevo `Project` POCO
3. Guarda el proyecto en disco usando `IProjectSerializer.SaveAsync()`
4. Abre el proyecto recién creado (mismo flujo que `OpenProjectAsync`)
5. Envía `ProjectOpenedMessage(projectPath)`

Excepciones:
- `ArgumentException` si `projectPath` o `projectName` están vacíos

### RefreshFilesAsync()

Refresca la lista de ficheros del proyecto activo escaneando la carpeta raíz en disco.

Flujo:
1. Valida que hay un proyecto abierto
2. Escanea `project.FolderPath` con `DirectoryInfo.EnumerateFileSystemInfos()`
3. Clasifica cada entrada con `ClassifyEntry()`
4. Actualiza `project.Files`

Excepciones:
- `InvalidOperationException` si no hay proyecto abierto
- `InvalidOperationException` si la carpeta no existe

## Mensajes enviados

- `ProjectOpenedMessage(string ProjectPath)` → Después de `OpenProjectAsync()` o `CreateNewProjectAsync()`
- `ProjectClosedMessage` → Después de `CloseProjectAsync()`
- `ProjectSavedMessage(string ProjectPath)` → Después de `SaveProjectAsync()` o `SaveProjectAsAsync()`

## IProjectContext

`ProjectService` trabaja con `IProjectContext` que expone:

- `CurrentProject` → `Project?` → Getter público, setter internal
- `CurrentProjectPath` → `string?` → Getter público, setter internal
- `IsProjectOpen` → `bool` → Derivado de `CurrentProject != null`

Regla: Solo `ProjectService` puede modificar el `IProjectContext`. Los ViewModels solo leen a través de `IProjectContext`.

## Estructura de ficheros

```
Services/Project/
├── ProjectService.cs             ← Métodos principales (Open, Close, Save, Create)
├── ProjectService.Files.cs       ← Operaciones de archivos (RefreshFilesAsync)
├── ProjectService.FileWatcher.cs ← FileWatcher (planificado)
├── ProjectContext.cs             ← Implementación de IProjectContext
└── JsonProjectSerializer.cs      ← Serialización JSON
```
