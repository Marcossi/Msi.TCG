# AGENTS.md

## Source of truth

Architecture docs live in `.agents/` (layered). Load only the minimum layer needed:
1. `.agents/proyecto/` — current product specifics
   - `arquitectura-y-dominio.md` — propósito, modelo de dominio, MVVM, mensajería, navegación
   - `arquitectura-ioc.md` — jerarquía de contenedores, patrón Context vs Service, DI table
   - `restricciones.md` — Scriban static-only, async I/O, naming, serialización, audit trail
   - `modelo-de-proyecto.md` — estructura conceptual .scribanproj, secciones recursivas
   - `services/` — detalles de ProjectService, TemplatesService, FileService
   - `screens/` — detalles de MainShell, TemplateEditor, ProjectExplorer, Settings
   - `adr/` — Architecture Decision Records (consultar `adr/README.md` antes de cambios estructurales)
   - `especificaciones/` — especificaciones técnicas de componentes
     - `bootstrap-alignment.md` — reestructuración del bootstrap con IApp
     - `command-routing-alignment.md` — clasificación de operaciones y fixes de alineación
2. `.agents/msi-guidelines-avalonia/` — Avalonia MVVM, bootstrap, shell, navigation rules
   - `msi-arquitectura-mvvm.md` — estructura de Screens, reglas MVVM, antipatrones, audit trail
   - `msi-bootstrap-avalonia.md` — arranque, builders, IApp (opcional)
   - `msi-navegacion-shell-y-ventanas.md` — reglas de shell, diálogos, documentos
   - `msi-navigation-simple.md` — patrón NavigateTo + SelectTarget (shells simples)
   - `msi-navigation-docking.md` — patrón Dock.Avalonia, API rica, AppDockFactory
3. `.agents/msi-guidelines-dotnet/` — .NET base conventions and hosting rules
   - `msi-base-dotnet.md` — código, async, DI, capas (3-project)
   - `msi-base-dotnet-single-project.md` — variante single-project
   - `msi-bootstrap-hosting.md` — host, Serilog, IoC
4. `.agents/libraries-doc/` — third-party library docs
   - `Dock-Avalonia/` — Dock for Avalonia (IDE-like docking)
   - `Scriban-7.2.5/` — Scriban template engine
     - **Sintaxis de scripts**: `language.md` + `builtins/` — cargar al escribir o depurar templates `.scriban`
     - **API de .NET**: `runtime/` — cargar al trabajar con el motor (TemplateContext, ScriptObject, parsing, rendering)

**Note:** `msi-base-dotnet.md` describes an ideal 3-project architecture (Domain/Infrastructure/App). This repo uses single-project structure. See `msi-base-dotnet-single-project.md` for the variant applied here.

## Commands

```powershell
# Build
dotnet build

# Run
dotnet run --project src/Msi.TemplateCodeGenerator/Msi.TemplateCodeGenerator.csproj

# Test
dotnet test
```

Build output goes to `artifacts/` at repo root (`UseArtifactsOutput=true`). Solution file, `Directory.Build.props`, `Directory.Packages.props`, and `global.json` are all at repo root.

### Logs

| Fichero | Modo | Ruta (desde repo root) | Propósito | Retención |
|---|---|---|---|---|
| `Msi.TemplateCodeGenerator-YYYYMMDD.log` | DEBUG y RELEASE | `artifacts/bin/Msi.TemplateCodeGenerator/debug/logs/` | Log histórico diario | 7 días (automática) |
| `Msi.TemplateCodeGenerator-last.log` | Solo DEBUG | `artifacts/bin/Msi.TemplateCodeGenerator/debug/logs/` | Log de la ejecución actual para debugging | Se sobrescribe en cada arranque |

Cuando un usuario reporte un problema, indicar que reproduzca el error y consulte `Msi.TemplateCodeGenerator-last.log`. Ese fichero contiene únicamente la ejecución problemática, sin ruido de sesiones anteriores.

## Stack

- .NET 10 (`net10.0-windows`), C# latest, Avalonia 12
- CommunityToolkit.Mvvm, Dock.Avalonia (IDE-like docking), Scriban 7 (template engine)
- Serilog (configured via `appsettings.json`), Microsoft.Extensions.Hosting/DI
- Solution format: `.slnx` (XML), central package management (`Directory.Packages.props`)

## Architecture rules (enforced)

- **MVVM strict**: ViewModels expose bindings/commands only. Business logic goes in `Services/`. No exceptions.
- **No UI framework in ViewModels**: Prohibido acceder a `Avalonia.*` types desde un ViewModel. Toda interacción con UI se abstrae en `UI/Services/`.
- **Service separation**: `Services/` = domain logic (no UI deps). `UI/Services/` = Avalonia-dependent services (navigation, dialogs, docking).
- **No direct VM-to-VM calls**: Use `WeakReferenceMessenger` or shared services. Unregister obligatorio al destruir el VM.
- **Context vs Service**: `IProjectContext` = read-only state. `IProjectService` = operations that mutate state.
- **No Service Locator**: Constructor injection only. `AppDockFactory` is the one justified exception for layout composition.
- **Scriban**: Only static methods are accessible from templates.
- **Audit Trail**: Toda interacción significativa del usuario loguea `[UI]` con nivel Information. Ver `restricciones.md`.
- **Logging obligatorio**: Todo servicio y ViewModel inyecta `ILogger<T>`. Prohibido `catch {}` vacío.
- **Accesibilidad**: Implementaciones de servicios son `internal sealed`. Solo interfaces son `public`.

## Code conventions (non-obvious)

- **Code in English**, comments and XML docs in **Spanish**.
- `var` is **disabled** (`csharp_style_var_* = false`) — always use explicit types.
- File-scoped namespaces are **enforced as error** (`file_scoped:error`).
- `_camelCase` private fields, `PascalCase` everything else, `I` prefix for interfaces.
- CRLF line endings. 4-space indent for `.cs` and `.axaml`. Tab indent for `.csproj`.
- All ViewModels inherit `BaseViewModel` (extends `ObservableObject`).
- ViewLocator convention: `FooViewModel` → `FooView` (namespace-qualified type name replacement).
- Service implementations are `internal sealed`. Only interfaces are `public`.
- All services and ViewModels inject `ILogger<T>`. No exceptions.
- Audit trail: significant user interactions log `[UI]` prefix. See `restricciones.md`.

## Project structure

```
<repo-root>/
├── AGENTS.md, .editorconfig, .gitignore
├── Msi.TemplateCodeGenerator.slnx
├── Directory.Build.props, Directory.Packages.props, global.json
├── artifacts/                    ← Build output (all projects)
├── src/
│   └── Msi.TemplateCodeGenerator/
│       ├── Program.cs, App.axaml(.cs), DependencyInjection.cs, ViewLocator.cs
│       ├── Constants/        Interfaces/       Messages/
│       ├── Models/           Services/
│       │   ├── Project/      ├── Project/
│       │   └── Templates/    └── Templates/
│       └── UI/
│           ├── Views/                    ← Screens (cada una en su carpeta)
│           │   ├── Shell/                ← MainShell + MainWindow
│           │   │   ├── MainShellView.axaml(.cs)
│           │   │   ├── MainWindow.axaml(.cs)
│           │   │   └── ViewModels/
│           │   │       └── MainShellViewModel.cs
│           │   ├── ProjectExplorer/
│           │   │   ├── ProjectExplorerShellView.axaml(.cs)
│           │   │   ├── ViewModels/
│           │   │   │   ├── ProjectExplorerShellViewModel.cs
│           │   │   │   └── FileEntryViewModel.cs
│           │   │   └── Converters/
│           │   │       ├── FileTypeToIconConverter.cs
│           │   │       └── FileTypeToForegroundConverter.cs
│           │   ├── TemplateEditor/
│           │   │   ├── TemplateEditorShellView.axaml(.cs)
│           │   │   └── ViewModels/
│           │   │       ├── TemplateEditorShellViewModel.cs
│           │   │       └── BaseTextEditorViewModel.cs
│           │   └── Settings/
│           │       ├── SettingsShellView.axaml(.cs)
│           │       └── ViewModels/
│           │           └── SettingsShellViewModel.cs
│           ├── Shared/                   ← Recursos compartidos entre screens
│           │   ├── BaseViewModel.cs
│           │   └── Converters/           ← Converters reutilizados
│           └── Services/                 ← UI services
│               ├── Navigation/           (NavigationService, AppDockFactory)
│               └── Dialogs/              (DialogService, SaveConfirmationDialog)
└── test/
    └── Msi.TemplateCodeGenerator.Tests/
```

### Namespace convention

```
UI.Views.<Screen>                    → View files
UI.Views.<Screen>.ViewModels         → ViewModels
UI.Views.<Screen>.Converters         → Local converters
UI.Shared                            → BaseViewModel, shared resources
UI.Shared.Converters                 → Shared converters
UI.Services.Navigation               → NavigationService, AppDockFactory
UI.Services.Dialogs                  → DialogService
```

## Bootstrap flow

`Program.Main` → `AppBuilder.Configure<App>()` → `App.OnFrameworkInitializationCompleted` → `Host.CreateApplicationBuilder` → config → Serilog → `AddTemplateCodeGeneratorServices()` → `host.Start()` → resolve `MainWindow` + `MainShellViewModel` → set as desktop main window.

Bootstrap wraps entry point resolution in try-catch with logging. Prohibido `catch {}` vacío.
