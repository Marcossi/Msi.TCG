# Especificación técnica: Integración UI del Command Routing

## Propósito

Definir la integración de la UI con el sistema de Command Routing: toolbar en el editor y atajo de teclado Ctrl+S en el Shell.

## Componentes

### 1. Toolbar en TemplateEditorShellView

**Ubicación:** `UI/Views/TemplateEditor/TemplateEditorShellView.axaml`

**Diseño:** Barra horizontal en la parte superior del editor con botón "Guardar".

```xml
<UserControl x:Class="Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.TemplateEditorShellView"
              x:ClassModifier="internal"
              xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
              xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
              xmlns:local="clr-namespace:Msi.TemplateCodeGenerator.UI.Views.TemplateEditor"
              mc:Ignorable="d" 
              d:DesignHeight="450" d:DesignWidth="800">

    <DockPanel>
        <!-- Toolbar -->
        <Border DockPanel.Dock="Top" 
                Background="#F0F0F0" 
                Padding="4"
                BorderBrush="#CCCCCC"
                BorderThickness="0,0,0,1">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <Button Content="💾 Guardar" 
                        Command="{Binding SaveCommand}"
                        ToolTip.Tip="Guardar (Ctrl+S)"
                        Padding="8,4"
                        MinWidth="80"/>
            </StackPanel>
        </Border>

        <!-- Editor + Preview -->
        <Grid ColumnDefinitions="*, 4, *">
            <!-- Editor (Izquierda) -->
            <TextBox Grid.Column="0"
                        Text="{Binding Content}"
                        AcceptsReturn="True"
                        AcceptsTab="True"
                        FontFamily="Consolas, Monospace, Courier New"
                        VerticalContentAlignment="Top"
                        PlaceholderText="Escribe tu plantilla Scriban aquí..."
                        BorderThickness="1"
                        BorderBrush="#CCCCCC"
                        CornerRadius="0"/>			
            
            <!-- Splitter -->
            <GridSplitter Grid.Column="1" 
                        ResizeDirection="Columns" 
                        Background="#DDDDDD" />

            <!-- Preview (Derecha) -->
            <TextBox Grid.Column="2"
                    Text="{Binding PreviewContent}"
                    IsReadOnly="True"
                    AcceptsReturn="True"
                    FontFamily="Consolas, Monospace, Courier New"
                    VerticalContentAlignment="Top"
                    PlaceholderText="Vista previa en tiempo real..."
                    BorderThickness="1"
                    BorderBrush="#CCCCCC"
                    CornerRadius="0"/>
        </Grid>
    </DockPanel>
</UserControl>
```

**Notas de diseño:**
- Avalonia no tiene un control `ToolBar` nativo como WPF. Se usa un `Border` con `StackPanel` y `Button`s.
- El botón se bindea a `SaveCommand` del ViewModel (que ya existe en `BaseTextEditorViewModel`).
- El tooltip indica el atajo de teclado para feedback al usuario.
- Estilo minimalista con fondo gris claro y borde inferior.

### 2. Atajo de teclado Ctrl+S en MainShellView

**Ubicación:** `UI/Views/Shell/MainShellView.axaml`

**Diseño:** KeyBinding global en el Shell que invoca `SaveCommand` del `MainShellViewModel`.

```xml
<UserControl x:Class="Msi.TemplateCodeGenerator.UI.Views.Shell.MainShellView"
              xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
              xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
              xmlns:local="clr-namespace:Msi.TemplateCodeGenerator.UI.Views.Shell"
              xmlns:vm="clr-namespace:Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels"
              xmlns:dock="clr-namespace:Dock.Avalonia.Controls;assembly=Dock.Avalonia"
              xmlns:dockMvvm="clr-namespace:Dock.Model.Mvvm.Controls;assembly=Dock.Model.Mvvm"
              mc:Ignorable="d"
              x:ClassModifier="internal"
              d:DesignHeight="450" d:DesignWidth="800">

    <UserControl.KeyBindings>
        <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>
    </UserControl.KeyBindings>

    <DockPanel>
        <!-- Barra de menus -->
        <Menu DockPanel.Dock="Top">
            <MenuItem Header="_Archivo">
                <MenuItem Header="_Nuevo Proyecto..." Command="{Binding NewProjectCommand}"/>
                <MenuItem Header="_Abrir Proyecto..." Command="{Binding OpenProjectCommand}"/>
                <MenuItem Header="_Cerrar Proyecto" Command="{Binding CloseProjectCommand}"/>
                <Separator />
                <MenuItem Header="_Guardar" Command="{Binding SaveCommand}" InputGesture="Ctrl+S"/>
                <MenuItem Header="Guardar _Como..." Command="{Binding SaveProjectAsCommand}"/>
                <Separator />
                <MenuItem Header="_Salir" Command="{Binding ExitCommand}"/>
            </MenuItem>
        </Menu>

        <!-- Layout de paneles gestionado por Dock.Avalonia -->
        <dock:DockControl Layout="{Binding Layout}">
            <dock:DockControl.DataTemplates>
                <DataTemplate DataType="{x:Type dockMvvm:Tool}">
                    <ContentControl Content="{Binding Context}" />
                </DataTemplate>
                <DataTemplate DataType="{x:Type dockMvvm:Document}">
                    <ContentControl Content="{Binding Context}" />
                </DataTemplate>
            </dock:DockControl.DataTemplates>
        </dock:DockControl>
    </DockPanel>
</UserControl>
```

**Notas de diseño:**
- `UserControl.KeyBindings` define atajos globales en el Shell.
- `Gesture="Ctrl+S"` es la sintaxis de Avalonia para keybindings.
- El menú "Guardar" muestra `InputGesture="Ctrl+S"` como feedback visual.
- El `SaveCommand` del Shell delega en `ICommandRegistry.ExecuteAsync("Save")`.

### 3. Cambios en MainShellViewModel

**Ubicación:** `UI/Views/Shell/ViewModels/MainShellViewModel.cs`

**Cambios:**
1. Inyectar `ICommandRegistry`.
2. Añadir `SaveCommand` contextual que delega en el registry.
3. Mantener `SaveProjectCommand` como comando global separado.

```csharp
internal partial class MainShellViewModel : BaseViewModel
{
    private readonly IProjectService _projectService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ICommandRegistry _commandRegistry;
    private readonly ILogger<MainShellViewModel> _logger;

    [ObservableProperty]
    private IRootDock? _layout;

    [ObservableProperty]
    private string? _statusMessage;

    public MainShellViewModel(
        INavigationService navigationService,
        IProjectService projectService,
        IFileDialogService fileDialogService,
        ICommandRegistry commandRegistry,
        ILogger<MainShellViewModel> logger)
    {
        _projectService = projectService;
        _fileDialogService = fileDialogService;
        _commandRegistry = commandRegistry;
        _logger = logger;
        _layout = navigationService.GetLayout();
    }

    // ... comandos existentes (NewProject, OpenProject, etc.) ...

    /// <summary>
    /// Guarda el contexto activo (editor de archivos).
    /// Delega en ICommandRegistry para resolver el comando contextual.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        _logger.LogInformation("[UI] Command: Save (contextual)");
        try
        {
            bool executed = await _commandRegistry.ExecuteAsync("Save");
            if (!executed)
            {
                _logger.LogDebug("No se pudo ejecutar Save contextual (sin editor activo o sin cambios)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing Save (contextual)");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Comprueba si el comando Save contextual puede ejecutarse.
    /// </summary>
    private bool CanSave() => _commandRegistry.CanExecute("Save");

    /// <summary>
    /// Guarda el proyecto actual (comando global, no contextual).
    /// </summary>
    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        _logger.LogInformation("[UI] Command: SaveProject");
        try
        {
            await _projectService.SaveProjectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Error executing SaveProject");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ... resto de comandos existentes ...
}
```

**Notas de diseño:**
- `SaveCommand` (contextual) y `SaveProjectCommand` (global) son comandos separados.
- `CanSave()` consulta al registry para habilitar/deshabilitar el botón y el menú.
- El menú "Guardar" ahora invoca `SaveCommand` (contextual), no `SaveProjectCommand`.
- Si se necesita un menú "Guardar Proyecto" separado, se puede añadir como otro entry.

### 4. Cambios en BaseTextEditorViewModel

**Ubicación:** `UI/Views/TemplateEditor/ViewModels/BaseTextEditorViewModel.cs`

**Cambios:**
1. Implementar `ICommandRoute`.
2. El `SaveCommand` existente se mantiene para uso directo desde la toolbar del editor.
3. Añadir `CanExecute` y `ExecuteAsync` para el routing.

```csharp
internal abstract partial class BaseTextEditorViewModel(
    IFileService fileService,
    IDialogService dialogService,
    ILogger<BaseTextEditorViewModel> logger)
    : BaseViewModel, ICloseAware, ICommandRoute
{
    // ... propiedades y métodos existentes ...

    #region ICommandRoute

    /// <inheritdoc/>
    public bool CanExecute(string commandName) => commandName switch
    {
        "Save" => CanSave(),
        _ => false
    };

    /// <inheritdoc/>
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

    #endregion

    // ... resto del código existente ...
}
```

**Notas de diseño:**
- `ICommandRoute` permite que el registry invoque comandos en este VM.
- `SaveCommand` (RelayCommand) se mantiene para la toolbar del editor.
- `ExecuteAsync("Save")` delega en `SaveAsync()` (el método privado del RelayCommand).

### 5. Cambios en NavigationService

**Ubicación:** `UI/Services/Navigation/NavigationService.cs`

**Cambios:**
1. Implementar `ICommandContext`.
2. Actualizar `ActiveRoute` cuando cambia el dockable activo.

```csharp
internal sealed class NavigationService(
    IServiceProvider serviceProvider,
    ILogger<NavigationService> logger) : INavigationService, ICommandContext
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<NavigationService> _logger = logger;
    private IRootDock? _rootDock;

    /// <inheritdoc/>
    public ICommandRoute? ActiveRoute { get; private set; }

    // ... métodos existentes de INavigationService ...

    /// <summary>
    /// Actualiza el contexto activo cuando cambia el dockable activo en Dock.Avalonia.
    /// </summary>
    private void OnActiveDockableChanged(IDockable? dockable)
    {
        ActiveRoute = dockable?.Context as ICommandRoute;
        _logger.LogDebug("Contexto activo cambiado: {ActiveRoute}", 
            ActiveRoute?.GetType().Name ?? "null");
    }

    // ... resto del código existente ...
}
```

**Notas de diseño:**
- `NavigationService` implementa ambas interfaces (`INavigationService` e `ICommandContext`).
- `ActiveRoute` se actualiza automáticamente cuando Dock.Avalonia cambia el documento activo.
- El mecanismo exacto de detección de cambio de foco depende de la API de Dock.Avalonia (consultar `docs/agents/libraries-doc/Dock-12.0.0.2/index.md`).

### 6. Cambios en DependencyInjection.cs

**Ubicación:** `DependencyInjection.cs`

**Cambios:**
1. Registrar `NavigationService` como ambas interfaces.
2. Registrar `CommandRegistry`.

```csharp
public static IServiceCollection AddTemplateCodeGeneratorServices(this IServiceCollection services)
{
    //----------------------
    // Infraestructura (UI)
    //----------------------
    services.AddSingleton<MainWindow>();

    // Registrar los ViewModels de las "páginas".
    services.AddSingleton<MainShellViewModel>();
    services.AddSingleton<SettingsShellViewModel>();

    // Registrar dock: AppDockFactory (interna) + INavigationService + ICommandContext (pública)
    services.AddSingleton<AppDockFactory>();
    services.AddSingleton<NavigationService>();
    services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
    services.AddSingleton<ICommandContext>(sp => sp.GetRequiredService<NavigationService>());

    // Registrar Command Registry
    services.AddSingleton<ICommandRegistry, CommandRegistry>();

    // Registrar Dock: tipos de Tools
    services.AddSingleton<ProjectExplorerShellViewModel>();

    // Registrar Dock: tipos de Document
    services.AddScoped<TemplateEditorShellViewModel>();

    // ... resto del registro existente ...
}
```

**Notas de diseño:**
- `NavigationService` se registra como Singleton y se expone vía ambas interfaces.
- `CommandRegistry` se registra como Singleton.

### 7. Nuevos ficheros a crear

**Interfaces:**
- `Interfaces/ICommandContext.cs`
- `Interfaces/ICommandRoute.cs`
- `Interfaces/ICommandRegistry.cs`

**Implementaciones:**
- `UI/Services/Commands/CommandRegistry.cs`

**Tests:**
- `test/Msi.TemplateCodeGenerator.Tests/UI/Services/Commands/CommandRegistryTests.cs`
- `test/Msi.TemplateCodeGenerator.Tests/UI/Views/TemplateEditor/ViewModels/BaseTextEditorViewModelCommandTests.cs`
- `test/Msi.TemplateCodeGenerator.Tests/UI/Services/Commands/CommandRoutingIntegrationTests.cs`

## Flujo completo de Save

### Escenario 1: Usuario presiona Ctrl+S con editor activo

```
1. Usuario presiona Ctrl+S
2. MainShellView.KeyBindings captura el gesto e invoca MainShellViewModel.SaveCommand
3. MainShellViewModel.SaveCommand consulta ICommandRegistry.CanExecute("Save")
4. CommandRegistry pregunta a ICommandContext.ActiveRoute
5. ActiveRoute es BaseTextEditorViewModel (el editor activo)
6. BaseTextEditorViewModel.CanExecute("Save") → true (si IsDirty && FilePath != "")
7. CommandRegistry ejecuta ActiveRoute.ExecuteAsync("Save")
8. BaseTextEditorViewModel.SaveAsync() → guarda el archivo vía IFileService
9. IsDirty se limpia → CanExecute("Save") devuelve false → botón se deshabilita
```

### Escenario 2: Usuario hace clic en botón "Guardar" de la toolbar del editor

```
1. Usuario hace clic en el botón "Guardar" de TemplateEditorShellView
2. El botón está bindeado a BaseTextEditorViewModel.SaveCommand (RelayCommand)
3. SaveCommand ejecuta SaveAsync() directamente (sin pasar por el registry)
4. SaveAsync() → guarda el archivo vía IFileService
5. IsDirty se limpia → SaveCommand se deshabilita (CanSave() = false)
```

### Escenario 3: Usuario presiona Ctrl+S sin editor activo

```
1. Usuario presiona Ctrl+S
2. MainShellView.KeyBindings captura el gesto e invoca MainShellViewModel.SaveCommand
3. MainShellViewModel.SaveCommand consulta ICommandRegistry.CanExecute("Save")
4. CommandRegistry pregunta a ICommandContext.ActiveRoute
5. ActiveRoute es null (no hay editor activo)
6. CommandRegistry.CanExecute("Save") → false
7. MainShellViewModel.SaveCommand no se ejecuta (botón deshabilitado)
```

## Consideraciones de testing

Ver `docs/agents/proyecto/especificaciones/command-routing-testing.md` para el plan de pruebas detallado.

**Pruebas adicionales para la integración UI:**
- Verificar que el toolbar del editor se muestra correctamente.
- Verificar que el botón "Guardar" se habilita/deshabilita según `IsDirty`.
- Verificar que Ctrl+S invoca el comando correcto.
- Verificar que el menú "Guardar" muestra el InputGesture "Ctrl+S".

## Referencias

- ADR: `docs/agents/proyecto/adr/ADR-001-command-routing.md`
- Especificación de Command Routing: `docs/agents/proyecto/especificaciones/command-routing.md`
- Plan de pruebas: `docs/agents/proyecto/especificaciones/command-routing-testing.md`
- Documentación de Dock.Avalonia: `docs/agents/libraries-doc/Dock-12.0.0.2/index.md`
