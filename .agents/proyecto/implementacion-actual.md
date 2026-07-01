# Implementación actual

> Estado real de la implementación: qué funciona, qué está a medias y qué no existe.

## Resumen ejecutivo

| Área | Estado | Notas |
|---|---|---|
| Gestión de proyectos | Completo | CRUD funcional con serialización JSON |
| Editor de plantillas | Funcional | Editor con vista previa y debounce |
| Explorador de archivos | Funcional | Árbol jerárquico con iconos |
| Navegación con docking | Funcional | Dock.Avalonia con pestañas |
| Logging | Completo | Serilog con ILogger<T> inyectado en todos los servicios y VMs |
| Tests unitarios | Completado | 30 tests (xUnit + NSubstitute) |
| Scoped resolution | Corregido | IServiceScope explícito para editores |
| FileWatcher | Esqueleto | Clase parcial creada, sin implementación |
| Secciones recursivas | No implementado | Planificado en modelo de proyecto |
| Gestión de modelos | No implementado | Solo DummyTemplateModel de prueba |
| Exportación de código | No implementado | Vista previa solo, sin export |

## Estado por componente

### ProjectService
- **Funcional**: `OpenProjectAsync`, `CloseProjectAsync`, `SaveProjectAsync`, `SaveProjectAsAsync`, `CreateNewProjectAsync`
- **Logging**: Completo (todos los métodos registran entrada/salida)
- **Validaciones**: Argumentos nulos/vacíos verificados en todas las operaciones
- **Pendiente**: FileWatcher (clase parcial `ProjectService.FileWatcher.cs` sin implementación)

### JsonProjectSerializer
- **Funcional**: Lectura y escritura de JSON con soporte de comentarios (JSONC)
- **Validaciones**: Versión del formato (mínima y máxima), archivo corrupto, path vacío
- **Limitación**: Los comentarios se leen pero NO se preservan al guardar (comportamiento conocido)
- **Pendiente**: Migración de versiones intermedias (stub en código)

### TemplatesService
- **Funcional**: Parseo y renderizado de plantillas Scriban
- **Validaciones**: Errores de sintaxis, errores de ejecución, contenido vacío
- **Modelo de datos**: DummyTemplateModel con datos de prueba fijos (GetDummyModel)
- **Pendiente**: Inyección de datos reales desde Project/Sections

### TemplateEditorShellViewModel
- **Funcional**: Editor de texto con Load/Save, vista previa con debounce
- **Scoped**: Resuelto correctamente con IServiceScope por pestaña
- **Pendiente**: Undo/Redo, autocompletado, navegación de errores

### ProjectExplorerShellViewModel
- **Funcional**: Árbol jerárquico de archivos con clasificación por FileType
- **Mensajería**: Escucha ProjectOpenedMessage, ProjectSavedMessage, ProjectClosedMessage
- **Memory management**: Implementa IDisposable con UnregisterAll
- **Pendiente**: Refresco automático con FileWatcher

### NavigationService
- **Funcional**: Lazy initialization del layout, apertura/cierre de documentos
- **Scoped**: Cada editor tiene su propio IServiceScope, disposed al cerrar
- **Limitación**: No hay persistencia de layout (paneles cerrados no se restauran)

### FileService
- **Funcional**: Lectura/escritura de texto plano
- **Logging**: Debug level en lectura y escritura
- **Limitación**: No hay gestión de codificación (siempre UTF-8 por defecto de .NET)

### DialogService / FileDialogService
- **Funcional**: Diálogo modal de confirmación de guardado, file pickers del SO
- **Owner**: MainWindow inyectado como owner para diálogos modales reales
- **Audit trail**: Loguea selección/cancelación de ficheros y resultados de diálogo

## Tests unitarios

**Proyecto**: `Msi.TemplateCodeGenerator.Tests` (xUnit + NSubstitute)
**Total**: 30 tests, todos pasando

### Cobertura por componente

| Componente | Tests | Cubierto |
|---|---|---|
| JsonProjectSerializer | 9 | Serialización round-trip, errores, comentarios, versión |
| TemplatesService | 8 | Templates válidos, errores de sintaxis, loops, condicionales, funciones |
| ProjectService | 13 | Validaciones de argumentos, contexto, llamadas al serializer |

## Antipatrones corregidos (Fases 1-3)

1. **UI framework en ViewModels** — MainShellViewModel ya no accede a Avalonia.Application.Current ni StorageProvider (extraído IFileDialogService)
2. **DialogService sin owner** — Ahora recibe MainWindow en el constructor y usa ShowDialog(owner)
3. **Scoped convertido en Singleton** — TemplateEditorShellViewModel se resuelve con IServiceScope explícito
4. **Catch vacío** — Eliminado en App.axaml.cs, reemplazado por logging
5. **Sin error handling** — Todos los comandos de MainShellViewModel envueltos en try-catch
6. **Sin Unregister** — ProjectExplorerShellViewModel implementa IDisposable
7. **63 instancias de var** — Corregidas a tipos explícitos
8. **Namespaces incorrectos** — Estructura alineada con AGENTS.md
9. **Servicios public** — Todos internal sealed
10. **ILogger ausente** — Inyectado en todos los servicios y ViewModels

## TODOs pendientes

### Alto impacto
- **FileWatcher**: Implementar vigilancia de cambios en ProjectService.FileWatcher.cs
- **Secciones recursivas**: Implementar modelo Section con datos y plantillas
- **Datos reales**: Reemplazar DummyTemplateModel con datos del proyecto activo

### Medio impacto
- **Exportación de código**: Botón para copiar/exportar el resultado del renderizado
- **Persistencia de layout**: Guardar/restaurar la disposición de paneles
- **Migración JSON**: Implementar migraciones de versiones intermedias del formato

### Bajo impacto
- **Preservar comentarios**: Migrar a JSON5 o approach similar
- **ReferencedAssemblies**: Propiedad en Project para ensamblados referenciados
- **Configuration**: Propiedad en Project para configuración del proyecto
