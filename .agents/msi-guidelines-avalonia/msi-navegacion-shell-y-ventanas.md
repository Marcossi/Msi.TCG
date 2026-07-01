# Navegacion, shell y ventanas

> Capa reutilizable para la organizacion de shell desktop, documentos, herramientas y ventanas en Avalonia.

## Regla de shell

La aplicacion debe tener una shell principal estable que orqueste navegacion, herramientas y documentos. La shell no debe convertirse en contenedor de logica de negocio.

## Bootstrap Avalonia

- `Program.cs` expone `BuildAvaloniaApp()`.
- `App.axaml.cs` completa la inicializacion del host .NET y despues crea la ventana principal.
- La ventana principal obtiene su `DataContext` desde el contenedor.

## Navegacion

- Abstraer la navegacion tras una interfaz, por ejemplo `INavigationService`.
- No navegar con referencias directas entre ViewModels.
- Si la aplicacion usa docking o documentos, encapsular la infraestructura en un servicio y, si hace falta, una factory.

## Docking y layout

Si se usa un layout dockeable estilo IDE:

- Separar `tool panes` y `document panes`.
- Centralizar los IDs de navegacion en constantes.
- Inicializar el layout de forma lazy si eso evita dependencias circulares.
- La factory del layout puede depender de `IServiceProvider` como punto de composicion, pero no trasladar ese patron al resto de la aplicacion.
- **Referencia obligatoria:** Para detalles de implementacion de `Dock.Avalonia`, modelado de paneles, persistencia de layout y patrones MVVM, leer primero `.agents/libraries-doc/Dock-12.0.0.2/index.md` y sus articulos asociados. No asumir comportamiento sin consultar la doc oficial.

## Dialogos y ventanas auxiliares

- Abstraer dialogos en `IDialogService`.
- Abstraer file pickers en `IFileDialogService` (o extender `IDialogService`).
- Evitar abrir ventanas o dialogos desde logica de dominio.
- Las confirmaciones de cierre, guardado y descarte deben resolverse en servicios o infraestructura de UI, no en el dominio.

### Regla de abstraccion de UI framework

**Prohibido** acceder a tipos de Avalonia desde un ViewModel para mostrar dialogos. Esto incluye:
- `Avalonia.Application.Current` para obtener la ventana principal.
- `StorageProvider.SaveFilePickerAsync()` o `StorageProvider.OpenFilePickerAsync()`.
- `Window.ShowDialog()` o `Window.Show()` directamente.

Toda interaccion con dialogos del sistema operativo se abstrae en un servicio de `UI/Services/Dialogs/`.

### Regla de owner window

Los dialogos modales requieren un owner window para funcionar correctamente:
- `IDialogService` recibe la ventana principal en su constructor o mediante un metodo `SetOwner(Window)`.
- Un dialogo sin owner no es modal y rompe el flujo de confirmacion (retorna inmediatamente).
- La ventana principal se registra como Singleton en el contenedor y se inyecta en el servicio de dialogos.

## Regla de documentos editables

Si una vista representa un documento:

- Debe poder declarar estado dirty.
- Debe poder participar en el flujo de cierre seguro.
- Debe evitar perdida de cambios al cerrar la ventana o el documento.

### Regla de ICloseAware

Todo documento editable implementa `ICloseAware`:

```csharp
public interface ICloseAware
{
    Task<bool> CanCloseAsync();
}
```

- `CanCloseAsync()` verifica dirty state y muestra dialogo de confirmacion si es necesario.
- `INavigationService.CloseDocumentAsync(id)` consulta `CanCloseAsync()` antes de cerrar.
- `INavigationService.CanCloseAllAsync()` itera todos los documentos abiertos antes de cerrar la aplicacion.
- Prohibido cerrar documentos sin verificar dirty state (perdida de datos).

## Riesgo conocido

En shells complejas con docking es facil crear dependencias circulares entre shell, servicio de navegacion, factory de layout y view models. La mitigacion preferida es lazy initialization del layout y composicion tardia de elementos UI.