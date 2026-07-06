# NavigationService Simple para Avalonia

> Patron de navegacion para shells con tabs o paneles simples. Para shells con docking IDE-like, consultar `msi-navigation-docking.md`.

## Contexto

Este patron aplica a aplicaciones con una shell que navega entre vistas mediante tabs, paneles o content switching simple. No usa Dock.Avalonia ni documentos editables.

## Estructura de archivos

```text
<App>/
├── Interfaces/
│   └── INavigationService.cs
└── UI/
    └── Services/
        └── Navigation/
            └── NavigationService.cs
```

### Regla de ubicacion

- La **interfaz** `INavigationService` se coloca en `Interfaces/`.
- La **implementacion** se coloca en `UI/Services/Navigation/`.
- En proyectos single-project, todas las interfaces van en `Interfaces/` (raiz).
- En proyectos multi-ensamblado, las interfaces de dominio van en `Domain/Interfaces/` y las de UI en `UI/Interfaces/`.

## Interfaz INavigationService

```csharp
namespace MyApp.Interfaces;

public interface INavigationService
{
    void NavigateToHome();
    void NavigateToSettings();
    void NavigateToDetails();
}
```

### Regla de metodos NavigateTo<Vista>()

- Cada vista navegable tiene un metodo `NavigateTo<Vista>()` correspondiente.
- Se **excluye** `MainWindow` de la navegacion (es la shell, no un destino).
- Los nombres siguen el patron `NavigateTo<Vista>()`.

### Regla de mapeo vista a metodo

Para cada vista navegable:
1. La screen vive en `UI/Views/<Screen>/`.
2. El ViewModel se llama `<Screen>ViewModel` y vive en `UI/Views/<Screen>/ViewModels/`.
3. La interfaz expone `NavigateTo<Screen>()`.
4. La implementacion llama a `_shell.SelectTarget("<Screen>")`.

## Implementacion NavigationService

```csharp
using MyApp.Interfaces;

namespace MyApp.UI.Services.Navigation;

internal sealed class NavigationService : INavigationService
{
    private readonly MainWindow _shell;

    public NavigationService(MainWindow shell)
    {
        _shell = shell;
    }

    public void NavigateToHome()
    {
        _shell.SelectTarget("Home");
    }

    public void NavigateToSettings()
    {
        _shell.SelectTarget("Settings");
    }

    public void NavigateToDetails()
    {
        _shell.SelectTarget("Details");
    }
}
```

### Consideraciones

- El constructor recibe la `MainWindow` como shell.
- `SelectTarget(targetId)` encapsula la activacion del destino en la shell.
- La infraestructura concreta puede ser tabs, paneles o cualquier mecanismo de content switching.

## MainWindow.SelectTarget()

```csharp
public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void SelectTarget(string targetId)
    {
        if (_viewModel is null) return;
        _viewModel.ActivateTarget(targetId);
    }
}
```

## Registro en IoC

```csharp
services.AddSingleton<INavigationService, NavigationService>();
```

- **Singleton**: servicio de UI compartido, sin estado.
- Si la shell se registra en el contenedor, la inyeccion directa es valida.
- Si la shell no debe inyectarse directamente, introducir una abstraccion intermedia.

## Flujo de navegacion

1. Un ViewModel inyecta `INavigationService`.
2. Llama a `NavigateToHome()`, `NavigateToSettings()`, etc.
3. `NavigationService` llama a `_shell.SelectTarget()`.
4. La shell delega en su ViewModel la activacion del destino.
5. La UI actualiza la vista via binding.

## Antipatrones

- Navegar con referencias directas entre ViewModels.
- Inyectar `Window` sin razon arquitectonica clara.
- Poner logica de negocio en el servicio de navegacion.
- Exponer `MainWindow` como destino de navegacion.
- Crear una nueva instancia de la ventana principal en cada navegacion.

## Actualizacion al anadir una nueva screen

1. Crear la screen en `UI/Views/<Screen>/`.
2. Anadir `NavigateTo<Screen>()` a `INavigationService`.
3. Implementar en `NavigationService`.
4. Registrar el destino en la shell.
