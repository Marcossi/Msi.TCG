# ADR-005: Arquitectura de Scripts y Modelo de Datos

## Estado

Accepted (2026-07-06)

## Contexto

La aplicación gestiona un proyecto que contiene:
- **N objetos de modelo** (Elements): entidades de dominio (Workflow, Vista, etc.) cargadas desde JSON
- **M scripts Scriban**: plantillas que generan código C# a partir de los Elements

**Problema principal:**
¿Cómo se relacionan los scripts con los Elements? No todos los scripts usan todos los Elements, y el flujo de trabajo es iterativo:
- A veces cambia el modelo (nuevas propiedades)
- A veces cambian los scripts (nueva lógica de generación)
- A veces cambian ambos

**Fuerzas:**
- Flexibilidad: el usuario debe poder iterar rápidamente sin overhead
- Control: el script debe decidir qué generar y dónde
- Simplicidad: evitar metadata duplicada o configuraciones complejas
- Pragmatismo: GenerateAll + diff para verificar cambios

## Decisión

### 1. Metamodelo universal (Element + ElementProperty)

**Clase Element:**
```csharp
public class Element
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }  // "Workflow", "Vista", etc.
    public List<ElementProperty> Properties { get; set; } = new();
}
```

**Clase ElementProperty:**
```csharp
public class ElementProperty
{
    public string Name { get; set; }
    public string Type { get; set; }  // "string", "int", "bool", "Activity", etc.
    public object Value { get; set; }
    public bool IsRequired { get; set; }
}
```

**Características clave:**
- 1 clase Element (metamodelo) para todas las instancias
- ElementProperty tiene campo `Type` (como reflection) para permitir switch por tipo en scripts
- Acceso seguro con método `Get<T>(name)` para errores explícitos vs silenciosos
- JSON como formato de datos (flexible, sin recompilar)

### 2. Patrón Capture con write_to_file

Los scripts tienen control total de qué generar y dónde usando el patrón Capture de Scriban:

```scriban
{{ for element in get_all_elements() }}
  {{ if element.Type == "Workflow" }}
    {{ capture content }}
namespace {{ element.Get<string>("Namespace") }};

public class {{ element.Name }}Dto
{
    {{ for prop in element.Properties }}
      {{ if prop.Type == "string" }}
    public string {{ prop.Name | pascal_case }} { get; set; }
      {{ else if prop.Type == "int" }}
    public int {{ prop.Name | pascal_case }} { get; set; }
      {{ end }}
    {{ end }}
}
    {{ end }}
    {{ write_to_file("src/" + element.Get<string>("Namespace") + "/" + element.Name + "Dto.cs", content) }}
  {{ end }}
{{ end }}
```

**Características clave:**
- `write_to_file(path, content)` como primitiva flexible
- Implementación puede variar: disco, memoria, preview, caché
- Sin metadata obligatoria (el script es auto-contenido)
- Un script puede generar N ficheros en rutas diferentes

**Detalles pendientes de definir:**
- Rutas (relativas a raíz del proyecto o absolutas)
- Overwrite (silencioso o error)
- Encoding (UTF-8 con/sin BOM)
- Line endings (CRLF o LF)
- Creación automática de directorios

### 3. Preview con primer elemento (MVP)

**Primera iteración (MVP):**
- Preview muestra el primer output generado por el script
- Sin selector de outputs

**Evolución futura:**
- Combo con todos los outputs capturados
- Usuario selecciona qué output previsualizar
- Capturar todos los outputs en memoria, mostrar el seleccionado

**Performance:**
- Debounce 1s (ya implementado en editor actual)
- Timeout de ejecución (ej: 30s) para evitar loops infinitos

### 4. Carga tolerante a errores

**Al abrir proyecto:**
- Leer todos los JSONs y scripts
- Si un JSON tiene error: marcar con aspa roja, ignorar (no está en la colección)
- Si un script tiene error: marcar con aspa roja, ignorar (preview y generación lo saltan)

**Al modificar fichero:**
- Re-cargar y validar automáticamente
- Aplicar cambios a la colección (JSON) o al preview/generación (script)

**Manejo de errores:**
- JSON inválido → error bloqueante para ese fichero, pero proyecto abre
- Propiedades faltantes → warning, pero permite cargar
- Validación con `Get<T>()` en runtime (errores explícitos)

### 5. Estructura de ficheros libre

**Identificación por extensión:**
- `.scriban` = script
- `.json` = datos

**Organización:**
- Carpetas opcionales para organización del usuario
- Sistema trata rutas como absolutas (carpetas no afectan al sistema)
- Usuario decide organización (ej: `scripts/vistas/`, `scripts/serialization/`)

**Ejemplo:**
```
proyecto/
├── scripts/
│   ├── workflow-dto.scriban
│   ├── vistas/
│   │   ├── crud.scriban
│   │   └── readonly.scriban
│   └── serialization/
│       └── json.scriban
└── data/
    ├── workflows.json
    └── vistas.json
```

### 6. Helpers C# (MVP)

**Registro:**
- Métodos fijos de la app en `ScriptHelpers`
- Registro simple con `ScriptObject.Import(typeof(ScriptHelpers))`

**Helpers iniciales:**
```csharp
public static class ScriptHelpers
{
    public static IEnumerable<Element> GetAllElements() { ... }
    public static string PascalCase(string input) { ... }
    // write_to_file se registra como función custom en TemplateContext
}
```

**Descubribilidad (MVP):**
- Documentación markdown con lista de helpers
- Usuario (nosotros) conoce los helpers disponibles

**Evolución futura:**
- Panel en UI con lista de helpers
- Autocompletado en editor (requiere Language Server)

### 7. Integración con UI existente

**Cambios necesarios (MVP):**
- **TemplateEditorShellViewModel:**
  - Preview muestra primer output capturado
  - Generate ejecuta script con `write_to_file` a disco
  - GenerateAll ejecuta todos los scripts

- **ProjectExplorer:**
  - Mostrar scripts y datos como listas (rutas no importan)
  - Marcar ficheros con error (aspa roja)

- **IProjectContext:**
  - Añadir `IElementCatalog` (colección de Elements)
  - Añadir lista de scripts
  - Añadir lista de datos

## Alternativas consideradas

### A. Metadata declarativa (.scriban.json)

Cada script tiene un `.scriban.json` que declara qué Elements aplica y outputPath.

**Descartada porque:**
- Overhead de mantenimiento (metadata + lógica del script)
- Duplicación (metadata puede quedar desincronizada)
- El script ya tiene toda la información con el patrón Capture

### B. Asociación en el proyecto (.scribanproj)

El proyecto define qué scripts aplican a qué Elements.

**Descartada porque:**
- Acoplamiento fuerte
- Difícil mantener sincronizado
- Centraliza información que debería estar en el script

### C. Convención de nombres

`workflow-*.scriban` aplica a Workflow, `vista-*.scriban` aplica a Vista.

**Descartada porque:**
- Rígida
- No escala con scripts transversales
- Nombres poco descriptivos

### D. Modelo tipado (30 clases C#)

Crear una clase C# por cada tipo de Element (Workflow, Vista, etc.).

**Descartada porque:**
- Pierde flexibilidad (cambiar modelo = cambiar clase + recompilar)
- Verboso (30 clases × 10-20 propiedades = 300-600 líneas)
- Duplicación de definición (clase + instanciación)
- No encaja con el flujo iterativo del usuario

### E. Source generators

Definir schema en JSON, generar clases C# automáticamente.

**Descartada porque:**
- Complejidad de implementación (source generators)
- Requiere recompilar al cambiar schema
- Overkill para el caso de uso

## Consecuencias

### Positivas

- **Flexibilidad máxima:** scripts deciden qué generar y dónde
- **Control total:** sin restricciones de metadata
- **Iteración rápida:** cambiar JSON sin recompilar
- **Sin overhead:** no hay metadata que mantener
- **Scripts auto-contenidos:** toda la lógica está en el script
- **Estructura libre:** usuario organiza como quiera

### Negativas

- **Sin type safety:** errores de propiedades en runtime (mitigado con `Get<T>()`)
- **Descubribilidad limitada:** sin IntelliSense para propiedades (mitigado con documentación)
- **Verbosidad en scripts:** acceso a properties más largo (mitigado con helpers)
- **Sin validación de schema:** JSONs pueden tener estructura incorrecta (mitigado con validación al cargar)

### Riesgos mitigados

- **Get<T>() para errores explícitos:** evita errores silenciosos
- **Validación al cargar JSON:** detecta errores temprano
- **Schemas documentados en markdown:** mejora descubribilidad
- **Tolerancia a errores:** proyecto abre aunque haya ficheros inválidos
- **Preview simplificado (MVP):** primer elemento, ya complicaremos después

## Plan de implementación

La implementación se divide en 4 fases secuenciales. Cada fase es auto-contenida y verificable independientemente. Un agente nuevo puede implementar cada fase leyendo solo el ADR + la especificación de esa fase.

| Fase | Especificación | Objetivo |
|------|----------------|----------|
| 1 | [fase-1-modelo-datos.md](../especificaciones/fase-1-modelo-datos.md) | Modelo de datos + carga de JSONs |
| 2 | [fase-2-motor-scripts.md](../especificaciones/fase-2-motor-scripts.md) | Motor de scripts + helpers C# |
| 3 | [fase-3-integracion-ui.md](../especificaciones/fase-3-integracion-ui.md) | Integración con UI |
| 4 | [fase-4-robustez-ux.md](../especificaciones/fase-4-robustez-ux.md) | Robustez y UX |

**Dependencias:** Fase 1 → Fase 2 → Fase 3 → Fase 4

## Referencias

- Documentación de Scriban: `docs/agents/libraries-doc/Scriban-7.2.5/`
- Modelo de proyecto: `docs/agents/proyecto/modelo-de-proyecto.md`
- Arquitectura y dominio: `docs/agents/proyecto/arquitectura-y-dominio.md`
