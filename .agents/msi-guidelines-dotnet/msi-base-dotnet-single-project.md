# Arquitectura Single-Project (.NET)

> Variante de `msi-base-dotnet.md` para proyectos donde un solo ensamblado es suficiente. La separacion de capas se mantiene por convencion de carpetas, no por boundaries de compilador.

## Cuando aplicar

- Aplicaciones de escritorio pequenas o medianas.
- Prototipos o MVPs donde 3 ensamblados es over-engineering.
- Proyectos donde el equipo es pequeno y la disciplina de carpetas es suficiente.

## Cuando NO aplicar

- Equipos grandes donde diferentes personas trabajan en diferentes capas.
- Proyectos donde Domain tiene logica compleja que merece aislamiento.
- Cuando se necesita publicar Domain como paquete NuGet independiente.
- Cuando las reglas de referencia entre capas deben ser enforceadas por el compilador.

## Estructura de la solución

La solución (`.slnx`) y los ficheros de configuración globales van en la **raíz del repositorio**:

```
<repo-root>/
├── AGENTS.md
├── .editorconfig
├── .gitignore
├── <App>.slnx
├── Directory.Build.props         ← UseArtifactsOutput=true
├── Directory.Packages.props      ← Central package management
├── global.json
├── artifacts/                    ← Salida de build (todos los proyectos)
├── src/
│   └── <App>/
│       ├── Models/
│       ├── Interfaces/
│       ├── Services/
│       ├── Constants/
│       ├── Messages/
│       ├── UI/
│       ├── DependencyInjection.cs
│       ├── Program.cs
│       └── App.axaml(.cs)
└── test/
    └── <App>.Tests/
```

### Ficheros de configuración globales (en la raíz)

| Fichero | Propósito |
|---|---|
| `Directory.Build.props` | Propiedades MSBuild comunes + `UseArtifactsOutput=true` |
| `Directory.Packages.props` | Central package management |
| `global.json` | Versión del SDK |
| `.editorconfig` | Convenciones de código (un solo fichero) |

### Sistema de Artifacts

Todos los proyectos usan el sistema de artifacts unificado. En `Directory.Build.props`:

```xml
<PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
</PropertyGroup>
```

### Comandos

Los comandos se ejecutan desde la raíz del repositorio:

```powershell
dotnet build
dotnet run --project src/<App>/<App>.csproj
dotnet test
```

## Separacion por carpetas

La separacion de capas se mantiene por convencion. La regla es la misma que en 3-project: **`Services/` NUNCA importa namespaces de Avalonia**.

Dentro del proyecto `src/<App>/`:

```
<App>/
├── Models/              ← Entidades de dominio (POCOs, enums)
├── Interfaces/          ← Todos los contratos (dominio + UI)
├── Services/            ← Logica de negocio (SIN dependencias de UI)
│   ├── Project/
│   └── Templates/
├── Constants/           ← Constantes de la aplicacion
├── Messages/            ← Mensajes del sistema de mensajeria
├── UI/
│   ├── Views/           ← Screens (vistas + ViewModels)
│   ├── Shared/          ← Recursos compartidos de UI
│   └── Services/        ← Servicios dependientes de Avalonia
├── DependencyInjection.cs
├── Program.cs
└── App.axaml(.cs)
```

### Regla de dependencias (por convencion)

```
UI/Views/  →  UI/Services/  →  Services/  →  Interfaces/  →  Models/
```

- `Services/` no conoce `UI/` ni Avalonia.
- `UI/Services/` puede conocer `Services/` e `Interfaces/`.
- `UI/Views/` puede conocer todo lo anterior.
- `Models/` e `Interfaces/` no conocen nada de UI ni Services.

## Interfaces

En single-project, **todas las interfaces van en `Interfaces/`** (raiz). No se separan por capa porque solo hay un ensamblado.

```
Interfaces/
├── IProjectService.cs       ← Contrato de dominio
├── IFileService.cs          ← Contrato de infraestructura
├── INavigationService.cs    ← Contrato de UI
└── IDialogService.cs        ← Contrato de UI
```

## DependencyInjection.cs

Un solo fichero con un solo metodo de extension:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddTemplateCodeGeneratorServices(
        this IServiceCollection services)
    {
        // Dominio
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFileService, FileService>();

        // UI
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        return services;
    }
}
```

No hay invocacion secuencial de multiples `AddXxxServices()` porque solo hay un ensamblado.

## Accesibilidad

- **Interfaces**: `public` (son el contrato).
- **Implementaciones**: `internal sealed` (detalle de implementacion).

```csharp
// Interfaz: public
public interface IProjectService { ... }

// Implementacion: internal sealed
internal sealed class ProjectService : IProjectService { ... }
```

## Ventajas

- Simplicidad: un solo `.csproj`, una sola compilacion.
- Sin friction de referencias entre proyectos.
- Mas rapido de configurar y mantener.

## Desventajas

- La separacion de capas es por disciplina, no por compilador.
- Un descuido puede introducir dependencias cruzadas.
- No se puede publicar Domain como paquete independiente.
- El compilador no detecta violaciones de capa.

## Mitigacion de desventajas

- Revision de code review: verificar que `Services/` no importa `Avalonia.*`.
- EditorConfig: reglas de namespace para detectar desviaciones.
- Documentacion: este fichero + `msi-base-dotnet.md` como referencia.
