# MSI Guidelines - Base .NET Código y Diseño

> Capa reutilizable y agnóstica. Define las reglas de estilo de código, convenciones y diseño arquitectónico para cualquier desarrollo en C#.

## 1. Convenciones de Código y Estilo
- **Idioma del Código:** Todo el código fuente (clases, métodos, variables, excepciones, logs) debe escribirse estrictamente en **inglés**.
- **Idioma de Documentación:** Los comentarios de soporte y la documentación XML de los métodos públicos se escribirán en **español**.
- **Nomenclatura (Naming Conventions):**
  - Clases, Interfaces, Métodos, Propiedades y Constantes: `PascalCase`.
  - Variables locales y parámetros de métodos: `camelCase`.
  - Campos privados de clase (fields): `_camelCase`.

## 2. Programación Asíncrona (Async/Await)
- **Regla de Oro:** Cualquier operación que implique Entrada/Salida (I/O) o procesamiento pesado que pueda bloquear un hilo de ejecución debe ser obligatoriamente `async`.
- **Casos Mínimos Obligatorios:**
  - Persistencia (Carga, guardado o acceso a sistema de archivos/disco).
  - Operaciones de red, API REST o comunicación externa.
  - Cualquier procesamiento que comprometa la fluidez de la interfaz de usuario (UI).

## 3. Principios de Arquitectura y Separación de Responsabilidades
- **Separación de Estado y Operación:**
  - **Modelos/Entidades:** Representan el estado y los datos del dominio.
  - **Servicios:** Ejecutan operaciones, contienen la lógica de negocio y orquestan la mutación del estado.
- **Inyección de Dependencias (DI):**
  - El acoplamiento entre componentes se resuelve exclusivamente por el constructor.
  - Queda prohibido el uso del patrón *Service Locator* como mecanismo general para resolver dependencias.
- **Contratos:** Las interfaces definen el comportamiento esperado. Su ubicación depende de la capa (ver sección 4).
- **Mensajería:** Para comunicación desacoplada entre componentes independientes, usar un bus de eventos o mensajería in-app. Evitar referencias directas que acoplen ciclos de vida.

## 4. Estructura de la Solución

### Regla de ubicación en la raíz

La solución (`.slnx` o `.sln`) y los ficheros de configuración globales deben estar en la **raíz del repositorio**, no dentro de una subcarpeta `src/`.

```
<repo-root>/
├── AGENTS.md
├── .editorconfig
├── .gitignore
├── <App>.slnx                    ← Solución en la raíz
├── Directory.Build.props         ← Propiedades comunes (incluye UseArtifactsOutput)
├── Directory.Packages.props      ← Central package management
├── global.json                   ← Versión del SDK
├── artifacts/                    ← Salida de build (todos los proyectos)
├── src/
│   ├── <App>.Domain/
│   ├── <App>.Infrastructure/
│   └── <App>/
└── test/
    └── <App>.Tests/
```

### Ficheros de configuración globales

Estos ficheros **siempre** van en la raíz del repositorio:

| Fichero | Propósito |
|---|---|
| `Directory.Build.props` | Propiedades MSBuild comunes (LangVersion, Nullable, WarningLevel, UseArtifactsOutput) |
| `Directory.Packages.props` | Central package management (todas las versiones de NuGet) |
| `global.json` | Versión del SDK de .NET |
| `.editorconfig` | Convenciones de código (un solo fichero en la raíz) |

### Sistema de Artifacts obligatorio

Todos los proyectos de la solución deben usar el sistema de artifacts unificado:

```xml
<!-- Directory.Build.props (en la raíz) -->
<PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
</PropertyGroup>
```

Esto garantiza que:
- Los outputs de build van a `artifacts/` en la raíz (no en `bin/` y `obj/` dentro de cada proyecto).
- Los proyectos de test también usan el mismo directorio de artifacts.
- El `.gitignore` puede ignorar `artifacts/` de forma centralizada.

### Comandos

Los comandos se ejecutan desde la raíz del repositorio:

```powershell
# Build
dotnet build

# Run
dotnet run --project src/<App>/<App>.csproj

# Test
dotnet test
```

## 5. Arquitectura de Capas

Modelo estándar de **3 proyectos**. Las referencias de proyecto son la única garantía de que las capas no se contaminen.

### Nota sobre variante Single-Project

Para proyectos pequeños o medianos donde 3 ensamblados es over-engineering, consultar `msi-base-dotnet-single-project.md`. La separación de capas se mantiene por convención de carpetas, no por boundaries de compilador. El proyecto decide cuál variante usar y lo documenta en su `AGENTS.md`.

### 4.1. `<App>.Domain` (Proyecto de dominio)
- **Contenido:** Entidades, value objects, enums del dominio, interfaces de servicios (`IRepository`, `IService`), modelos de datos y DTOs.
- **Dependencias:** Ninguna. Solo `System.*` y librerías puras de dominio (ej. `Ardalis.GuardClauses`).
- **Regla:** **PROHIBIDO** referenciar cualquier otro proyecto de la solución. **PROHIBIDO** importar namespaces de UI, EF Core, `System.IO`, `System.Net.Http`, o cualquier framework.

### 4.2. `<App>.Infrastructure` (Proyecto de infraestructura)
- **Contenido:** Implementaciones concretas de las interfaces de Domain. Acceso a datos (EF Core, Dapper, SQLite), lectura/escritura de ficheros, APIs externas, serialización, logging concreto.
- **Dependencias:** Referencia a `<App>.Domain`. Librerías técnicas (EF Core, HttpClient, etc.).
- **Regla:** **PROHIBIDO** conocer la UI. **PROHIBIDO** importar namespaces de Avalonia, WPF, o cualquier framework de presentación.

### 4.3. `<App>` (Proyecto de aplicación / UI)
- **Contenido:** Bootstrap (`Program.cs`, `App.axaml`), composición DI, ViewModels, vistas, converters, servicios de UI (navegación, diálogos, clipboard).
- **Dependencias:** Referencia a `<App>.Domain` y `<App>.Infrastructure`. Framework de UI (Avalonia, etc.).
- **Regla:** **PROHIBIDO** que Domain o Infrastructure referencien a este proyecto.

### 4.4. Regla de dependencias (resumen)
```
<App>  →  <App>.Infrastructure  →  <App>.Domain
  │                                  ↑
  └──────────────────────────────────┘
```
- Domain: 0 referencias a proyectos propios.
- Infrastructure: solo referencia a Domain.
- App: referencia a Domain e Infrastructure.
- **NUNCA** referencias circulares. **NUNCA** que Infrastructure o Domain conozcan App.

## 6. Organización Interna de Proyectos

### 6.1. `<App>.Domain`
```
Domain/
├── Entities/       (clases de dominio con estado)
├── Interfaces/     (I<Nombre>Service, IRepository<T>)
├── Models/         (DTOs, records de datos, filtros)
└── Enums/          (enumerados del dominio)
```

### 6.2. `<App>.Infrastructure`
```
Infrastructure/
├── Repositories/   (implementaciones de IRepository<T>)
├── Services/       (implementaciones de IService)
├── Adapters/       (mapeo de datos externos → entidades de dominio)
└── Configuration/  (DbContext, configuraciones de EF Core, etc.)
```

### 6.3. `<App>` (Capa de presentación)
```
<App>/
├── UI/
│   ├── Views/          (vistas y ViewModels por pantalla)
│   ├── Services/       (NavigationService, DialogService, etc.)
│   └── Converters/     (IValueConverter para binding)
├── DependencyInjection.cs
├── Program.cs
└── App.axaml / App.axaml.cs
```

### 6.4. Regla de carpetas para implementaciones
- **Un solo fichero** → directamente en la carpeta del tipo (`Services/NombreService.cs`).
- **Más de un fichero** (helpers, enums internos, records auxiliares) → subcarpeta dedicada (`Services/NombreService/`).

### 6.5. Registro en IoC
- Cada proyecto expone un método de extensión `Add<Proyecto>Services(this IServiceCollection)` en su propio `DependencyInjection.cs`.
- El proyecto App llama a todos en su `Program.cs` en orden: Domain → Infrastructure → UI Services.

### 6.6. Accesibilidad

- **Interfaces**: `public` (son el contrato).
- **Implementaciones de servicios**: `internal sealed` (detalle de implementación).

```csharp
// Interfaz: public
public interface IProjectService { ... }

// Implementación: internal sealed
internal sealed class ProjectService : IProjectService { ... }
```

Excepción: clases que el framework necesita instanciar por reflexión (Views, ViewModels registrados en DI) pueden ser `public`.

### 6.7. Estilo de código

- **`var` deshabilitado**: Usar siempre tipos explícitos. El `.editorconfig` lo fuerza (`csharp_style_var_* = false`).
- **File-scoped namespaces**: Obligatorios (`namespace X;`). El `.editorconfig` lo fuerza como error.
- **Usings redundantes**: Eliminar `using System;`, `using System.IO;`, `using System.Linq;` cuando están cubiertos por `ImplicitUsings`.

## 7. Antipatrones Prohibidos
- **Fuga de Dominio:** Meter lógica de negocio, validaciones complejas o cálculo de datos en ViewModels o servicios de UI.
- **Saltarse capas:** Que un ViewModel use directamente una implementación de Infrastructure en lugar de su interfaz de Domain.
- **Dominio Sucio:** Que Domain dependa de EF Core, `System.IO`, `System.Net.Http`, o cualquier framework externo.
- **Service Locator:** Resolver servicios desde el contenedor dentro de la lógica de negocio o ViewModels.
- **Acoplamiento Directo:** Llamadas directas entre ViewModels o pantallas sin mediador (servicio compartido o mensajería).
- **Implementaciones públicas:** Servicios con `public class` cuando deben ser `internal sealed`. Solo las interfaces son `public`.
- **Ignorar convenciones de estilo:** Usar `var` cuando está deshabilitado, omitir file-scoped namespaces, o dejar usings redundantes.
