# Arquitectura y dominio

> Propósito, modelo de dominio, arquitectura MVVM, mensajería, navegación y estado actual del proyecto.

## Propósito

**Msi.TemplateCodeGenerator** es una aplicación de escritorio (Avalonia UI) para generar código automatizado mediante plantillas Scriban.

1. Crear y gestionar plantillas Scriban que definan la estructura del código a generar.
2. Definir modelos de datos (clases C#) como fuente de información para las plantillas.
3. Visualizar en tiempo real el resultado de la generación de código.

## Estructura del proyecto

```
<repo-root>/
├── AGENTS.md, .editorconfig, .gitignore
├── Msi.TemplateCodeGenerator.slnx
├── Directory.Build.props, Directory.Packages.props, global.json
├── artifacts/                    ← Build output (all projects)
├── src/
│   └── Msi.TemplateCodeGenerator/
│       ├── Program.cs, App.axaml(.cs), DependencyInjection.cs, ViewLocator.cs
│       ├── Models/              ← Entidades de dominio
│       ├── Interfaces/          ← Contratos IoC
│       ├── Services/            ← Lógica de negocio (sin deps de UI)
│       │   ├── Project/
│       │   └── Templates/
│       ├── Constants/           ← Constantes de navegación y proyecto
│       ├── Messages/            ← Mensajes del sistema (mensajería desacoplada)
│       └── UI/
│           ├── Views/           ← Screens (cada una en su carpeta con ViewModels/ y Converters/)
│           ├── Shared/          ← Recursos compartidos (BaseViewModel, converters globales)
│           └── Services/        ← Servicios de UI (Navigation/, Dialogs/)
└── test/
    └── Msi.TemplateCodeGenerator.Tests/
```

### Estructura de Screens

Cada Screen vive en `UI/Views/<Screen>/` con sus artefactos locales:

```
UI/Views/<Screen>/
├── <Screen>View.axaml(.cs)      ← Vista
├── ViewModels/                   ← ViewModel principal + sub-VMs
└── Converters/                   ← Converters locales a esta screen
```

Recursos compartidos entre screens van en `UI/Shared/` (BaseViewModel, converters globales, estilos).

## Modelo de dominio

- **Project**: Contenedor de plantillas. Propiedades: `Name`, `FolderPath`, `Files` (List<FileEntry>). Futuras: `ReferencedAssemblies`, `Configuration`.
- **Template**: Archivo `.scriban` con texto estático + expresiones dinámicas.
- **Model**: Clases C# referenciadas desde el proyecto. Las plantillas acceden a sus propiedades/métodos.

## Arquitectura MVVM

Cadena de dependencias: `UI/Views (.axaml) → ViewModels → ICommandRegistry → ICommandRoute → Services → Interfaces → Models`

- `Models/` → POCOs de dominio
- `Interfaces/` → Contratos IoC (incluye `ICommandRegistry`, `ICommandRoute`, `ICommandContext`)
- `Services/` → Lógica de negocio
- `UI/Services/` → Lógica de presentación (depende de Avalonia/Dock)
- `UI/Services/Commands/` → `CommandRegistry` (intermediario entre Shell y ViewModels contextuales)
- `UI/Views/` → ViewModels + XAML (.axaml)

### Capa de comandos (Command Routing)

Los comandos del Shell que operan en el documento/tool activo **deben** usar `ICommandRegistry` para resolver el comando contextual. Esto permite que el Shell invoque operaciones sin acoplarse a ViewModels concretos.

**Flujo obligatorio para comandos contextuales:**
1. UI (menú/toolbar/keybinding) → `MainShellViewModel.SaveCommand`
2. `MainShellViewModel` → `ICommandRegistry.ExecuteAsync("Save")`
3. `CommandRegistry` → `ICommandContext.ActiveRoute` (el ViewModel del documento activo)
4. `ICommandRoute.ExecuteAsync("Save")` → `BaseTextEditorViewModel.SaveAsync()`
5. `SaveAsync()` → `IFileService.WriteTextAsync()`

**Excepción:** Los comandos globales del Shell (abrir/cerrar proyecto, nuevo proyecto, salir) invocan servicios directamente, sin pasar por `ICommandRegistry`.

Ver `especificaciones/command-routing.md` para detalles de implementación.

## Inyección de dependencias

Servicios registrados en contenedor IoC, inyectados por constructor:

```csharp
services.AddSingleton<IProjectContext, ProjectContext>();
services.AddSingleton<IProjectSerializer, JsonProjectSerializer>();
services.AddSingleton<IProjectService, ProjectService>();
services.AddSingleton<ITemplatesService, TemplatesService>();
services.AddSingleton<IFileService, FileService>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<INavigationService, NavigationService>();
```

Dependencias de ViewModels:
- `MainShellViewModel` → `IProjectService`, `INavigationService`, `ICommandRegistry`
- `ProjectExplorerShellViewModel` → `IProjectContext`, `IProjectService`, `IMessenger`, `INavigationService`
- `TemplateEditorShellViewModel` → `ITemplatesService`, `IFileService`, `IDialogService`, `ICommandRoute` (implementa)

## Sistema de mensajería

Comunicación desacoplada entre ViewModels usando `WeakReferenceMessenger.Default` (CommunityToolkit.Mvvm):

- `ProjectOpenedMessage(string ProjectPath)` → Trigger: proyecto abierto
- `ProjectClosedMessage` → Trigger: proyecto cerrado
- `ProjectSavedMessage(string ProjectPath)` → Trigger: proyecto guardado

Flujo típico:
1. Usuario abre proyecto → `MainShellViewModel.OpenProjectAsync()`
2. `IProjectService.OpenProjectAsync(path)` → carga proyecto, actualiza `IProjectContext`
3. `ProjectService` envía `ProjectOpenedMessage`
4. `ProjectExplorerShellViewModel` recibe mensaje y refresca contexto

**Regla**: Todo ViewModel que se suscriba a mensajes debe hacer `Unregister` al destruirse (implementar `IDisposable` o `ICloseAware`).

## Audit Trail

Toda interacción significativa del usuario produce una línea de log con nivel `Information` y prefijo `[UI]`.

Eventos logueados:
- Ejecución de comandos: `[UI] Command: {CommandName}`
- Navegación: `[UI] Navigate: {Target}`
- Apertura/cierre de documentos: `[UI] Open document: {FilePath}`
- FileDialog (selección o cancelación): `[UI] FileDialog: Selected '{Path}'` / `Cancelled`
- Diálogos de confirmación: `[UI] Dialog: '{Question}' → {Result}`
- Operaciones de proyecto: `[UI] Project: {Operation} '{Path}'`

No se loguean: pulsaciones de tecla, cambios de foco, scroll, hover, resize, debounce interno.

El objetivo es poder reproducir el flujo del usuario leyendo el log.

## Sistema de navegación (Dock.Avalonia)

Layout gestionado por `Dock.Avalonia`:

```
RootDock
└── ProportionalDock (horizontal)
    ├── ToolDock (22% ancho) ← ProjectExplorer
    ├── Splitter (redimensionable)
    └── DocumentDock (pestañas) ← Editores, Settings
```

Componentes:
- `INavigationService` / `NavigationService`: Contrato + implementación que abstrae Dock.Avalonia. Lazy initialization del layout. **También implementa `ICommandContext`** para trackear el documento activo.
- `AppDockFactory : Factory`: Hereda de `Dock.Model.Mvvm.Factory`. Crea layout lazy. Resuelve ViewModels desde `IServiceProvider`.

Métodos de INavigationService:
- `GetLayout()` → Devuelve `IRootDock` para binding
- `ActivateDockable(id)` → Activa panel por ID
- `HideDockable(id)` → Oculta panel
- `OpenFile(path)` → Abre archivo en nuevo editor (pestaña)
- `CloseDocumentAsync(id)` → Cierra documento con confirmación si es necesario (ICloseAware)
- `CanCloseAllAsync()` → Comprueba si todos los documentos pueden cerrarse
- `GetOpenEditors()` → Obtiene lista de editores abiertos

Propiedad de ICommandContext (implementado por NavigationService):
- `ActiveRoute` → Devuelve el `ICommandRoute` del documento/tool activo (null si no hay o no implementa la interfaz)

## Estado actual

Implementado:
- Gestión de proyectos (abrir, cerrar, guardar, guardar como, crear)
- Editor Scriban con vista previa en tiempo real (debounce 1s)
- Explorador de archivos del proyecto (árbol jerárquico)
- Navegación con paneles dockeables
- Serialización JSON de proyectos (JSONC lectura, sin preservar comentarios al guardar)
- Logging con Serilog (Console + File)
- Sistema de mensajería con WeakReferenceMessenger
- Diálogo de confirmación de guardado (ICloseAware)

Pendiente:
- `FileWatcher`: Vigilancia de cambios en carpeta del proyecto (esqueleto en `ProjectService.FileWatcher.cs`)
- Validaciones de estructura al cargar
- Gestión de plantillas en modelo `Project`
- Migración a JSON5 (preservar comentarios)
- `ReferencedAssemblies` en modelo `Project`
- `Configuration` en modelo `Project`
