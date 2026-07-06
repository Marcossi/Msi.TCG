# ADR-001: Command Routing con Active Route

## Estado

Accepted (2026-07-02)

## Contexto

La aplicación tiene múltiples puntos de entrada para comandos (menú, toolbar, atajos de teclado, menú contextual) y múltiples ViewModels que exponen comandos contextuales (ej: `SaveCommand` en editores de texto).

**Problema:**
- No hay un mecanismo centralizado para invocar comandos según el contexto activo.
- El Shell (menú) solo expone comandos globales (`SaveProjectCommand`), pero no comandos contextuales al documento activo (`SaveCommand` del editor).
- Añadir atajos de teclado (Ctrl+S) requiere que el Shell sepa qué comando ejecutar según el foco, lo que acopla el Shell a los ViewModels concretos.
- Sin un sistema extensible, cada nuevo comando contextual requerirá lógica condicional en el Shell.

**Fuerzas:**
- Necesidad de extensibilidad: Save es el primero, pero habrá más (Copy, Paste, Undo, Redo, etc.).
- Separación de responsabilidades: El Shell no debe conocer los ViewModels concretos.
- Testeabilidad: El sistema debe ser fácilmente testeable sin dependencias de Avalonia.
- Integración con Dock.Avalonia: El framework ya trackea el documento activo; debemos aprovecharlo.

## Decisión

Implementar un **Command Routing basado en Active Route** con los siguientes componentes:

### 1. `ICommandContext`
Servicio que expone el documento/tool activo. Extensión de `INavigationService` o servicio separado.

```csharp
public interface ICommandContext
{
    ICommandRoute? ActiveRoute { get; }
}
```

### 2. `ICommandRoute`
Interfaz que los ViewModels implementan para exponer comandos contextuales.

```csharp
public interface ICommandRoute
{
    bool CanExecute(string commandName);
    Task ExecuteAsync(string commandName);
}
```

### 3. `ICommandRegistry`
Servicio que resuelve comandos por nombre consultando al contexto activo.

```csharp
public interface ICommandRegistry
{
    Task<bool> ExecuteAsync(string commandName);
    bool CanExecute(string commandName);
}
```

### 4. Comandos separados
- **Comandos globales** (Shell): `SaveProjectCommand`, `OpenProjectCommand`, etc.
- **Comandos contextuales** (Documentos/Tools): `Save`, `Copy`, `Paste`, etc.

El menú puede tener ambos entries (`Save` y `SaveProject`) o el Shell puede hacer fallback si no hay contexto activo.

### 5. Lifecycle automático
El `CommandRegistry` consulta dinámicamente a `ICommandContext.ActiveRoute`. No hay registro/unregister explícito. El VM solo necesita implementar `ICommandRoute` y el registry pregunta al contexto.

## Alternativas consideradas

### Alternativa A: Chain of Responsibility
Cada VM se registra en una cadena. Al invocar un comando, se consulta la cadena hasta que un VM responda.

**Descartada porque:**
- Más compleja de implementar y depurar.
- Riesgo de múltiples respuestas si varios VMs registran el mismo comando.
- Active Route es suficiente para comandos contextuales al foco.

### Alternativa B: Messenger para comandos globales
Broadcast de mensajes como "SaveActiveDocument". El VM activo responde.

**Descartada porque:**
- Flujo implícito, difícil de depurar.
- No permite `CanExecute` (el Shell no sabe si el comando está disponible).
- Riesgo de memory leaks si los VMs no se desuscriben.

### Alternativa C: Comando inteligente en el Shell
`MainShellViewModel.SaveCommand` consulta si hay editor activo con cambios. Si hay → guarda el archivo. Si no → guarda el proyecto.

**Descartada porque:**
- Lógica condicional en el Shell (acoplamiento).
- No extensible: cada nuevo comando requiere más condicionales.
- Viola el principio de separación de responsabilidades.

## Consecuencias

### Positivas
- **Extensible**: Añadir nuevos comandos contextuales es trivial (implementar `ICommandRoute`).
- **Desacoplado**: El Shell no conoce los ViewModels concretos.
- **Testeable**: `CommandRegistry` y `CommandContext` son mockeables.
- **Keybindings declarativos**: Se pueden asociar a nombres de comandos.
- **Contexto explícito**: Fácil de depurar (¿qué comando está activo ahora?).

### Negativas
- **Complejidad adicional**: Introducir 3 interfaces nuevas (`ICommandContext`, `ICommandRoute`, `ICommandRegistry`).
- **Overhead de consulta**: Cada invocación de comando requiere consultar al contexto (mínimo, pero existe).
- **Curva de aprendizaje**: Los desarrolladores deben entender el patrón para implementar nuevos comandos.

### Riesgos mitigados
- **Lifecycle automático**: Evita leaks por olvido de unregister.
- **Comandos separados**: Evita lógica condicional en el Shell.
- **Integración con Dock.Avalonia**: `NavigationService` ya trackea el documento activo; solo necesitamos exponerlo vía `ICommandContext`.

## Referencias

- Especificación técnica: `docs/agents/proyecto/especificaciones/command-routing.md`
- Plan de pruebas: `docs/agents/proyecto/especificaciones/command-routing-testing.md`
