# ADR-003: Sistema de Editores de Código

## Estado

Accepted (2026-07-02)

## Contexto

La aplicación permite editar múltiples tipos de archivos de texto dentro del proyecto:
- Plantillas Scriban (`.scriban`)
- Metadatos JSON (`.json`)
- Futuros: configuración YAML, scripts de inicialización, etc.

**Problema:**
- Actualmente solo existe `TemplateEditorShellViewModel` para editar plantillas Scriban.
- No hay una estrategia clara para abrir diferentes tipos de editores según el tipo de archivo.
- Cada nuevo tipo de editor requeriría duplicar la lógica de load/save/dirty tracking.

**Fuerzas:**
- **Reutilización**: La lógica de load/save/dirty tracking es común a todos los editores de texto.
- **Extensibilidad**: Nuevos tipos de editores aparecerán con el tiempo.
- **Consistencia**: Todos los editores deben comportarse igual (guardar, cerrar con confirmación, routing de comandos).
- **Experiencia de usuario**: El usuario espera que al hacer doble-click en un archivo se abra el editor adecuado automáticamente.

## Decisión

Implementar un **sistema de editores de código basado en herencia** con las siguientes características:

### 1. Arquitectura de 2 capas

```
BaseTextEditorViewModel (abstracta, reutilizable)
  ├── Load/Save, dirty tracking, ICloseAware, ICommandRoute
  ├── Punto de extensión: OnContentChangedCore()
  │
  ├── TemplateEditorShellViewModel (especialización para Scriban)
  │     └── Preview en tiempo real con debounce
  │
  └── MetadataEditorShellViewModel (especialización para JSON)
        └── Preview del objeto parseado + defaults aplicados
```

**Razones:**
- `BaseTextEditorViewModel` ya encapsula la lógica común (load/save/dirty/close/command routing).
- Las especializaciones solo añaden comportamiento específico (preview, validación, etc.).
- Nuevos editores se crean heredando de la base y sobrescribiendo `OnContentChangedCore()`.

### 2. Detección de editor por extensión de archivo

El `ProjectExplorer` detecta la extensión del archivo y abre el editor correspondiente:

| Extensión | Editor |
|-----------|--------|
| `.scriban` | `TemplateEditorShellViewModel` |
| `.json` (en carpeta `metadata/`) | `MetadataEditorShellViewModel` |
| Otros | Editor de texto genérico (futuro) |

**Flujo:**
1. Usuario hace doble-click en un archivo del `ProjectExplorer`.
2. `ProjectExplorerShellViewModel` invoca `INavigationService.OpenFile(filePath)`.
3. `NavigationService` detecta la extensión y resuelve el ViewModel adecuado.
4. Se crea el documento dockable con el ViewModel como contexto.

**Alternativas descartadas:**
- **Menú contextual con opciones manuales**: El usuario elige "Editar como Scriban" / "Editar como Metadata". Más flexible pero más tedioso.
- **Registro de editores por MIME type**: Demasiado complejo para el caso de uso actual.

### 3. Split-view: Editor + Preview

Todos los editores especializados usan un layout de pantalla dividida:

```
┌─────────────────────────────────────────────────┐
│ [Archivo.scriban] [Archivo.json]                │ ← Pestañas del dock
├──────────────────────────┬──────────────────────┤
│ Editor (editable)        │ Preview (solo lectura)│
│                          │                      │
│ TextBox con contenido    │ Resultado parseado   │
│ con syntax highlighting  │ + defaults aplicados │
│                          │ o mensaje de error   │
└──────────────────────────┴──────────────────────┘
```

**Razones:**
- El preview permite validar en tiempo real que el contenido se está interpretando correctamente.
- Para Scriban: muestra el resultado de renderizar la plantilla.
- Para JSON: muestra el objeto deserializado + defaults aplicados.
- Si hay errores de sintaxis, el panel de preview muestra un mensaje de error claro.

**Alternativas descartadas:**
- **Editor de texto plano sin preview**: El usuario no puede validar que el contenido es correcto hasta que lo usa.
- **Editor WYSIWYG complejo**: Demasiado costoso de implementar para el valor que aporta.

### 4. MetadataEditorShellViewModel: Especialización para JSON

```csharp
internal sealed partial class MetadataEditorShellViewModel : BaseTextEditorViewModel
{
    [ObservableProperty]
    private string _previewContent = string.Empty;
    
    [ObservableProperty]
    private bool _hasError;
    
    protected override void OnContentChangedCore(string value)
    {
        // Validar JSON + aplicar defaults + actualizar preview
        UpdatePreview(value);
    }
    
    private void UpdatePreview(string jsonContent)
    {
        try
        {
            // 1. Parsear JSON
            MetadataFile file = JsonSerializer.Deserialize<MetadataFile>(jsonContent);
            
            // 2. Cargar defaults (si existen)
            MetadataFile defaults = LoadDefaults(file.Header.Defaults);
            
            // 3. Aplicar merge
            MetadataFile merged = MergeDefaults(defaults, file);
            
            // 4. Serializar a JSON formateado para el preview
            PreviewContent = JsonSerializer.Serialize(merged, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            HasError = false;
        }
        catch (Exception ex)
        {
            PreviewContent = $"Error: {ex.Message}";
            HasError = true;
        }
    }
}
```

**Características:**
- Valida JSON en tiempo real (al escribir).
- Aplica defaults si el header los referencia.
- Muestra el objeto mergeado en el preview (JSON formateado o representación jerárquica).
- Si hay errores de sintaxis, muestra un mensaje de error en el preview.

## Consecuencias

### Positivas

- **Reutilización**: La lógica común (load/save/dirty) se escribe una vez en `BaseTextEditorViewModel`.
- **Extensibilidad**: Nuevos editores se crean heredando de la base.
- **Consistencia**: Todos los editores se comportan igual (guardar, cerrar, routing de comandos).
- **Experiencia de usuario**: Detección automática del editor adecuado por extensión.
- **Validación en tiempo real**: El preview permite validar que el contenido es correcto.

### Negativas

- **Complejidad de routing**: `NavigationService` debe decidir qué editor abrir según la extensión.
- **Mantenimiento de especializaciones**: Cada editor especializado requiere su propia lógica de preview/validación.

### Riesgos mitigados

- **Detección por extensión**: Simple y efectivo. Si se necesita más flexibilidad en el futuro, se puede añadir un registro de editores.
- **Preview de JSON**: Si el merge de defaults es complejo, se puede simplificar mostrando solo el JSON parseado sin defaults.

## Referencias

- Especificación técnica: `.agents/proyecto/especificaciones/metadata-editor.md`
- ADR-002: Sistema de Metadatos para Plantillas Scriban
- Arquitectura MVVM: `.agents/proyecto/arquitectura-y-dominio.md`
