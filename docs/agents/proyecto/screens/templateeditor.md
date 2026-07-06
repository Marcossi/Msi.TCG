# TemplateEditorView

> Descripción detallada de TemplateEditorView. Editor de plantillas Scriban con vista previa en tiempo real. Permite editar una plantilla y ver el resultado de la generación de código.

## Ubicación

- **Carpeta**: `UI/Views/TemplateEditor/`
- **Ficheros**:
  - `TemplateEditorShellView.axaml` → Vista XAML
  - `TemplateEditorShellView.axaml.cs` → Code-behind
  - `TemplateEditorShellViewModel.cs` → ViewModel
  - `BaseTextEditorViewModel.cs` → Clase base abstracta para editores de texto

## Pantalla

- **Tipo Dock**: Document (pestaña)
- **ID**: `TemplateEditorId` (definido en `NavigationConstants.TemplateEditorId`)

## Funcionalidad

### Editor

- Área de texto para editar plantillas Scriban
- Soporte para sintaxis Scriban
- Indicador de errores de sintaxis

### Vista previa

- Panel que muestra el resultado de la generación de código
- Se actualiza automáticamente al editar la plantilla
- Muestra errores de sintaxis si los hay

### Debounce

El editor usa debounce (1 segundo) para evitar renderizados constantes mientras el usuario escribe.

## ViewModel

### TemplateEditorShellViewModel

Dependencias:
- `ITemplatesService` → Procesamiento de plantillas
- `IFileService` → Operaciones de ficheros
- `IDialogService` → Diálogos de confirmación

Herencia:
- Hereda de `BaseTextEditorViewModel` (gestiona filePath, contenido, dirty tracking, ICloseAware)

Propiedades:
- `PreviewContent` → Resultado renderizado
- `StatusMessage` → Mensaje de estado (heredado de BaseTextEditorViewModel, se usa para mostrar errores)
- `FilePath` → Ruta del fichero (heredado)
- `Content` → Contenido de la plantilla editada (heredado de BaseTextEditorViewModel)
- `IsDirty` → Estado de cambios sin guardar (heredado)

Comandos:
- `SaveAsync()` → Guarda el fichero (heredado de BaseTextEditorViewModel)
- `LoadFileAsync()` → Carga un fichero (heredado de BaseTextEditorViewModel)

Flujo:
1. Usuario edita plantilla → `Content` cambia
2. `OnContentChanged` → marca IsDirty, llama a `OnContentChangedCore`
3. `UpdatePreviewWithDebounceAsync` → debounce 1 segundo
4. `ITemplatesService.ProcessTemplateAsync(content)`
5. Si éxito → `PreviewContent = result.Result`
6. Si error → `PreviewContent = "Error: {result.ErrorMessage}"`, `StatusMessage = "Error: ..."`

### BaseTextEditorViewModel

Clase base abstracta que gestiona:
- `FilePath`: Ruta del fichero editado
- `Content`: Contenido del fichero
- `IsDirty`: Estado de cambios sin guardar
- `TabTitle`: Título de la pestaña (derivado de FilePath, "Nueva Plantilla" si no hay fichero)
- `StatusMessage`: Mensaje de estado
- `LoadFileAsync()`: Carga un fichero desde disco
- `SaveAsync()`: Guarda el fichero en disco (CanExecute = IsDirty && !empty FilePath)
- `CanCloseAsync()`: Implementa `ICloseAware` para confirmación de cierre
- `MarkAsSaved()`: Marca el fichero como guardado
- `OnContentChangedCore()`: Punto de extensión para clases derivadas

## Registro en DI

- `TemplateEditorShellViewModel` → `TemplateEditorShellViewModel` → **Scoped**

Nota: Scoped porque se crea una instancia por cada pestaña de editor abierta.
