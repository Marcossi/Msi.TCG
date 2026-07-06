# MSI Guidelines - Bootstrap y Hosting .NET

> Capa reutilizable y agnóstica. Define la secuencia canónica de arranque, inicialización de infraestructura, configuración y ciclo de vida de la aplicación.

## 1. Regla de Arranque Base
- Toda aplicación debe estructurarse alrededor de un Host genérico de .NET (`HostApplicationBuilder` o equivalente moderno), independientemente de si el runtime final es una aplicación de Consola, un Servicio Web o un entorno Desktop.

## 2. Secuencia Canónica de Inicialización
Al arrancar el proceso de la aplicación, se debe seguir estrictamente este orden secuencial:
1. Instanciar el constructor del host (`Host.CreateApplicationBuilder`)[cite: 4].
2. Cargar los archivos de configuración externa (`appsettings.json`)[cite: 4].
3. Configurar e inicializar el sistema de logging estructurado[cite: 4].
4. Registrar los servicios de la aplicación en el contenedor de inversión de control (IoC)[cite: 4].
5. Construir (`Build()`) e iniciar el ciclo de vida del host[cite: 4].
6. Resolver el punto de entrada o servicio inicial desde el contenedor construido para arrancar la ejecución[cite: 4].

## 3. Configuración del Sistema
- La configuración técnica se lee de entornos declarativos como `appsettings.json`[cite: 4].
- Queda prohibido cablear credenciales, rutas físicas rígidas o parámetros de infraestructura en código duro (*hardcoded*)[cite: 4].
- Se habilita `reloadOnChange: true` en entornos de desarrollo para agilizar las pruebas de configuración en caliente[cite: 4].

## 4. Infraestructura de Logging (Serilog)
- El proveedor oficial de logging estructurado para todo el ecosistema es **Serilog**[cite: 4, 5].
- **Secuencia de inicialización de logs:**
  1. Instanciar `LoggerConfiguration`[cite: 4].
  2. Leer las directivas de sumideros (*sinks*) y niveles desde la configuración cargada del constructor[cite: 4].
  3. Limpiar los proveedores por defecto del framework para evitar duplicidades de salida[cite: 4].
  4. Registrar Serilog de forma nativa en el constructor de la aplicación[cite: 4].
- En entornos locales de desarrollo, se mantendrá obligatoriamente una salida duplicada: una por consola para depuración rápida y otra hacia un archivo físico estable y controlado en el directorio de salida[cite: 4, 5].

### Regla de ILogger<T> obligatorio

Todo servicio registrado en el contenedor debe inyectar `ILogger<T>` en su constructor. El logging no es opcional:

```csharp
internal sealed class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }
}
```

Un servicio sin logging es un servicio que no se puede diagnosticar en producción.

## 5. Gestión del Contenedor e Inversión de Control (IoC)

### 5.1 Fichero DependencyInjection.cs por Proyecto

- **Obligatorio:** Cada proyecto de la solución (excluyendo proyectos de test) debe contener un fichero `DependencyInjection.cs` en su **raíz**.
- Cada fichero contiene un **único** método de extensión público con la nomenclatura exacta: `Add<NombreProyecto>Services`.
- `NombreProyecto` = nombre del ensamblado sin extensión (ej. `Stt.Schedules.Domain` → `AddDomainServices`).
- **Visibilidad:** Solo las interfaces son `public`. Las implementaciones deben ser `internal`.

### 5.2 Invocación Secuencial en Program.cs

- `Program.cs` debe invocar cada método de extensión **una única vez**, de forma secuencial, en orden de dependencia:
  ```csharp
  services.AddDomainServices();
  services.AddInfrastructureServices();
  services.AddApplicationServices();
  ```
- **PROHIBIDO** que un método `AddXxxServices()` llame internamente a otro `AddYyyServices()`. Cada proyecto es responsable de sus propios registros. Si el proyecto B depende del proyecto A, la invocación se hace en `Program.cs` antes del `AddBServices()`, no dentro del código de B.
- **PROHIBIDO** el encadenamiento interno. Cada invocación en `Program.cs` es una línea independiente.

### 5.3 Lifetimes Intencionales

- `Transient`: Servicios ligeros y sin estado.
- `Scoped`: Operaciones acotadas a un contexto temporal.
- `Singleton`: Exclusivamente para servicios globales compartidos sin estado de sesión crítico[cite: 4].

#### Regla de Scoped con IServiceScope

Un servicio Scoped **requiere** un `IServiceScope` explícito. Resolver un Scoped desde el root provider sin crear scope es un error que convierte el Scoped en Singleton efectivo:

```csharp
// CORRECTO
using IServiceScope scope = _serviceProvider.CreateScope();
MyScopedService service = scope.ServiceProvider.GetRequiredService<MyScopedService>();

// INCORRECTO — el Scoped se comporta como Singleton
MyScopedService service = _serviceProvider.GetRequiredService<MyScopedService>();
```

## 6. Ciclo de Vida y Diagnóstico
- **Liberación:** Al cerrarse la aplicación, es imperativo asegurar la detención controlada del host y disposición (`Dispose`) de los recursos del contenedor de dependencias[cite: 4].
- **Banners:** Se permite la impresión por consola de un banner informativo con la versión del software en el arranque, siempre que no interfiera con los streams de datos de la infraestructura[cite: 4].

### Regla de error handling en bootstrap

El bootstrap debe envolver la resolución del entry point en try-catch con logging. **Prohibido** `catch {}` vacío:

```csharp
try
{
    MainWindow mainWindow = services.GetRequiredService<MainWindow>();
    mainWindow.Show();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Error al inicializar la aplicación");
    throw;
}
```

Todo catch debe loguear la excepción con nivel apropiado (`LogError` para errores recuperables, `LogCritical` para fallos de arranque).