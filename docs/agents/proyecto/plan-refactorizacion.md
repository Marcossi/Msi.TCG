# Plan de Refactorización — Msi.TemplateCodeGenerator

> Documento de arquitectura. Define las 3 fases de refactorización con pasos concretos, ficheros afectados y criterios de validación. Un agente implantador ejecuta cada paso; un agente validador verifica los criterios al final de cada fase.

## Estado actual (resumen del diagnóstico)

| Severidad | Cantidad | Descripción |
|---|---|---|
| Crítico | 4 | UI framework en VM, DialogService sin owner, clases Dummy en servicio, 63 violaciones de `var` |
| Alto | 4 | Namespaces incorrectos, catch vacío, sin Unregister en mensajería, cero tests |
| Moderado | 5 | Logging ausente, sin error handling en comandos, accesibilidad incorrecta, Scoped mal resuelto, usings redundantes |

---

## Fase 1 — Corregir lo roto

**Objetivo**: Eliminar bugs funcionales y antipatrones que se replicarán con cada feature nueva.

**Criterio de entrada**: Build correcto (`dotnet build`).

### Paso 1.1 — Extraer IFileDialogService

**Problema**: `MainShellViewModel` accede directamente a `Avalonia.Application.Current` y `StorageProvider` para file pickers (`MainShellViewModel.cs:37-142`). Viola MVVM.

**Acciones**:

1. Crear interfaz `IFileDialogService` en `Interfaces/`:
   ```csharp
   public interface IFileDialogService
   {
       Task<string?> SaveFileAsync(string title, string defaultExtension, IReadOnlyList<FilePickerFileType>? fileTypeFilter = null);
       Task<string?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypeFilter = null);
   }
   ```

2. Crear implementación `FileDialogService` en `UI/Services/Dialogs/`:
   - `internal sealed class FileDialogService : IFileDialogService`
   - Constructor recibe `MainWindow` como owner.
   - Usa `StorageProvider.SaveFilePickerAsync()` y `StorageProvider.OpenFilePickerAsync()`.
   - Inyecta `ILogger<FileDialogService>`.
   - Loguea audit trail: `[UI] FileDialog: Selected '{Path}'` o `[UI] FileDialog: Cancelled`.

3. Registrar en `DependencyInjection.cs`:
   ```csharp
   services.AddSingleton<IFileDialogService, FileDialogService>();
   ```

4. Refactorizar `MainShellViewModel`:
   - Añadir `IFileDialogService` al constructor.
   - Eliminar todo acceso a `Avalonia.Application.Current` y `StorageProvider`.
   - Reemplazar `SaveFilePickerAsync()` por `_fileDialogService.SaveFileAsync()`.
   - Reemplazar `OpenFilePickerAsync()` por `_fileDialogService.OpenFileAsync()`.

**Ficheros afectados**:
- `Interfaces/IFileDialogService.cs` (NUEVO)
- `UI/Services/Dialogs/FileDialogService.cs` (NUEVO)
- `UI/Views/MainShellViewModel.cs` (MODIFICAR)
- `DependencyInjection.cs` (MODIFICAR)

**Validación**:
- [ ] `MainShellViewModel` no importa ningún namespace `Avalonia.*`.
- [ ] `FileDialogService` es `internal sealed`.
- [ ] Build correcto.

---

### Paso 1.2 — Arreglar DialogService (owner window)

**Problema**: `DialogService` nunca recibe owner window (`DialogService.cs:15`, `DependencyInjection.cs:59`). El diálogo de confirmación de guardado no funciona como modal — retorna `Cancel` siempre.

**Acciones**:

1. Modificar `DialogService`:
   - Constructor recibe `MainWindow ownerWindow` (no opcional).
   - Almacenar como `_ownerWindow`.
   - En `ShowSaveConfirmationAsync()`, usar `dialog.ShowDialog(_ownerWindow)` en lugar de `dialog.Show()`.

2. Verificar que `MainWindow` ya está registrada como Singleton en `DependencyInjection.cs` (línea 30). Si es así, el contenedor la inyectará automáticamente.

**Ficheros afectados**:
- `UI/Services/Dialogs/DialogService.cs` (MODIFICAR)

**Validación**:
- [ ] `DialogService` recibe `MainWindow` en su constructor (no null, no opcional).
- [ ] `ShowSaveConfirmationAsync()` usa `ShowDialog(_ownerWindow)` (modal real).
- [ ] Build correcto.
- [ ] Al cerrar la app con un documento dirty, el diálogo de confirmación aparece como modal y bloquea la interacción con la ventana principal.

---

### Paso 1.3 — Añadir error handling en MainShellViewModel

**Problema**: Los comandos `NewProjectAsync`, `OpenProjectAsync`, `SaveProjectAsync`, `SaveProjectAsAsync` no tienen try-catch. Excepciones de `ProjectService` propagan sin manejar.

**Acciones**:

1. Añadir `ILogger<MainShellViewModel>` al constructor.
2. Envolver cada comando en try-catch:
   ```csharp
   [RelayCommand]
   private async Task OpenProjectAsync()
   {
       _logger.LogInformation("[UI] Command: OpenProject");
       try
       {
           string? path = await _fileDialogService.OpenFileAsync("Abrir proyecto", ...);
           if (path is null) return;
           await _projectService.OpenProjectAsync(path);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "[UI] Error executing OpenProject");
           StatusMessage = $"Error: {ex.Message}";
       }
   }
   ```
3. Aplicar el mismo patrón a `NewProjectAsync`, `SaveProjectAsync`, `SaveProjectAsAsync`, `CloseProjectAsync`, `Exit`.
4. Añadir propiedad `StatusMessage` (si no existe) para feedback al usuario.

**Ficheros afectados**:
- `UI/Views/MainShellViewModel.cs` (MODIFICAR)

**Validación**:
- [ ] Todos los comandos tienen try-catch con logging.
- [ ] Ninguna excepción de servicio propaga sin manejar.
- [ ] `MainShellViewModel` inyecta `ILogger<MainShellViewModel>`.
- [ ] Build correcto.

---

### Paso 1.4 — Eliminar catch vacío en App.axaml.cs

**Problema**: `catch { }` vacío en `App.axaml.cs:92` que traga excepciones del banner de arranque.

**Acciones**:

1. Localizar el `catch { }` vacío.
2. Reemplazar por catch con logging:
   ```csharp
   catch (Exception ex)
   {
       logger.LogError(ex, "Error al mostrar banner de arranque");
   }
   ```
3. Verificar que `App.axaml.cs` tiene acceso a `ILogger<App>` (inyectado o resuelto desde `Services`).

**Ficheros afectados**:
- `App.axaml.cs` (MODIFICAR)

**Validación**:
- [ ] No existe `catch {}` vacío en ningún fichero del proyecto.
- [ ] Build correcto.

---

### Criterios de validación de Fase 1 (completos)

- [ ] `dotnet build src/Msi.TemplateCodeGenerator.slnx` → 0 errores.
- [ ] `dotnet run` → la aplicación arranca sin excepciones en consola.
- [ ] Abrir proyecto → funciona, el file picker aparece como diálogo del SO.
- [ ] Cerrar proyecto con documento dirty → diálogo de confirmación modal bloquea la ventana.
- [ ] `grep -r "Avalonia\." UI/Views/ --include="*ViewModel.cs"` → 0 resultados (ningún VM importa Avalonia).
- [ ] `grep -r "catch\s*{" src/ --include="*.cs"` → 0 resultados (ningún catch vacío).

---

## Fase 2 — Limpiar estructura y convenciones

**Objetivo**: Alinear el código con las convenciones documentadas en las guías actualizadas.

**Criterio de entrada**: Fase 1 completada y validada.

### Paso 2.1 — Corregir namespaces de UI/Views/

**Problema**: `MainShellViewModel.cs`, `MainShellView.axaml.cs`, `MainWindow.axaml.cs` están en `UI/Views/` pero usan namespace `Msi.TemplateCodeGenerator.UI` en lugar de `Msi.TemplateCodeGenerator.UI.Views`.

**Acciones**:

1. Reorganizar ficheros de Shell en subcarpeta `UI/Views/Shell/`:
   - Mover `MainShellView.axaml(.cs)` → `UI/Views/Shell/`
   - Mover `MainWindow.axaml(.cs)` → `UI/Views/Shell/`
   - Mover `MainShellViewModel.cs` → `UI/Views/Shell/ViewModels/`

2. Actualizar namespaces:
   - `MainShellView` → `Msi.TemplateCodeGenerator.UI.Views.Shell`
   - `MainWindow` → `Msi.TemplateCodeGenerator.UI.Views.Shell`
   - `MainShellViewModel` → `Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels`

3. Mover ViewModels de las demás screens a subcarpetas `ViewModels/`:
   - `ProjectExplorerShellViewModel.cs` → `UI/Views/ProjectExplorer/ViewModels/`
   - `FileEntryViewModel.cs` → `UI/Views/ProjectExplorer/ViewModels/`
   - `TemplateEditorShellViewModel.cs` → `UI/Views/TemplateEditor/ViewModels/`
   - `BaseTextEditorViewModel.cs` → `UI/Views/TemplateEditor/ViewModels/`
   - `SettingsShellViewModel.cs` → `UI/Views/Settings/ViewModels/`

4. Actualizar namespaces de todos los ViewModels movidos:
   - `ProjectExplorerShellViewModel` → `Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels`
   - `FileEntryViewModel` → `Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels`
   - `TemplateEditorShellViewModel` → `Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels`
   - `BaseTextEditorViewModel` → `Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels`
   - `SettingsShellViewModel` → `Msi.TemplateCodeGenerator.UI.Views.Settings.ViewModels`

5. Mover converters de ProjectExplorer a subcarpeta local:
   - `FileTypeToIconConverter.cs` → `UI/Views/ProjectExplorer/Converters/`
   - `FileTypeToForegroundConverter.cs` → `UI/Views/ProjectExplorer/Converters/`
   - Actualizar namespaces a `Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.Converters`

6. Mover `BaseViewModel.cs` a `UI/Shared/`:
   - `BaseViewModel.cs` → `UI/Shared/BaseViewModel.cs`
   - Namespace → `Msi.TemplateCodeGenerator.UI.Shared`

7. Renombrar `UI/Converters/` a `UI/Shared/Converters/` (si existen converters globales).

8. Actualizar `ViewLocator.cs`:
   - Actualizar imports a los nuevos namespaces.
   - Ajustar switch cases y convención de fallback.

9. Actualizar `DependencyInjection.cs`:
   - Actualizar imports a los nuevos namespaces.

10. Actualizar `App.axaml` y `App.axaml.cs`:
    - Actualizar referencias a `MainWindow` y `MainShellViewModel`.

11. Actualizar `AppDockFactory.cs`:
    - Actualizar imports a los nuevos namespaces de ViewModels.

12. Actualizar `NavigationService.cs`:
    - Actualizar imports si referencia tipos movidos.

13. Actualizar ficheros `.axaml` que referencien converters por namespace:
    - `ProjectExplorerShellView.axaml` → actualizar xmlns de converters locales.

**Ficheros afectados**:
- Todos los ficheros de `UI/Views/` (MOVER + MODIFICAR namespace)
- `ViewLocator.cs` (MODIFICAR)
- `DependencyInjection.cs` (MODIFICAR)
- `App.axaml` (MODIFICAR xmlns)
- `App.axaml.cs` (MODIFICAR imports)
- `UI/Services/Navigation/AppDockFactory.cs` (MODIFICAR imports)
- `UI/Services/Navigation/NavigationService.cs` (MODIFICAR imports)

**Validación**:
- [ ] Build correcto.
- [ ] `dotnet run` → la aplicación arranca y todas las screens se muestran correctamente.
- [ ] Todos los namespaces coinciden con la estructura de carpetas.
- [ ] `BaseViewModel` está en `UI/Shared/`.

---

### Paso 2.2 — Extraer clases Dummy de TemplatesService

**Problema**: `TemplatesService` contiene 3 clases Dummy anidadas (`DummyTemplateModel`, `DummyElement`, `DummyField`) que son modelos de datos, no lógica de servicio.

**Acciones**:

1. Crear `Models/DummyTemplateModel.cs`:
   - Mover `DummyTemplateModel` desde `TemplatesService.cs`.
   - Namespace: `Msi.TemplateCodeGenerator.Models`.

2. Crear `Models/DummyElement.cs`:
   - Mover `DummyElement` desde `TemplatesService.cs`.
   - Namespace: `Msi.TemplateCodeGenerator.Models`.

3. Crear `Models/DummyField.cs`:
   - Mover `DummyField` desde `TemplatesService.cs`.
   - Namespace: `Msi.TemplateCodeGenerator.Models`.

4. Actualizar `TemplatesService.cs`:
   - Eliminar las clases anidadas.
   - Añadir `using Msi.TemplateCodeGenerator.Models;`.

**Ficheros afectados**:
- `Models/DummyTemplateModel.cs` (NUEVO)
- `Models/DummyElement.cs` (NUEVO)
- `Models/DummyField.cs` (NUEVO)
- `Services/Templates/TemplatesService.cs` (MODIFICAR)

**Validación**:
- [ ] `TemplatesService.cs` no contiene clases anidadas.
- [ ] Build correcto.

---

### Paso 2.3 — Cambiar accesibilidad de servicios

**Problema**: `TemplatesService`, `TemplateResult` y las clases Dummy son `public` cuando deberían ser `internal`.

**Acciones**:

1. `TemplatesService.cs`: cambiar `public class TemplatesService` → `internal sealed class TemplatesService`.
2. `TemplateResult.cs`: cambiar `public class TemplateResult` → `internal sealed class TemplateResult`.
3. `DummyTemplateModel.cs`: cambiar a `internal sealed class`.
4. `DummyElement.cs`: cambiar a `internal sealed class`.
5. `DummyField.cs`: cambiar a `internal sealed class`.
6. Revisar todos los demás servicios y verificar que son `internal sealed`. Si alguno es `public`, cambiarlo.

**Ficheros afectados**:
- `Services/Templates/TemplatesService.cs` (MODIFICAR)
- `Services/Templates/TemplateResult.cs` (MODIFICAR)
- `Models/DummyTemplateModel.cs` (MODIFICAR)
- `Models/DummyElement.cs` (MODIFICAR)
- `Models/DummyField.cs` (MODIFICAR)
- Potencialmente otros servicios (REVISAR)

**Validación**:
- [ ] `grep -r "public class.*Service" src/ --include="*.cs"` → solo interfaces y `DependencyInjection`.
- [ ] `grep -r "public class.*Result" src/ --include="*.cs"` → 0 resultados en servicios.
- [ ] Build correcto.

---

### Paso 2.4 — Eliminar las 63 instancias de var

**Problema**: El `.editorconfig` prohíbe `var` pero se usa extensivamente.

**Acciones**:

1. Ejecutar búsqueda: `grep -rn "\bvar\b" src/Msi.TemplateCodeGenerator/ --include="*.cs"`.
2. Para cada instancia, reemplazar `var` por el tipo explícito correspondiente.
3. Ficheros más afectados (orden de prioridad):
   - `MainShellViewModel.cs` (~10 instancias)
   - `TemplatesService.cs` (~12 instancias)
   - `NavigationService.cs` (~9 instancias)
   - `AppDockFactory.cs` (~7 instancias)
   - `ProjectExplorerShellViewModel.cs` (~9 instancias)
   - `App.axaml.cs` (~5 instancias)
   - `JsonProjectSerializer.cs` (~4 instancias)
   - Resto (~7 instancias)

**Ficheros afectados**:
- Todos los ficheros `.cs` que contengan `var`.

**Validación**:
- [ ] `grep -rn "\bvar\b" src/Msi.TemplateCodeGenerator/ --include="*.cs"` → 0 resultados (excepto en comentarios o strings).
- [ ] Build correcto.

---

### Paso 2.5 — Eliminar usings redundantes

**Problema**: `using System;`, `using System.IO;`, `using System.Linq;` en `NavigationService.cs` y `AppDockFactory.cs` son redundantes con `ImplicitUsings`.

**Acciones**:

1. `NavigationService.cs`: eliminar `using System;`, `using System.IO;`, `using System.Linq;`.
2. `AppDockFactory.cs`: eliminar `using System;`.
3. Revisar otros ficheros por usings redundantes.

**Ficheros afectados**:
- `UI/Services/Navigation/NavigationService.cs` (MODIFICAR)
- `UI/Services/Navigation/AppDockFactory.cs` (MODIFICAR)

**Validación**:
- [ ] Build correcto.
- [ ] No warnings de usings innecesarios.

---

### Paso 2.6 — Añadir Unregister en ProjectExplorerShellViewModel

**Problema**: `_messenger.Register()` en constructor sin `_messenger.Unregister()` correspondiente. Memory leak potencial.

**Acciones**:

1. Hacer que `ProjectExplorerShellViewModel` implemente `IDisposable`.
2. En `Dispose()`, llamar a `_messenger.UnregisterAll(this)`.
3. Añadir `ILogger<ProjectExplorerShellViewModel>` al constructor.
4. Loguear en `Dispose()`: `_logger.LogDebug("ProjectExplorerShellViewModel disposed");`.

**Ficheros afectados**:
- `UI/Views/ProjectExplorer/ViewModels/ProjectExplorerShellViewModel.cs` (MODIFICAR)

**Validación**:
- [ ] `ProjectExplorerShellViewModel` implementa `IDisposable`.
- [ ] `Dispose()` llama a `_messenger.UnregisterAll(this)`.
- [ ] Build correcto.

---

### Criterios de validación de Fase 2 (completos)

- [ ] `dotnet build src/Msi.TemplateCodeGenerator.slnx` → 0 errores.
- [ ] `dotnet run` → la aplicación arranca y todas las screens funcionan correctamente.
- [ ] Estructura de carpetas coincide con la definida en `AGENTS.md`.
- [ ] Todos los namespaces coinciden con la estructura de carpetas.
- [ ] `grep -rn "\bvar\b" src/ --include="*.cs"` → 0 resultados (excepto comentarios/strings).
- [ ] `grep -r "public class.*Service" src/ --include="*.cs"` → solo `DependencyInjection`.
- [ ] `TemplatesService.cs` no contiene clases anidadas.
- [ ] `BaseViewModel` está en `UI/Shared/`.
- [ ] `ProjectExplorerShellViewModel` implementa `IDisposable`.

---

## Fase 3 — Fortalecer la base

**Objetivo**: Añadir logging, tests y documentación pendiente. Puede intercalarse con features nuevas si el tiempo apremia, pero los tests deben existir antes de tocar el modelo de dominio.

**Criterio de entrada**: Fase 2 completada y validada.

### Paso 3.1 — Inyectar ILogger<T> en todos los servicios y ViewModels

**Problema**: Solo existe un `ILogger<App>` en el banner. Ningún servicio ni ViewModel inyecta logging.

**Acciones**:

Servicios (añadir `ILogger<T>` al constructor):
1. `ProjectService` → `ILogger<ProjectService>`
2. `TemplatesService` → `ILogger<TemplatesService>`
3. `FileService` → `ILogger<FileService>`
4. `JsonProjectSerializer` → `ILogger<JsonProjectSerializer>`
5. `NavigationService` → `ILogger<NavigationService>`
6. `DialogService` → `ILogger<DialogService>`
7. `AppDockFactory` → `ILogger<AppDockFactory>`

ViewModels (añadir `ILogger<T>` al constructor):
1. `MainShellViewModel` → ya lo tiene (Fase 1.3)
2. `ProjectExplorerShellViewModel` → ya lo tiene (Fase 2.6)
3. `TemplateEditorShellViewModel` → `ILogger<TemplateEditorShellViewModel>`
4. `SettingsShellViewModel` → `ILogger<SettingsShellViewModel>`
5. `BaseTextEditorViewModel` → `ILogger` (protegido, pasado por clases derivadas)

**Ficheros afectados**:
- Todos los servicios y ViewModels listados.

**Validación**:
- [ ] `grep -r "ILogger" src/ --include="*.cs"` → al menos una inyección por servicio y VM.
- [ ] Build correcto.

---

### Paso 3.2 — Resolver Scoped de TemplateEditorShellViewModel correctamente

**Problema**: `TemplateEditorShellViewModel` es Scoped pero se resuelve desde el root provider en `NavigationService.cs` sin crear `IServiceScope`. Convierte el Scoped en Singleton efectivo.

**Acciones**:

1. En `NavigationService.OpenFile()`:
   - Crear `IServiceScope` explícito: `using IServiceScope scope = _serviceProvider.CreateScope();`
   - Resolver `TemplateEditorShellViewModel` desde `scope.ServiceProvider`.
   - Almacenar el scope junto con el documento para hacer `Dispose` al cerrar la pestaña.

2. En el flujo de cierre de documento:
   - Hacer `Dispose` del scope asociado al documento cerrado.

**Ficheros afectados**:
- `UI/Services/Navigation/NavigationService.cs` (MODIFICAR)

**Validación**:
- [ ] `NavigationService` crea `IServiceScope` antes de resolver `TemplateEditorShellViewModel`.
- [ ] Al cerrar un documento, se hace `Dispose` del scope.
- [ ] Build correcto.
- [ ] Abrir dos pestañas de editor → cada una tiene su propia instancia de VM.

---

### Paso 3.3 — Crear proyecto de tests

**Problema**: Cero cobertura de tests. No existe proyecto de tests en la solución.

**Acciones**:

1. Crear proyecto de tests:
   ```
   dotnet new xunit -n Msi.TemplateCodeGenerator.Tests -o test/Msi.TemplateCodeGenerator.Tests
   ```

2. Añadir referencia al proyecto principal:
   ```
   dotnet add test/Msi.TemplateCodeGenerator.Tests/Msi.TemplateCodeGenerator.Tests.csproj reference src/Msi.TemplateCodeGenerator/Msi.TemplateCodeGenerator.csproj
   ```

3. Añadir el proyecto de tests a la solución:
   ```
   dotnet sln src/Msi.TemplateCodeGenerator.slnx add test/Msi.TemplateCodeGenerator.Tests/Msi.TemplateCodeGenerator.Tests.csproj
   ```

4. Añadir `InternalsVisibleTo` en el proyecto principal para que los tests puedan acceder a clases `internal`:
   - En `Msi.TemplateCodeGenerator.csproj`, añadir:
   ```xml
   <ItemGroup>
       <InternalsVisibleTo Include="Msi.TemplateCodeGenerator.Tests" />
   </ItemGroup>
   ```

5. Crear tests iniciales para `JsonProjectSerializer`:
   - `test/Msi.TemplateCodeGenerator.Tests/Services/Project/JsonProjectSerializerTests.cs`
   - Tests: serializar/deserializar proyecto, leer JSONC con comentarios, manejar fichero corrupto.

6. Crear tests iniciales para `TemplatesService`:
   - `test/Msi.TemplateCodeGenerator.Tests/Services/Templates/TemplatesServiceTests.cs`
   - Tests: plantilla válida, plantilla con error de sintaxis, plantilla vacía.

7. Crear tests iniciales para `ProjectService`:
   - `test/Msi.TemplateCodeGenerator.Tests/Services/Project/ProjectServiceTests.cs`
   - Tests: abrir proyecto, cerrar proyecto, guardar, validaciones de argumentos.
   - Usar mocks para `IProjectSerializer`, `IProjectContext`, `IMessenger`.

8. Añadir paquete de mocking:
   ```
   dotnet add test/Msi.TemplateCodeGenerator.Tests/Msi.TemplateCodeGenerator.Tests.csproj package NSubstitute
   ```

**Ficheros afectados**:
- `test/Msi.TemplateCodeGenerator.Tests/` (NUEVO proyecto)
- `src/Msi.TemplateCodeGenerator/Msi.TemplateCodeGenerator.csproj` (MODIFICAR — InternalsVisibleTo)
- `src/Msi.TemplateCodeGenerator.slnx` (MODIFICAR — añadir proyecto)

**Validación**:
- [ ] `dotnet test` → todos los tests pasan.
- [ ] Al menos 10 tests unitarios cubriendo `JsonProjectSerializer`, `TemplatesService`, `ProjectService`.
- [ ] Build correcto de la solución completa.

---

### Paso 3.4 — Completar documentación vacía

**Problema**: `alcance-y-dominio.md` e `implementacion-actual.md` están vacíos.

**Acciones**:

1. `alcance-y-dominio.md`:
   - Definir el alcance funcional del producto.
   - Límites del dominio (qué hace y qué NO hace).
   - Criterios de exclusión.

2. `implementacion-actual.md`:
   - Documentar el estado real de la implementación.
   - Qué funciona, qué está a medias, qué no existe.
   - Referencia a los TODOs pendientes (FileWatcher, secciones recursivas, etc.).

**Ficheros afectados**:
- `docs/agents/proyecto/alcance-y-dominio.md` (MODIFICAR)
- `docs/agents/proyecto/implementacion-actual.md` (MODIFICAR)

**Validación**:
- [ ] Ambos ficheros tienen contenido significativo.
- [ ] El contenido es coherente con `arquitectura-y-dominio.md` y `modelo-de-proyecto.md`.

---

### Criterios de validación de Fase 3 (completos)

- [ ] `dotnet build src/Msi.TemplateCodeGenerator.slnx` → 0 errores.
- [ ] `dotnet test` → todos los tests pasan (mínimo 10).
- [ ] `dotnet run` → la aplicación arranca y funciona correctamente.
- [ ] Todos los servicios y VMs inyectan `ILogger<T>`.
- [ ] `TemplateEditorShellViewModel` se resuelve con `IServiceScope` explícito.
- [ ] `alcance-y-dominio.md` e `implementacion-actual.md` tienen contenido.

---

## Orden de ejecución recomendado

```
Fase 1 (corregir lo roto)
├── 1.1 IFileDialogService
├── 1.2 DialogService owner
├── 1.3 Error handling en MainShellVM
└── 1.4 Catch vacío en App.axaml.cs
    ↓ Validación Fase 1
Fase 2 (limpiar estructura)
├── 2.1 Namespaces y carpetas (el más grande)
├── 2.2 Extraer clases Dummy
├── 2.3 Accesibilidad internal sealed
├── 2.4 Eliminar var (63 instancias)
├── 2.5 Usings redundantes
└── 2.6 Unregister en ProjectExplorerVM
    ↓ Validación Fase 2
Fase 3 (fortalecer base)
├── 3.1 ILogger<T> en servicios y VMs
├── 3.2 Scoped correcto para TemplateEditorVM
├── 3.3 Proyecto de tests
└── 3.4 Documentación vacía
    ↓ Validación Fase 3
```

## Notas para el agente implantador

- Ejecutar `dotnet build` después de cada paso, no solo al final de la fase.
- Ejecutar `dotnet run` después de cada fase para verificar funcionalidad.
- Si un paso falla, no continuar al siguiente sin resolver el fallo.
- Los pasos dentro de una fase son secuenciales (algunos dependen de otros).
- Respetar las convenciones de las guías actualizadas: código en inglés, comentarios en español, tipos explícitos (no `var`), `internal sealed` para implementaciones.

## Notas para el agente validador

- Ejecutar todos los checkboxes de validación de cada fase.
- Si un checkbox falla, reportar el paso específico y el fichero afectado.
- No aprobar una fase hasta que todos sus checkboxes estén marcados.
- Verificar coherencia con las guías en `docs/agents/msi-guidelines-avalonia/` y `docs/agents/msi-guidelines-dotnet/`.
