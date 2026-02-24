# En el Editor

-   **Panel de Edición**: Es el área donde trabajarás principalmente. Aquí es donde escribirás y editarás tus plantillas Scriban.
-   **Panel de Preview**: Este panel muestra en tiempo real cómo quedará el código generado una vez que apliques tus plantillas sobre los modelos de datos. También te indicará si hay errores en las plantillas.

# Creando tu Primer Proyecto de Plantillas

Para comenzar a usar **Msi.TemplateCodeGenerator**, sigue estos pasos:

1. **Crea un Nuevo Proyecto**: Desde la aplicación, selecciona la opción para crear un nuevo proyecto de plantillas.
2. **Configura el Proyecto**:
   - Asigna un nombre y una ubicación para tu proyecto.
   - Agrega referencias a las DLLs que contienen tus modelos de datos.
3. **Añade Plantillas**:
   - Crea un nuevo archivo de plantilla Scriban desde el editor.
   - Define el contenido de tu plantilla usando la sintaxis de Scriban. **Recuerda usar paréntesis `()` alrededor de los argumentos de los métodos para mayor claridad, ya que los usuarios son desarrolladores de C# acostumbrados a esta sintaxis.**
4. **Verifica la Salida**: A medida que edits tu plantilla, observa el panel de preview para ver cómo cambia el código generado.
5. **Guarda y Comparte**: Una vez que estés satisfecho con tus plantillas y el código generado, guarda tu proyecto. Puedes compartir la carpeta del proyecto con tu equipo para que ellos también puedan generar el código.

# Consejos y Trucos

- Usa la funcionalidad de **autocompletar** del editor para acelerar la escritura de tus plantillas.
- Aprovecha los **ejemplos de plantillas** incluidos en la documentación como punto de partida.
- No dudes en **experimentar** con diferentes características de Scriban, como funciones personalizadas y lógica condicional, para crear plantillas potentes y flexibles.

-   **Nomenclatura e Idioma**: 
    -   **Código**: El código (nombres de variables, clases, métodos, etc.) debe estar en **inglés**, siguiendo las convenciones estándar de C# y .NET.
    -   **Comentarios**: Los **comentarios** y la **documentación XML** deben escribirse en **español (Castellano)** para facilitar la comprensión y el mantenimiento del equipo.
-   **Métodos en Scriban**: Solo se pueden registrar métodos estáticos en Scriban; no se deben exponer métodos públicos de instancia.

## Dependencias Clave

-   **Avalonia UI**: Framework multiplataforma para interfaces de usuario (reemplazo de WPF). Usa XAML para definir vistas.
-   **Scriban**: Motor de plantillas para la generación de código. Soporta funciones personalizadas y modelos de objetos complejos.
    -   **Documentación Oficial**: [https://scriban.github.io/docs/](https://scriban.github.io/docs/) (Consultar esta URL para sintaxis, funciones integradas y API de C#).
-   **CommunityToolkit.Mvvm**: Biblioteca oficial de Microsoft para implementar el patrón MVVM con source generators. Proporciona atributos como `[ObservableProperty]` y `[RelayCommand]` que generan automáticamente el código boilerplate necesario.

---

# Arquitectura de la Aplicación

## Estructura de Carpetas

El proyecto sigue una organización estándar para aplicaciones WPF/Avalonia con MVVM:

```
Msi.TemplateCodeGenerator/
├── Constants/           ← Constantes de la aplicación (ProjectConstants con extensiones y versiones)
├── Messages/            ← Mensajes del sistema de mensajería (ProjectOpenedMessage, etc.)
├── Models/              ← POCOs de dominio/negocio (Project, Template, etc.)
├── Interfaces/          ← Contratos publicados en el IoC (IProjectContext, IProjectService, ITemplatesService, IProjectSerializer)
├── Services/            ← Implementaciones de servicios registrados en el IoC
│   ├── Project/         ← ProjectContext, ProjectService, JsonProjectSerializer
│   └── Templates/       ← TemplatesService
└── UI/                  ← ViewModels y Views (MVVM)
    ├── MainShellViewModel.cs + MainShellView.axaml (Shell principal)
    ├── ProjectExplorer/
    ├── TemplateEditor/
    └── Settings/
```

## Separación de Responsabilidades

### 1. **Models** (Dominio)
- Contienen la lógica de negocio y datos puros (POCOs).
- **Estado actual**: `Project` tiene la propiedad `Name`. Futuras propiedades planificadas: `Templates`, `ReferencedAssemblies`, `Configuration` (ver TODOs en el código).
- **No dependen** de UI ni de servicios de infraestructura.

### 2. **Interfaces** (Contratos IoC)
- Definen los contratos de servicios que se registran en el contenedor de dependencias.
- **Separación clave**:
  - **`IProjectContext`**: Solo contiene **estado/datos** del proyecto activo (sin lógica de operaciones).
    - `Project? CurrentProject`: El proyecto activo cargado en memoria.
    - `string? CurrentProjectPath`: Ruta en disco del proyecto.
    - `bool IsProjectOpen`: Derivado de `CurrentProject != null`.
  - **`IProjectService`**: Contiene **operaciones/lógica** relacionadas con proyectos.
    - `OpenProjectAsync()`: Carga proyecto, inicia FileWatcher, actualiza contexto.
    - `CloseProjectAsync()`: Cierra proyecto, detiene FileWatcher, limpia contexto.
    - `SaveProjectAsync()`: Persiste cambios al XML.
    - `CreateNewProjectAsync()`: Crea estructura de carpetas y archivo XML inicial.

### 3. **Services** (Implementaciones)
- Implementan las interfaces y contienen la lógica de negocio/infraestructura.
- **`ProjectContext`**: Implementación interna que solo almacena el estado. Expone setters `internal` para que solo `ProjectService` pueda modificarlo.
- **`ProjectService`**: Gestiona operaciones complejas (carga XML, FileWatcher, validaciones) y actualiza `ProjectContext`.
- **Principio**: Los servicios **no implementan `INotifyPropertyChanged`**. Son independientes de la UI. Para notificar cambios se usará mensajería (futuro).

### 4. **UI (MVVM)**
- **ViewModels**:
  - Heredan de `BaseViewModel` (que a su vez hereda de `ObservableObject` de CommunityToolkit.Mvvm).
  - Usan `[ObservableProperty]` para propiedades con binding.
  - Usan `[RelayCommand]` para comandos invocables desde la vista.
  - **Principio de ViewModels vs Servicios**:
    - Los ViewModels **no contienen lógica de negocio compleja**.
    - Delegan operaciones a servicios (inyectados por DI).
    - Exponen propiedades y comandos para la vista.
- **Views**:
  - Archivos `.axaml` (Avalonia XAML) que definen la interfaz.
  - Usan binding a propiedades y comandos del ViewModel.

## Gestión de Proyectos (Project Management)

### Contexto vs Servicio

**Decisión de diseño**: Separar el estado (contexto) de las operaciones (servicio).

- **`IProjectContext`** (Estado):
  - Solo propiedades de lectura pública.
  - Representa el "estado actual" de la aplicación respecto al proyecto.
  - Los ViewModels **leen** de aquí para mostrar información.
  - **No tiene métodos** de modificación expuestos públicamente.

- **`IProjectService`** (Operaciones):
  - Métodos que modifican el contexto indirectamente.
  - Contiene toda la lógica compleja (carga XML, validaciones, FileWatcher).
  - Los ViewModels **invocan** este servicio para ejecutar acciones (abrir, cerrar, guardar).

**Ventajas**:
- Testeable: fácil mockear el contexto para pruebas.
- Separación clara: el contexto crece con datos, el servicio con lógica.
- Reutilizable: múltiples ViewModels pueden leer del mismo contexto sin duplicar estado.

## Comandos Globales y Locales

**Problema resuelto**: Comunicación entre servicios y ViewModels sin acoplamiento.

**Solución implementada: Sistema de Mensajería**
1. **Mensajes** (`Messages/`): Records inmutables que representan eventos del dominio.
   - `ProjectOpenedMessage(string ProjectPath)`
   - `ProjectClosedMessage`
   - `ProjectSavedMessage(string ProjectPath)`

2. **`IProjectService` como Publisher**: Envía mensajes después de cada operación.
   ```csharp
   _messenger.Send(new ProjectOpenedMessage(projectPath));
   ```

3. **ViewModels como Subscribers**: Se suscriben en el constructor.
   ```csharp
   _messenger.Register<ProjectOpenedMessage>(this, (r, m) => r.RefreshProjectContext());
   ```

4. **Desacoplamiento total**: 
   - `MainShellViewModel` NO conoce a `ProjectExplorerShellViewModel`
   - Cualquier ViewModel puede suscribirse sin modificar código existente
   - Los servicios notifican sin depender de la UI

**Messenger usado**: `CommunityToolkit.Mvvm.Messaging` (`WeakReferenceMessenger.Default`)

**Deprecated**: ~~Llamadas manuales a `RefreshProjectContextCommand.Execute(null)`~~

## Convenciones de Código

### Métodos Asíncronos
- **Todos los métodos con I/O deben ser async**, incluyendo:
  - Apertura/cierre de proyectos (`OpenProjectAsync`, `CloseProjectAsync`).
  - Guardado (`SaveProjectAsync`).
  - Renderizado de plantillas (`ProcessTemplateAsync`).
- **Razón**: Mantener la UI responsive, permitir cleanup con I/O, y consistencia.

### Nomenclatura
- **Código**: Inglés (variables, clases, métodos).
- **Comentarios y XML docs**: Español (Castellano).
- **Terminología de proyecto**: Se usa "Project" en lugar de "Workspace" para alinearse con la experiencia de desarrolladores C# (similar a Visual Studio).

### Inyección de Dependencias
- Registrar servicios en `DependencyInjection.cs`:
  - `IMessenger` / `WeakReferenceMessenger.Default`: Singleton (sistema de mensajería).
  - `IProjectContext` / `ProjectContext`: Singleton (estado compartido).
  - `IProjectSerializer` / `JsonProjectSerializer`: Singleton (persistencia).
  - `IProjectService` / `ProjectService`: Singleton.
  - ViewModels: Singleton (uno por vista en el Shell principal).

## Flujo de Trabajo de Proyecto

1. **Usuario abre proyecto** (Archivo → Abrir Proyecto):
   - `MainShellViewModel.OpenProjectAsync()` abre diálogo de archivo.
   - Llama a `IProjectService.OpenProjectAsync(path)`.
   - `ProjectService`:
     - Carga y parsea XML (TODO).
     - Crea instancia de `Project` (POCO).
     - Inicia `FileWatcher` (TODO).
     - Actualiza `ProjectContext.CurrentProject` y `CurrentProjectPath`.
   - `MainShellViewModel` refresca manualmente `ProjectExplorerShellViewModel`.

2. **Usuario cierra proyecto** (Archivo → Cerrar Proyecto):
   - `MainShellViewModel.CloseProjectAsync()` invoca `IProjectService.CloseProjectAsync()`.
   - `ProjectService`:
     - Detiene `FileWatcher` (TODO).
     - Limpia recursos.
     - Establece `ProjectContext.CurrentProject = null`.
   - ViewModels se refrescan y muestran "sin solución".

3. **Renderizado de plantillas**:
   - `TemplateEditorShellViewModel` usa debounce (1 segundo) para evitar renderizados constantes.
   - Llama a `ITemplatesService.ProcessTemplateAsync(templateContent)`.
   - Si hay errores, los muestra directamente en el panel de preview.

## Estructura del Proyecto de Usuario (en disco)

Similar a proyectos de Visual Studio:
- **Archivo principal**: `*.tproj` (XML con metadatos del proyecto).
- **Carpeta del proyecto**: Contiene el `.tproj` y todos los archivos/subcarpetas.
- **FileWatcher**: Vigila cambios en la carpeta (añadir/eliminar archivos) para actualizar el modelo en memoria.
- **Plantillas**: Archivos `.scriban` dentro de la carpeta del proyecto.

## Serialización de Proyectos

- **Formato actual**: JSON con soporte JSONC (comentarios leídos pero no preservados al guardar).
- **Abstracción**: `IProjectSerializer` permite cambiar de formato fácilmente.
- **Implementación**: `JsonProjectSerializer` usa `System.Text.Json` con `ReadCommentHandling.Skip`.
- **Futura mejora**: Migrar a JSON5 si se requiere preservar comentarios al guardar.

## Futuras Mejoras Planificadas

- **FileWatcher**: Vigilar cambios en la carpeta del proyecto para mantener sincronizado el modelo en memoria.
- **Validaciones**: Validar estructura de proyectos al cargar.
- **Templates management**: Colección de plantillas en el modelo `Project`, editor de plantillas individual.
- **JSON5**: Migrar a JSON5 para preservar comentarios en archivos de proyecto.
