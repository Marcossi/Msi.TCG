# Plan de pruebas: Command Routing

## Propósito

Definir la estrategia de pruebas para el sistema de Command Routing, estableciendo una base que sirva como rutina para futuros comandos.

## Estrategia general

### Niveles de prueba

| Nivel | Qué se prueba | Herramientas | Frecuencia |
|-------|---------------|--------------|------------|
| **Unit - ViewModel** | `ICommandRoute.CanExecute` / `ExecuteAsync` de cada VM | xUnit + NSubstitute | Cada cambio |
| **Unit - Registry** | `CommandRegistry.Resolve` con distintos contextos | xUnit + NSubstitute | Cada cambio |
| **Unit - Context** | `CommandContext.ActiveRoute` tracking | xUnit + NSubstitute | Cada cambio |
| **Integration** | Flujo completo: menú → registry → VM → servicio | xUnit + NSubstitute | Cambios estructurales |

### Convenciones

- **Ubicación**: `test/Msi.TemplateCodeGenerator.Tests/`
- **Nomenclatura**: `<Componente>Tests.cs` (ej: `CommandRegistryTests.cs`, `BaseTextEditorViewModelCommandTests.cs`)
- **Estructura**: Arrange → Act → Assert (AAA)
- **Mocks**: NSubstitute para interfaces (`ICommandContext`, `IFileService`, etc.)

## Pruebas unitarias

### 1. `CommandRegistryTests`

**Responsabilidad:** Verificar que `CommandRegistry` resuelve correctamente los comandos según el contexto activo.

```csharp
public sealed class CommandRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoActiveRoute_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns((ICommandRoute?)null);
        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveRoute_ExecutesCommand()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(true);
        route.ExecuteAsync("Save").Returns(Task.CompletedTask);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeTrue();
        await route.Received(1).ExecuteAsync("Save");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanExecuteReturnsFalse_DoesNotExecute()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(false);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeFalse();
        await route.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }

    [Fact]
    public void CanExecute_WithNoActiveRoute_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns((ICommandRoute?)null);
        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = registry.CanExecute("Save");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_WithActiveRoute_DelegatesToRoute()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(true);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = registry.CanExecute("Save");

        // Assert
        result.Should().BeTrue();
        route.Received(1).CanExecute("Save");
    }
}
```

### 2. `BaseTextEditorViewModelCommandTests`

**Responsabilidad:** Verificar que `BaseTextEditorViewModel` implementa correctamente `ICommandRoute` para el comando "Save".

```csharp
public sealed class BaseTextEditorViewModelCommandTests
{
    [Fact]
    public void CanExecute_Save_WhenDirtyAndHasPath_ReturnsTrue()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.IsDirty = true;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_Save_WhenNotDirty_ReturnsFalse()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.IsDirty = false;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_Save_WhenNoPath_ReturnsFalse()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        vm.FilePath = string.Empty;
        vm.IsDirty = true;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_UnknownCommand_ReturnsFalse()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        // Act
        bool canExecute = vm.CanExecute("UnknownCommand");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Save_WhenCanExecute_WritesFile()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        fileService.WriteTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.Content = "test content";
        vm.IsDirty = true;

        // Act
        await vm.ExecuteAsync("Save");

        // Assert
        await fileService.Received(1).WriteTextAsync("C:\\test.scriban", "test content");
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileService, dialogService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.ExecuteAsync("UnknownCommand"));
    }

    // VM de prueba concreto (BaseTextEditorViewModel es abstracto)
    private sealed class TestTextEditorViewModel : BaseTextEditorViewModel
    {
        public TestTextEditorViewModel(
            IFileService fileService,
            IDialogService dialogService,
            ILogger<BaseTextEditorViewModel> logger)
            : base(fileService, dialogService, logger)
        {
        }
    }
}
```

### 3. `NavigationServiceCommandContextTests`

**Responsabilidad:** Verificar que `NavigationService` actualiza correctamente `ActiveRoute` al cambiar el dockable activo.

```csharp
public sealed class NavigationServiceCommandContextTests
{
    [Fact]
    public void ActiveRoute_WhenActiveDockableChanges_UpdatesRoute()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<NavigationService>>();
        var navigationService = new NavigationService(serviceProvider, logger);

        var route = Substitute.For<ICommandRoute>();
        var dockable = new Document { Context = route };

        // Act
        navigationService.SetActiveDockable(dockable); // Método interno expuesto para testing

        // Assert
        navigationService.ActiveRoute.Should().BeSameAs(route);
    }

    [Fact]
    public void ActiveRoute_WhenDockableHasNoContext_ReturnsNull()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<NavigationService>>();
        var navigationService = new NavigationService(serviceProvider, logger);

        var dockable = new Document { Context = null };

        // Act
        navigationService.SetActiveDockable(dockable);

        // Assert
        navigationService.ActiveRoute.Should().BeNull();
    }

    [Fact]
    public void ActiveRoute_WhenContextIsNotICommandRoute_ReturnsNull()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<NavigationService>>();
        var navigationService = new NavigationService(serviceProvider, logger);

        var context = new object(); // No implementa ICommandRoute
        var dockable = new Document { Context = context };

        // Act
        navigationService.SetActiveDockable(dockable);

        // Assert
        navigationService.ActiveRoute.Should().BeNull();
    }
}
```

## Pruebas de integración

### `CommandRoutingIntegrationTests`

**Responsabilidad:** Verificar el flujo completo desde el Shell hasta el servicio de dominio.

```csharp
public sealed class CommandRoutingIntegrationTests
{
    [Fact]
    public async Task SaveCommand_FromShell_WhenEditorActive_SavesFile()
    {
        // Arrange
        var fileService = Substitute.For<IFileService>();
        fileService.WriteTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var editorVm = new TestTextEditorViewModel(fileService, dialogService, logger);
        editorVm.FilePath = "C:\\test.scriban";
        editorVm.Content = "test content";
        editorVm.IsDirty = true;

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(editorVm);

        var registryLogger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, registryLogger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeTrue();
        await fileService.Received(1).WriteTextAsync("C:\\test.scriban", "test content");
        editorVm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_FromShell_WhenNoEditorActive_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns((ICommandRoute?)null);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeFalse();
    }

    // VM de prueba concreto
    private sealed class TestTextEditorViewModel : BaseTextEditorViewModel
    {
        public TestTextEditorViewModel(
            IFileService fileService,
            IDialogService dialogService,
            ILogger<BaseTextEditorViewModel> logger)
            : base(fileService, dialogService, logger)
        {
        }
    }
}
```

## Regla para futuros comandos

Todo nuevo comando debe incluir tests de:

1. **`CanExecute` en estado válido e inválido:**
   - Verificar que devuelve `true` cuando las precondiciones se cumplen.
   - Verificar que devuelve `false` cuando las precondiciones no se cumplen.

2. **`ExecuteAsync` con mocks de servicios:**
   - Verificar que invoca los servicios correctos con los parámetros correctos.
   - Verificar que actualiza el estado del VM (ej: `IsDirty = false` tras guardar).

3. **Error handling:**
   - Verificar que las excepciones se capturan y loguean (audit trail).
   - Verificar que el estado del VM se mantiene consistente tras un error.

### Ejemplo de checklist para un nuevo comando "Copy"

- [ ] `BaseTextEditorViewModelCommandTests.CanExecute_Copy_WhenHasSelection_ReturnsTrue`
- [ ] `BaseTextEditorViewModelCommandTests.CanExecute_Copy_WhenNoSelection_ReturnsFalse`
- [ ] `BaseTextEditorViewModelCommandTests.ExecuteAsync_Copy_WhenHasSelection_CopiesToClipboard`
- [ ] `CommandRegistryTests.ExecuteAsync_Copy_WithActiveRoute_ExecutesCommand`
- [ ] `CommandRoutingIntegrationTests.CopyCommand_FromShell_WhenEditorActive_CopiesToClipboard`

## Configuración del proyecto de tests

### Dependencias necesarias

```xml
<!-- test/Msi.TemplateCodeGenerator.Tests/Msi.TemplateCodeGenerator.Tests.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
</ItemGroup>
```

### Estructura de carpetas

```
test/Msi.TemplateCodeGenerator.Tests/
├── Services/
│   └── Project/
│       ├── ProjectServiceTests.cs
│       └── JsonProjectSerializerTests.cs
└── UI/
    ├── Services/
    │   └── Commands/
    │       ├── CommandRegistryTests.cs
    │       └── CommandRoutingIntegrationTests.cs
    └── Views/
        └── TemplateEditor/
            └── ViewModels/
                └── BaseTextEditorViewModelCommandTests.cs
```

## Ejecución de tests

```powershell
# Ejecutar todos los tests
dotnet test

# Ejecutar solo tests de Command Routing
dotnet test --filter "FullyQualifiedName~CommandRegistry"

# Ejecutar con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Referencias

- ADR: `.agents/proyecto/adr/ADR-001-command-routing.md`
- Especificación técnica: `.agents/proyecto/especificaciones/command-routing.md`
- Convenciones de testing: `.agents/msi-guidelines-dotnet/msi-base-dotnet.md` (sección de testing si existe)
