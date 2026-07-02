# Especificación técnica: Sistema de Metadatos

> Detalles de implementación del sistema de metadatos para plantillas Scriban.
> Referencia: ADR-002

## Estructura de carpetas en el proyecto de usuario

```
proyecto.scribanproj
metadata/
├── elements/
│   ├── _defaults.json          ← defaults de la categoría (opcional)
│   ├── Workflow.json
│   ├── CustomerQuery.json
│   ├── MainMenu.json
│   └── ...                     ← un fichero por elemento
├── element-ids/
│   ├── _defaults.json
│   ├── WorkflowId.json
│   ├── ViewId.json
│   └── ...
└── commands/                   ← futuro
    ├── _defaults.json
    └── ...
```

**Reglas:**
- La carpeta `metadata/` es obligatoria en todo proyecto que use metadatos.
- Cada subcarpeta (`elements/`, `element-ids/`, etc.) representa una categoría.
- El nombre de la carpeta debe coincidir con el `category` declarado en los headers de sus ficheros.
- El fichero de defaults es opcional. Si no existe, no hay valores por defecto para esa categoría.
- Cada elemento es un fichero `.json` independiente.
- Los subelementos se embeben dentro del JSON de su elemento padre.

## Formato JSON

### Estructura general

Todo fichero de metadatos tiene esta estructura:

```json
{
  "header": {
    "version": 1,
    "category": "Element",
    "defaults": "_defaults.json"
  },
  "data": {
    // campos específicos de la categoría
  }
}
```

### Header

```csharp
public sealed class MetadataFileHeader
{
    public int Version { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Defaults { get; set; }
}
```

**Campos:**

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `version` | `int` | Sí | Versión del formato. Permite migraciones futuras. |
| `category` | `string` | Sí | Categoría del metadato (ej: `"Element"`, `"ElementId"`, `"Command"`). Debe coincidir con el nombre de la carpeta contenedora. |
| `defaults` | `string?` | No | Nombre del fichero de defaults (relativo a la misma carpeta). Si es `null` o está ausente, no hay defaults. |

**Validaciones al cargar:**
- `version` debe ser un entero positivo.
- `category` no puede estar vacío.
- `category` debe coincidir con el nombre de la carpeta contenedora (warning si no coincide).
- Si `defaults` está presente, el fichero referenciado debe existir en la misma carpeta.

### Data: ElementMetadata

```csharp
public sealed class ElementMetadata
{
    // Identificación
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Flags de generación
    public bool GenerateSerialization { get; set; }
    public bool GenerateDto { get; set; }
    public bool GeneratePersistence { get; set; }
    public bool GenerateValidation { get; set; }
    
    // Composición
    public List<PropertyMetadata> Properties { get; set; } = [];
    public List<SubElementMetadata> SubElements { get; set; } = [];
}
```

**Campos obligatorios:**
- `Id`: Identificador único del elemento dentro de su categoría.
- `Name`: Nombre legible del elemento.

**Campos derivados almacenados:**
- `NameLower`: Versión en minúsculas de `Name`. Se almacena explícitamente para evitar problemas con nombres irregulares.
- `ClassName`: Nombre de la clase C# generada. Puede diferir de `Name` si hay convenciones de nomenclatura específicas.

**Flags de generación:**
- Booleanos que controlan qué plantillas se aplican a este elemento.
- Las plantillas Scriban consultan estos flags para decidir si generar código o hacer skip.
- Ejemplo: `{{ if element.GenerateDto }} ... {{ end }}`

### Data: PropertyMetadata

```csharp
public sealed class PropertyMetadata
{
    // Identificación
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Accesibilidad y visibilidad
    public string Accessibility { get; set; } = "public";
    public bool IsReadOnly { get; set; }
    public bool IsNullable { get; set; }
    
    // Serialización
    public string SerializationToken { get; set; } = string.Empty;
    public bool SerializeAsReference { get; set; }
    public string SerializationFormat { get; set; } = string.Empty;
    
    // Inicialización y copia
    public string DefaultValue { get; set; } = string.Empty;
    public string InitializationMode { get; set; } = "default";
    public string CopyMode { get; set; } = "shallow";
    
    // Validación
    public bool IsRequired { get; set; }
    public string ValidationRules { get; set; } = string.Empty;
}
```

**Campos obligatorios:**
- `Name`: Nombre de la propiedad.
- `Type`: Tipo de dato (ej: `"string"`, `"int"`, `"DateTime"`, `"List<OrderItem>"`).

**Campos de serialización:**
- `SerializationToken`: Token/clave usado en serialización (ej: `"wfName"` para `Name`).
- `SerializeAsReference`: Si es `true`, serializar como referencia (ID) en lugar de objeto completo.
- `SerializationFormat`: Formato específico (ej: `"iso8601"` para fechas, `"base64"` para binarios).

**Campos de inicialización:**
- `DefaultValue`: Valor por defecto como string (ej: `"0"`, `"string.Empty"`, `"DateTime.UtcNow"`).
- `InitializationMode`: Cómo se inicializa (ej: `"default"`, `"lazy"`, `"eager"`).
- `CopyMode`: Cómo se copia (ej: `"shallow"`, `"deep"`, `"reference"`).

### Data: SubElementMetadata

```csharp
public sealed class SubElementMetadata
{
    // Identificación
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Relación con el padre
    public string RelationType { get; set; } = "composition";
    public bool IsCollection { get; set; }
    
    // Propiedades propias
    public List<PropertyMetadata> Properties { get; set; } = [];
}
```

**Campos obligatorios:**
- `Id`: Identificador único del subelemento dentro de su elemento padre.
- `Name`: Nombre del subelemento.

**Relación con el padre:**
- `RelationType`: Tipo de relación (ej: `"composition"`, `"aggregation"`, `"reference"`).
- `IsCollection`: Si es `true`, el subelemento es una colección (ej: `List<OrderItem>`).

### Ejemplo completo: Workflow.json

```json
{
  "header": {
    "version": 1,
    "category": "Element",
    "defaults": "_defaults.json"
  },
  "data": {
    "Id": "Workflow",
    "Name": "Workflow",
    "NameLower": "workflow",
    "ClassName": "Workflow",
    "Description": "Define un flujo de trabajo automatizado",
    "GenerateSerialization": true,
    "GenerateDto": true,
    "GeneratePersistence": true,
    "GenerateValidation": true,
    "Properties": [
      {
        "Name": "Name",
        "NameLower": "name",
        "Type": "string",
        "Description": "Nombre del workflow",
        "Accessibility": "public",
        "IsReadOnly": false,
        "IsNullable": false,
        "SerializationToken": "wfName",
        "SerializeAsReference": false,
        "SerializationFormat": "",
        "DefaultValue": "string.Empty",
        "InitializationMode": "default",
        "CopyMode": "shallow",
        "IsRequired": true,
        "ValidationRules": "MaxLength(100)"
      },
      {
        "Name": "Status",
        "NameLower": "status",
        "Type": "WorkflowStatus",
        "Description": "Estado actual del workflow",
        "Accessibility": "public",
        "IsReadOnly": false,
        "IsNullable": false,
        "SerializationToken": "wfStatus",
        "SerializeAsReference": false,
        "SerializationFormat": "enum-string",
        "DefaultValue": "WorkflowStatus.Draft",
        "InitializationMode": "default",
        "CopyMode": "shallow",
        "IsRequired": true,
        "ValidationRules": ""
      }
    ],
    "SubElements": [
      {
        "Id": "WorkflowStep",
        "Name": "WorkflowStep",
        "NameLower": "workflowStep",
        "Description": "Paso individual del workflow",
        "RelationType": "composition",
        "IsCollection": true,
        "Properties": [
          {
            "Name": "Order",
            "NameLower": "order",
            "Type": "int",
            "Description": "Orden de ejecución del paso",
            "Accessibility": "public",
            "IsReadOnly": false,
            "IsNullable": false,
            "SerializationToken": "stepOrder",
            "SerializeAsReference": false,
            "SerializationFormat": "",
            "DefaultValue": "0",
            "InitializationMode": "default",
            "CopyMode": "shallow",
            "IsRequired": true,
            "ValidationRules": "Range(0, 1000)"
          }
        ]
      }
    ]
  }
}
```

## Contratos de servicios

### IMetadataRegistry

```csharp
public interface IMetadataRegistry
{
    void RegisterCategory<T>(string categoryName) where T : class, new();
    Type? GetCategoryType(string categoryName);
    IReadOnlyList<string> GetRegisteredCategories();
}
```

**Responsabilidad:**
- Registrar las categorías de metadatos conocidas y su tipo POCO asociado.
- Permitir consulta de categorías registradas.

**Uso:**
```csharp
// En DependencyInjection.cs o bootstrap
registry.RegisterCategory<ElementMetadata>("elements");
registry.RegisterCategory<ElementIdMetadata>("element-ids");
// Futuro:
// registry.RegisterCategory<CommandMetadata>("commands");
```

### IMetadataService

```csharp
public interface IMetadataService
{
    Task<List<T>> LoadCategoryAsync<T>(string categoryName) where T : class, new();
    Task<T?> LoadElementAsync<T>(string categoryName, string elementId) where T : class, new();
    Task SaveElementAsync<T>(string categoryName, T element) where T : class, new();
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<IReadOnlyList<string>> GetElementIdsAsync(string categoryName);
}
```

**Responsabilidad:**
- Cargar metadatos de una categoría completa (todos los elementos).
- Cargar un elemento individual.
- Guardar un elemento.
- Listar categorías y elementos disponibles.

**Flujo de carga:**

```csharp
public async Task<List<T>> LoadCategoryAsync<T>(string categoryName) where T : class, new()
{
    // 1. Validar que la categoría está registrada
    Type categoryType = _registry.GetCategoryType(categoryName)
        ?? throw new InvalidOperationException($"Categoría '{categoryName}' no registrada");
    
    // 2. Obtener carpeta de la categoría
    string categoryPath = Path.Combine(_context.CurrentProject!.FolderPath, "metadata", categoryName);
    if (!Directory.Exists(categoryPath))
        return [];
    
    // 3. Buscar fichero de defaults (si existe)
    string? defaultsFile = FindDefaultsFile(categoryPath);
    T? defaults = defaultsFile != null
        ? await LoadMetadataFileAsync<T>(defaultsFile)
        : null;
    
    // 4. Cargar todos los elementos
    List<T> elements = [];
    foreach (string file in Directory.EnumerateFiles(categoryPath, "*.json"))
    {
        if (Path.GetFileName(file).StartsWith("_"))
            continue; // saltar _defaults.json y otros ficheros especiales
        
        T element = await LoadMetadataFileAsync<T>(file);
        
        // 5. Merge con defaults (si existen)
        if (defaults != null)
            MergeDefaults(defaults, element);
        
        elements.Add(element);
    }
    
    return elements;
}
```

### Merge de defaults

```csharp
private void MergeDefaults<T>(T defaults, T target) where T : class, new()
{
    // Merge de un nivel: solo propiedades de T, no recursivo en listas
    PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    
    foreach (PropertyInfo prop in properties)
    {
        // Solo hacer merge en propiedades simples (no listas)
        if (IsCollectionType(prop.PropertyType))
            continue;
        
        object? targetValue = prop.GetValue(target);
        object? defaultValue = prop.GetValue(defaults);
        
        // Si el target tiene el valor por defecto del tipo, usar el default
        if (IsDefaultValue(targetValue, prop.PropertyType) && defaultValue != null)
        {
            prop.SetValue(target, defaultValue);
        }
    }
}

private bool IsDefaultValue(object? value, Type type)
{
    if (value == null)
        return true;
    
    if (type == typeof(string))
        return string.IsNullOrEmpty((string)value);
    
    if (type == typeof(bool))
        return !(bool)value; // false es el default
    
    if (type.IsValueType)
        return value.Equals(Activator.CreateInstance(type));
    
    return false;
}
```

**Reglas de merge:**
- Solo se hace merge en propiedades simples (strings, bools, ints). No en listas (`Properties`, `SubElements`).
- Si el target tiene el valor por defecto del tipo (string vacío, bool false, int 0), se usa el valor del default.
- Si el target tiene un valor explícito (aunque sea el mismo que el default), se respeta.
- Las listas (`Properties`, `SubElements`) no se mergean. Si el elemento define propiedades, son las suyas. Si no define, está vacío.

**Defaults en SubElements:**
- Los SubElements pueden tener su propio fichero de defaults dentro de la carpeta del elemento padre.
- Ejemplo: `metadata/elements/Workflow/_defaults.json` aplica a los SubElements de Workflow.
- El merge sigue las mismas reglas: un nivel, sin cascadas.

## Flujo hacia Scriban

### Carga y preparación

```csharp
// En TemplatesService o un servicio coordinador
public async Task<TemplateResult> ProcessTemplateWithMetadataAsync(
    string templateContent,
    params string[] categories)
{
    // 1. Cargar metadatos de las categorías solicitadas
    Dictionary<string, object> metadataObjects = [];
    
    foreach (string category in categories)
    {
        Type categoryType = _registry.GetCategoryType(category)!;
        
        // Llamada genérica vía reflexión (o pattern matching)
        object elements = await LoadCategoryDynamicAsync(category);
        metadataObjects[category] = elements;
    }
    
    // 2. Parsear template
    Template template = ParseTemplate(templateContent);
    
    // 3. Construir ScriptObject con metadatos
    ScriptObject scriptObject = [];
    foreach (KeyValuePair<string, object> kvp in metadataObjects)
    {
        scriptObject.Add(kvp.Key, kvp.Value);
    }
    
    // 4. Renderizar
    return await RenderTemplateAsync(template, scriptObject);
}
```

### Acceso desde Scriban

```scriban
{{ for element in elements }}
  Elemento: {{ element.Name }} ({{ element.NameLower }})
  Clase: {{ element.ClassName }}
  
  {{ if element.GenerateDto }}
    Generando DTO para {{ element.Name }}...
    
    {{ for prop in element.Properties }}
      Propiedad: {{ prop.Name }} : {{ prop.Type }}
      Token serialización: {{ prop.SerializationToken }}
      {{ if prop.IsRequired }}*requerido{{ end }}
    {{ end }}
    
    {{ for sub in element.SubElements }}
      SubElemento: {{ sub.Name }} ({{ if sub.IsCollection }}colección{{ else }}único{{ end }})
      {{ for prop in sub.Properties }}
        - {{ prop.Name }} : {{ prop.Type }}
      {{ end }}
    {{ end }}
  {{ end }}
{{ end }}
```

### Inyección de métodos estáticos

```csharp
// Registrar métodos estáticos útiles para metadatos
scriptObject.Import(nameof(MetadataHelpers.FormatPropertyName), MetadataHelpers.FormatPropertyName);
scriptObject.Import(nameof(MetadataHelpers.GenerateClassName), MetadataHelpers.GenerateClassName);
```

**Ejemplo de helper:**

```csharp
public static class MetadataHelpers
{
    public static string FormatPropertyName(string name, string convention = "pascal")
    {
        return convention switch
        {
            "camel" => char.ToLowerInvariant(name[0]) + name.Substring(1),
            "pascal" => char.ToUpperInvariant(name[0]) + name.Substring(1),
            _ => name
        };
    }
    
    public static string GenerateClassName(string elementName, string suffix = "")
    {
        return elementName + suffix;
    }
}
```

## Validaciones

### Al cargar un fichero

1. **Header válido:**
   - `version` es entero positivo.
   - `category` no está vacío.
   - `category` coincide con el nombre de la carpeta contenedora (warning si no).

2. **Defaults existe:**
   - Si `header.defaults` está presente, el fichero referenciado debe existir.

3. **Data válido:**
   - Deserialización exitosa al tipo POCO registrado.
   - Campos obligatorios presentes (`Id`, `Name` para elementos).
   - `Id` único dentro de la categoría.

### Al guardar un fichero

1. **Header completo:**
   - `version`, `category` obligatorios.
   - `defaults` opcional pero si está presente debe ser un nombre de fichero válido.

2. **Data válido:**
   - Campos obligatorios completos.
   - `Id` no vacío, sin caracteres inválidos para nombre de fichero.

3. **Consistencia:**
   - `category` coincide con la carpeta donde se guarda.

## Migraciones de versión

### Estrategia

- Cada versión del formato tiene un número entero (`version: 1`, `version: 2`, etc.).
- El `MetadataService` detecta la versión del header y aplica la migración correspondiente si es necesario.
- Las migraciones son funciones puras: `Told MigrateV1ToV2(Told old)`.

### Ejemplo de migración futura

```csharp
private ElementMetadata MigrateV1ToV2(ElementMetadataV1 old)
{
    return new ElementMetadata
    {
        Id = old.Id,
        Name = old.Name,
        NameLower = old.NameLower,
        ClassName = old.ClassName,
        // Nuevo campo en v2
        GenerateApiEndpoints = true, // default para elementos migrados
        Properties = old.Properties.Select(MigratePropertyV1ToV2).ToList(),
        SubElements = old.SubElements.Select(MigrateSubElementV1ToV2).ToList()
    };
}
```

### Política de migración

- **Automática al cargar**: Si el fichero tiene versión antigua, se migra en memoria.
- **Guardado explícito**: El fichero migrado no se sobreescribe automáticamente. El usuario decide cuándo guardar.
- **Backup**: Antes de guardar una migración, se crea un backup del fichero original (`.json.v1.bak`).

## Testing

### Unit tests

- **MetadataFileHeader**: Parsing de headers válidos e inválidos.
- **MergeDefaults**: Merge de un nivel, propiedades simples, listas no mergeadas.
- **Validaciones**: Ficheros con headers inválidos, campos obligatorios ausentes, IDs duplicados.

### Integration tests

- **LoadCategoryAsync**: Cargar categoría completa con defaults y overrides.
- **SaveElementAsync**: Guardar elemento y recargarlo.
- **Migraciones**: Cargar fichero v1, migrar a v2, guardar, recargar.

### Ejemplo de test

```csharp
[Fact]
public async Task LoadCategoryAsync_WithDefaults_MergesCorrectly()
{
    // Arrange
    string categoryPath = CreateTempCategory(
        defaults: """
        {
          "header": { "version": 1, "category": "elements" },
          "data": {
            "GenerateSerialization": true,
            "GenerateDto": true
          }
        }
        """,
        element: """
        {
          "header": { "version": 1, "category": "elements", "defaults": "_defaults.json" },
          "data": {
            "Id": "Workflow",
            "Name": "Workflow",
            "NameLower": "workflow",
            "ClassName": "Workflow",
            "GenerateDto": false
          }
        }
        """
    );
    
    // Act
    List<ElementMetadata> elements = await _service.LoadCategoryAsync<ElementMetadata>("elements");
    
    // Assert
    ElementMetadata workflow = elements.Single();
    workflow.GenerateSerialization.ShouldBeTrue(); // del default
    workflow.GenerateDto.ShouldBeFalse(); // override explícito
}
```

## Consideraciones de UI

### Editor de metadatos

- **Árbol de categorías**: `Elements > Workflow`, `ElementIds > WorkflowId`.
- **Formulario por elemento**: Campos organizados en secciones (Identificación, Flags, Properties, SubElements).
- **Indicadores visuales**:
  - Campos heredados de defaults: texto en gris, tooltip "Heredado de _defaults.json".
  - Campos explícitos: texto normal.
  - Botón "Reset to default" en cada campo heredado.

### Editor de defaults

- **Formulario especial**: Similar al de elementos, pero sin `Id` ni `Name`.
- **Acceso**: Desde el nodo raíz de la categoría (ej: `Elements > Defaults`).

### Validación en tiempo real

- **Campos obligatorios**: Marcar en rojo si están vacíos.
- **IDs duplicados**: Warning si el ID ya existe en la categoría.
- **Referencias rotas**: Warning si `defaults` apunta a un fichero inexistente.

## Futuras extensiones

### Categorías adicionales

- **ElementIds**: Metadatos para identificadores de elementos (WorkflowId, ViewId, etc.).
- **Commands**: Metadatos para comandos del sistema (Save, Copy, Paste, etc.).
- **Views**: Metadatos para pantallas y vistas.
- **Services**: Metadatos para servicios y sus interfaces.

### Features avanzadas

- **Plantillas declaran dependencias**: Header en el template `.scriban` que declara qué categorías necesita.
- **Validación cross-category**: Validar que las referencias entre categorías son válidas (ej: un ElementId referencia un Element existente).
- **Import/Export**: Importar metadatos desde fuentes externas (BD, API, esquema XML).
- **Generación de esqueletos**: UI para generar un elemento nuevo a partir de defaults o de otro elemento existente.
