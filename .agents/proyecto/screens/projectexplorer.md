# ProjectExplorerView

> Descripción detallada de ProjectExplorerView. Explorador de archivos del proyecto. Muestra la estructura de carpetas y ficheros del proyecto abierto en un panel lateral.

## Ubicación

- **Carpeta**: `UI/Views/ProjectExplorer/`
- **Ficheros**:
  - `ProjectExplorerShellView.axaml` → Vista XAML
  - `ProjectExplorerShellView.axaml.cs` → Code-behind
  - `ProjectExplorerShellViewModel.cs` → ViewModel
  - `FileEntryViewModel.cs` → ViewModel de presentación para una entrada del árbol

## Pantalla

- **Tipo Dock**: Tool (panel lateral fijo)
- **ID**: `ProjectExplorerId` (definido en `NavigationConstants.ProjectExplorerId`)

## Funcionalidad

### Árbol de archivos

- Muestra `Project.Files` como un árbol jerárquico
- Cada entrada es un `FileEntry` con `Name`, `RelativePath` y `Type`
- Tipos soportados: `Project`, `Script`, `Directory`, `Other`
- Ordenación: directorios antes que ficheros, después alfabético por ruta

### Iconos y colores

- Diferentes iconos según el tipo de archivo (FileTypeToIconConverter)
- Colores de foreground según el tipo (FileTypeToForegroundConverter)

## ViewModel

### ProjectExplorerShellViewModel

Dependencias:
- `IProjectContext` → Lectura del proyecto activo
- `IProjectService` → Operaciones de proyecto
- `IMessenger` → Sistema de mensajería
- `INavigationService` → Navegación

Propiedades:
- `IsProjectOpen` → Indica si hay un proyecto abierto
- `ProjectName` → Nombre del proyecto actual (default: "sin solución")
- `FileTree` → ObservableCollection<FileEntryViewModel> (árbol de archivos)

Actualización:
- Se actualiza al recibir `ProjectOpenedMessage` → ejecuta `RefreshProjectContextCommand`
- Se limpia al recibir `ProjectClosedMessage` → ejecuta `RefreshProjectContextCommand`
- Se actualiza al recibir `ProjectSavedMessage` → ejecuta `RefreshProjectContextCommand`

Comandos:
- `RefreshProjectContext()` → Refresca el estado del explorador: rescanea el disco y reconstruye el árbol
- `OpenEntry(FileEntryViewModel?)` → Abre el fichero de la entrada seleccionada en una nueva pestaña del editor (solo para FileType.Script)
- `CreateTestDocument()` → Comando de prueba: abre un nuevo documento de prueba

### FileEntryViewModel

ViewModel de presentación para una entrada (fichero o directorio) del árbol de proyecto. Se reconstruye completamente en cada refresco, por lo que no necesita notificación de cambios.

Propiedades:
- `Name` → Nombre del fichero/directorio
- `RelativePath` → Ruta relativa normalizada
- `Type` → `FileType`
- `IsExpanded` → Indica si el nodo debe mostrarse expandido al cargar el árbol
- `Children` → List<FileEntryViewModel> (hijos del nodo)

Nota: `Icon` y `Foreground` NO son propiedades del ViewModel. Se resuelven en la vista mediante converters:
- `FileTypeToIconConverter` → Convierte `FileType` a glifo Unicode (Segoe MDL2 Assets)
- `FileTypeToForegroundConverter` → Convierte `FileType` a color (Project=CornflowerBlue, Directory=DarkGoldenrod, Script=Green, Other=Black)

## Converters usados

- `FileTypeToIconConverter` → Convierte `FileType` a icono
- `FileTypeToForegroundConverter` → Convierte `FileType` a color de foreground

## Registro en DI

- `ProjectExplorerShellViewModel` → `ProjectExplorerShellViewModel` → Singleton
