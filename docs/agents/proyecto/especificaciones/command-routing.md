# Especificación técnica: Command Routing

## Propósito

Definir la arquitectura de Command Routing para invocar comandos contextuales según el documento/tool activo en el shell de Dock.Avalonia.

## Componentes

### 1. `ICommandContext`

**Responsabilidad:** Exponer el documento/tool activo que puede manejar comandos contextuales.

**Ubicación:** `Interfaces/ICommandContext.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Contexto de comandos. Expone el documento o tool activo que puede manejar comandos contextuales.
/// Se integra con INavigationService para trackear el foco en el layout de Dock.Avalonia.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    /// Obtiene la ruta de comandos activa (el VM del documento/tool con foco).
    /// Devuelve null si no hay documento activo o el activo no implementa ICommandRoute.
    /// </summary>
    ICommandRoute? ActiveRoute { get; }
}
```

**Integración con `INavigationService`:**

`NavigationService` (implementación de `INavigationService`) debe implementar también `ICommandContext`. Al cambiar el documento activo en Dock.Avalonia, se actualiza `ActiveRoute`.

```csharp
internal sealed class NavigationService : INavigationService, ICommandContext
{
    public ICommandRoute? ActiveRoute { get; private set; }
    
    // Al activar un dockable en Dock.Avalonia:
    private void OnActiveDockableChanged(IDockable? dockable)
    {
        ActiveRoute = dockable?.Context as ICommandRoute;
    }
}
```

**Registro en DI:**

`INavigationService` ya está registrado como Singleton. Como `NavigationService` implementa ambas interfaces, se registra como:

```csharp
services.AddSingleton<NavigationService>();
services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
services.AddSingleton<ICommandContext>(sp => sp.GetRequiredService<NavigationService>());
```

### 2. `ICommandRoute`

**Responsabilidad:** Interfaz que los ViewModels implementan para exponer comandos contextuales.

**Ubicación:** `Interfaces/ICommandRoute.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Ruta de comandos contextuales. Los ViewModels que implementan esta interfaz
/// pueden manejar comandos invocados por nombre (ej: "Save", "Copy", "Paste").
/// </summary>
public interface ICommandRoute
{
    /// <summary>
    /// Comprueba si el comando especificado puede ejecutarse en el estado actual.
    /// </summary>
    /// <param name="commandName">Nombre del comando (ej: "Save").</param>
    /// <returns>true si el comando puede ejecutarse; false en caso contrario.</returns>
    bool CanExecute(string commandName);

    /// <summary>
    /// Ejecuta el comando especificado.
    /// </summary>
    /// <param name="commandName">Nombre del comando (ej: "Save").</param>
    /// <exception cref="InvalidOperationException">Si el comando no está soportado o no puede ejecutarse.</exception>
    Task ExecuteAsync(string commandName);
}
```

**Implementación en `BaseTextEditorViewModel`:**

```csharp
internal abstract partial class BaseTextEditorViewModel : BaseViewModel, ICloseAware, ICommandRoute
{
    public bool CanExecute(string commandName) => commandName switch
    {
        "Save" => CanSave(),
        _ => false
    };

    public async Task ExecuteAsync(string commandName)
    {
        switch (commandName)
        {
            case "Save":
                await SaveAsync();
                break;
            default:
                throw new InvalidOperationException($"Comando no soportado: {commandName}");
        }
    }

    // SaveCommand existente se mantiene para uso directo desde la UI del editor
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() { ... }
}
```

### 3. `ICommandRegistry`

**Responsabilidad:** Resolver y ejecutar comandos por nombre consultando al contexto activo.

**Ubicación:** `Interfaces/ICommandRegistry.cs`

```csharp
namespace Msi.TemplateCodeGenerator.Interfaces;

/// <summary>
/// Registro de comandos. Resuelve comandos por nombre consultando al contexto activo.
/// Actúa como intermediario entre la UI (menú, toolbar, keybindings) y los ViewModels.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Comprueba si el comando especificado puede ejecutarse en el contexto actual.
    /// </summary>
    bool CanExecute(string commandName);

    /// <summary>
    /// Ejecuta el comando especificado en el contexto activo.
    /// </summary>
    /// <returns>true si el comando se ejecutó; false si no hay contexto activo o no puede ejecutarse.</returns>
    Task<bool> ExecuteAsync(string commandName);
}
```

**Implementación:**

```csharp
namespace Msi.TemplateCodeGenerator.UI.Services.Commands;

internal sealed class CommandRegistry(
    ICommandContext commandContext,
    ILogger<CommandRegistry> logger) : ICommandRegistry
{
    private readonly ICommandContext _commandContext = commandContext;
    private readonly ILogger<CommandRegistry> _logger = logger;

    public bool CanExecute(string commandName)
    {
        ICommandRoute? route = _commandContext.ActiveRoute;
        if (route is null)
        {
            _logger.LogDebug("No hay contexto activo para ejecutar '{CommandName}'", commandName);
            return false;
        }

        bool canExecute = route.CanExecute(commandName);
        _logger.LogDebug("CanExecute('{CommandName}') = {CanExecute} en {ActiveRoute}", 
            commandName, canExecute, route.GetType().Name);
        return canExecute;
    }

    public async Task<bool> ExecuteAsync(string commandName)
    {
        ICommandRoute? route = _commandContext.ActiveRoute;
        if (route is null)
        {
            _logger.LogWarning("No hay contexto activo para ejecutar '{CommandName}'", commandName);
            return false;
        }

        if (!route.CanExecute(commandName))
        {
            _logger.LogWarning("El comando '{CommandName}' no puede ejecutarse en el contexto actual", commandName);
            return false;
        }

        _logger.LogInformation("[UI] Command: {CommandName} (contextual)", commandName);
        await route.ExecuteAsync(commandName);
        return true;
    }
}
```

**Registro en DI:**

```csharp
services.AddSingleton<ICommandRegistry, CommandRegistry>();
```

## Flujo de ejecución

### Ejemplo: Usuario presiona Ctrl+S (o hace clic en menú "Guardar")

```
1. UI (Menu/KeyBinding) invoca MainShellViewModel.SaveCommand
2. MainShellViewModel.SaveCommand consulta ICommandRegistry.CanExecute("Save")
3. CommandRegistry pregunta a ICommandContext.ActiveRoute
4. Si hay un editor activo (BaseTextEditorViewModel):
   a. ActiveRoute.CanExecute("Save") → true (si IsDirty && FilePath != "")
   b. CommandRegistry ejecuta ActiveRoute.ExecuteAsync("Save")
   c. BaseTextEditorViewModel.SaveAsync() → guarda el archivo
5. Si no hay editor activo:
   a. ActiveRoute es null → CanExecute devuelve false
   b. MainShellViewModel puede hacer fallback a SaveProjectCommand (opcional)
```

## Integración con la UI

### Menú en `MainShellView.axaml`

```xml
<Menu DockPanel.Dock="Top">
    <MenuItem Header="_Archivo">
        <MenuItem Header="_Nuevo Proyecto..." Command="{Binding NewProjectCommand}"/>
        <MenuItem Header="_Abrir Proyecto..." Command="{Binding OpenProjectCommand}"/>
        <MenuItem Header="_Cerrar Proyecto" Command="{Binding CloseProjectCommand}"/>
        <Separator />
        <MenuItem Header="_Guardar" Command="{Binding SaveCommand}"/>
        <MenuItem Header="Guardar _Como..." Command="{Binding SaveProjectAsCommand}"/>
        <Separator />
        <MenuItem Header="_Salir" Command="{Binding ExitCommand}"/>
    </MenuItem>
</Menu>
```

### Comandos en `MainShellViewModel`

```csharp
internal partial class MainShellViewModel : BaseViewModel
{
    private readonly ICommandRegistry _commandRegistry;

    // Comandos globales (proyecto)
    [RelayCommand]
    private async Task SaveProjectAsync() { ... }

    // Comandos contextuales (delegan en el registry)
    [RelayCommand(CanExecute = nameof(CanSaveContextual))]
    private async Task SaveAsync()
    {
        _logger.LogInformation("[UI] Command: Save (contextual)");
        bool executed = await _commandRegistry.ExecuteAsync("Save");
        if (!executed)
        {
            _logger.LogWarning("No se pudo ejecutar Save contextual");
        }
    }

    private bool CanSaveContextual() => _commandRegistry.CanExecute("Save");
}
```

### KeyBindings (futuro)

```xml
<UserControl.KeyBindings>
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>
</UserControl.KeyBindings>
```

## Reglas de diseño

### 1. Comandos separados
- **Globales**: `SaveProjectCommand`, `OpenProjectCommand`, etc. (en el Shell)
- **Contextuales**: `Save`, `Copy`, `Paste`, etc. (en documentos/tools vía `ICommandRoute`)

### 2. Lifecycle automático
- No hay registro/unregister explícito.
- `CommandRegistry` consulta dinámicamente a `ICommandContext.ActiveRoute`.
- El VM solo implementa `ICommandRoute`; el cambio de foco lo maneja `NavigationService`.

### 3. Audit Trail
- Todo comando contextual loguea `[UI] Command: {CommandName} (contextual)` en `CommandRegistry.ExecuteAsync`.
- Los VMs loguean su propia lógica interna (ej: `[UI] Editor: Guardando '{FilePath}'`).

### 4. Error handling
- `CommandRegistry.ExecuteAsync` devuelve `false` si no hay contexto o no puede ejecutarse.
- Los VMs deben manejar excepciones internamente (try-catch en `SaveAsync`).

## Extensibilidad

### Añadir un nuevo comando contextual

1. **Definir el nombre del comando** (ej: "Copy").
2. **Implementar `ICommandRoute`** en el VM que lo soporta:
   ```csharp
   public bool CanExecute(string commandName) => commandName switch
   {
       "Copy" => HasSelection,
       _ => false
   };

   public async Task ExecuteAsync(string commandName)
   {
       switch (commandName)
       {
           case "Copy":
               await CopyToClipboardAsync();
               break;
       }
   }
   ```
3. **Exponer en el Shell** (opcional):
   ```csharp
   [RelayCommand(CanExecute = nameof(CanCopy))]
   private async Task CopyAsync() => await _commandRegistry.ExecuteAsync("Copy");

   private bool CanCopy() => _commandRegistry.CanExecute("Copy");
   ```
4. **Añadir al menú/toolbar** (opcional):
   ```xml
   <MenuItem Header="_Copiar" Command="{Binding CopyCommand}"/>
   ```

## Consideraciones de testing

Ver `docs/agents/proyecto/especificaciones/command-routing-testing.md` para el plan de pruebas detallado.

## Migración del código existente

### Cambios en `BaseTextEditorViewModel`

1. Implementar `ICommandRoute`.
2. Mantener `SaveCommand` para uso directo desde la UI del editor (si es necesario).
3. Exponer `SaveAsync` vía `ExecuteAsync("Save")`.

### Cambios en `NavigationService`

1. Implementar `ICommandContext`.
2. Actualizar `ActiveRoute` cuando cambia el dockable activo en Dock.Avalonia.

### Cambios en `MainShellViewModel`

1. Inyectar `ICommandRegistry`.
2. Añadir `SaveCommand` contextual que delega en el registry.
3. Mantener `SaveProjectCommand` como comando global.

### Cambios en `DependencyInjection.cs`

```csharp
// Registrar NavigationService como ambas interfaces
services.AddSingleton<NavigationService>();
services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
services.AddSingleton<ICommandContext>(sp => sp.GetRequiredService<NavigationService>());

// Registrar CommandRegistry
services.AddSingleton<ICommandRegistry, CommandRegistry>();
```

## Referencias

- ADR: `docs/agents/proyecto/adr/ADR-001-command-routing.md`
- Plan de pruebas: `docs/agents/proyecto/especificaciones/command-routing-testing.md`
- Documentación de Dock.Avalonia: `docs/agents/libraries-doc/Dock-12.0.0.2/index.md`
