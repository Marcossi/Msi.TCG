# ADR-002: Sistema de Metadatos para Plantillas Scriban

## Estado

Accepted (2026-07-02)

## Contexto

La aplicación genera código mediante plantillas Scriban que reciben objetos de metadatos como diccionarios clave-valor. El caso de uso principal es generar código para un conjunto de entidades (Elements) y sus subentidades, donde cada entidad tiene propiedades con múltiples atributos (nombre, tipo, serialización, inicialización, etc.).

**Problema:**
- Actualmente los metadatos están hardcodeados en `DummyTemplateModel` dentro de `TemplatesService`. No hay modelo de dominio real ni persistencia.
- Se necesita un sistema para definir, almacenar y editar metadatos de ~30 entidades principales, cada una con 0-15 subentidades.
- Las propiedades de cada entidad tienen 10+ atributos (nombre, tipo, serialización, validación, UI hints, etc.).
- Los metadatos deben ser versionables en git, editables externamente (JSON raw) y mediante UI interna.
- El sistema debe ser extensible para futuras categorías de metadatos (ElementIds, Commands, Views, etc.).

**Fuerzas:**
- **Simplicidad en Scriban**: Las plantillas funcionan mejor con datos planos (strings, bools, listas). Objetos anidados con lógica, campos calculados o herencia compleja dificultan la escritura y depuración de templates.
- **Escalabilidad**: 30+ entidades con 10+ propiedades cada una = cientos de campos. Un único fichero monolítico sería inmanejable.
- **Extensibilidad**: Nuevas categorías de metadatos aparecerán con el tiempo (ElementIds, Commands, Screens).
- **Mantenibilidad**: Añadir un nuevo campo al esquema no debe requerir editar 30 ficheros manualmente.
- **Git-friendly**: Los diffs deben ser limpios y los merges manejables.

## Decisión

Implementar un **sistema de metadatos basado en JSON** con las siguientes características:

### 1. Formato: JSON

**Razones:**
- Infraestructura existente: `System.Text.Json` + `JsonProjectSerializer` ya están en el proyecto.
- Git-friendly: diffs línea a línea, merges manejables con formato indentado.
- Soporte universal: cualquier editor, lenguaje y herramienta.
- La verbosidad del JSON es un no-problema si la UI es el editor principal.

**Alternativas descartadas:**
- **YAML**: Requiere dependencia externa (YamlDotNet), tiene ambigüedades de parsing, no aporta valor suficiente.
- **TOML**: No escala bien con anidamiento profundo (propiedades con 10+ campos).
- **XML**: Verboso sin compensación clara.

### 2. Organización física: Un fichero por elemento, carpeta por categoría

```
proyecto.scribanproj
metadata/
├── elements/
│   ├── _defaults.json          ← defaults de la categoría (opcional)
│   ├── Workflow.json
│   ├── CustomerQuery.json
│   └── MainMenu.json
├── element-ids/
│   ├── _defaults.json
│   ├── WorkflowId.json
│   └── ViewId.json
└── commands/                   ← futuro
    └── _defaults.json
```

**Razones:**
- **Git diffs limpios**: modificar un elemento no toca los demás.
- **Escalabilidad**: 30 elementos = 30 ficheros manejables, no un monolito de 5000 líneas.
- **La UI los presenta como árbol**: `Elements > Workflow`, `ElementIds > WorkflowId`.
- **Subelementos embebidos**: las 0-15 subentidades de un Element van dentro de su JSON. Si un elemento crece demasiado, se puede partir en subcarpeta, pero no es el caso general.

**Alternativas descartadas:**
- **Un único fichero monolítico**: Inmanejable con 30+ entidades. Diffs ruidosos, merges conflictivos.
- **Base de datos**: Rompe git-friendly, dificulta edición externa, añade complejidad.

### 3. Formato JSON con cabecera separada

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
    "GenerateSerialization": true,
    "GenerateDto": true,
    "Properties": [ ... ],
    "SubElements": [ ... ]
  }
}
```

**Razones:**
- **`version` explícito desde el día 1**: Evita el anti-patrón "si no existe es v1".
- **`category`**: Permite validación al cargar (ej: "este fichero dice ser Element pero está en `element-ids/`" → warning).
- **`defaults` explícito**: Cada fichero apunta a su defaults concreto. Elimina la magia del `_defaults.json` por convención de nombre. Permite que subgrupos de elementos compartan defaults distintos.
- **Separación `header`/`data`**: El deserializador lee el header primero para decidir a qué DTO mapear el `data`, validar versión y localizar defaults. Si estuviera todo plano, el parser tendría que distinguir campos de infraestructura vs. dominio por nombre (frágil).

### 4. Modelo de dominio: Clases C# planas como contrato

El esquema de cada categoría se define como una clase C# plana (POCO). Estas clases son el **contrato** entre el JSON y Scriban:

```
JSON (almacenamiento) → Clase C# plana (contrato/deserialización) → ScriptObject (Scriban)
```

**Características:**
- Clases planas: strings, bools, listas. Sin lógica de negocio, sin propiedades calculadas, sin herencia.
- Campos derivados (ej: `NameLower`) se **almacenan en el JSON**, no se calculan. Razón: nombres irregulares pueden ser problemáticos si se autocalculan. El usuario tiene control total.
- El `MemberRenamer` de Scriban ya está configurado como `m => m.Name` (PascalCase). Las propiedades llegan tal cual a las plantillas.

**Alternativas descartadas:**
- **Clases con lógica, propiedades calculadas, herencia**: Intentado anteriormente. Se desbordó en complejidad, especialmente con defaults y autocalculados en objetos anidados. Los scripts de Scriban se volvieron difíciles de depurar.
- **Diccionarios dinámicos (`Dictionary<string, object>`)**: Pierde tipado fuerte, dificulta refactoring, no hay validación en compilación.

### 5. Defaults: Mecanismo simple de merge de un nivel

Un fichero `_defaults.json` por categoría (opcional) con valores por defecto. Al cargar un elemento:

1. Deserializar `_defaults.json` (si existe) → objeto base.
2. Deserializar `Workflow.json` → objeto override.
3. Merge: los campos presentes en el override pisan los del default. Los ausentes heredan el default.

**Reglas para evitar complejidad desbordada:**
- Defaults solo a nivel raíz de categoría y a nivel de SubElement. No defaults por propiedad individual.
- Solo valores estáticos. Cero campos calculados, cero lógica, cero expresiones.
- El JSON del elemento es **explícito**: si un campo está en el JSON, es porque el usuario lo quiso así. Si no está, viene del default.
- La UI muestra visualmente qué campos son heredados (ej: texto en gris) vs. explícitos (texto normal). El usuario puede hacer "Reset to default" en cualquier campo.
- El fichero de defaults se referencia explícitamente en el header (`"defaults": "_defaults.json"`), no por convención mágica.

**Alternativas descartadas:**
- **Sin defaults**: Cada elemento define todos sus valores explícitamente. Funciona pero es tedioso al añadir nuevos campos (hay que editar 30 ficheros).
- **Defaults complejos con cascadas de herencia**: Intentado anteriormente. Se desbordó en complejidad, especialmente con objetos anidados y relaciones entre defaults.
- **Defaults implícitos por convención de nombre** (ej: siempre buscar `_defaults.json`): Menos explícito, dificulta tener múltiples ficheros de defaults para subgrupos.

### 6. Pipeline hacia Scriban

```
MetadataService.LoadCategoryAsync("elements")
  → lee _defaults.json (si existe)
  → lee cada *.json de la carpeta
  → merge defaults + overrides
  → List<ElementMetadata> resuelta (100% plana, cero magia)
      ↓
TemplatesService.ProcessTemplateAsync(template, model)
  → importa la lista al ScriptObject
  → Scriban renderiza con datos planos
```

Scriban **nunca ve** los defaults ni el merge. Solo ve objetos planos y completos. Esto preserva la simplicidad validada anteriormente.

### 7. Extensibilidad para nuevas categorías

El sistema es abierto por diseño:
- Nueva categoría = nueva carpeta en `metadata/` + nueva clase POCO.
- Un `IMetadataRegistry` registra las categorías conocidas y su tipo asociado.
- El `MetadataService` es genérico: `LoadCategoryAsync<T>(string categoryName)`.
- Las plantillas Scriban acceden por nombre: `{{ elements }}`, `{{ elementIds }}`, `{{ commands }}`.

Añadir "Commands" en el futuro es:
1. Crear carpeta `metadata/commands/`.
2. Crear clase `CommandMetadata`.
3. Registrar en el registry.
4. Las plantillas ya pueden usar `{{ commands }}`.

### 8. Decisión diferida: Qué datos recibe Scriban

**Estado**: Diferida.

**Pregunta**: Cuando una plantilla Scriban se renderiza, ¿qué metadatos recibe?

**Opciones consideradas:**
- **Siempre todo**: El sistema inyecta todas las categorías siempre al `ScriptObject`. La plantilla ignora las que no necesita. Más simple, pero si hay 5 categorías con cientos de elementos, se cargan todas siempre.
- **Plantilla declara**: Cada plantilla declara en un header qué categorías necesita (ej: `{{ require: elements, elementIds }}`). El sistema solo carga esas. Más eficiente, pero añade complejidad a las plantillas.
- **Contexto decide**: El contexto de ejecución (desde dónde se lanza) determina qué categorías se inyectan. Ej: si renderizo desde el nodo "Workflow" del árbol, el sistema sabe que necesita Elements.

**Preferencia actual**: "Plantilla declara". Se decidirá cuando se implemente el pipeline de renderizado con metadatos reales.

## Consecuencias

### Positivas

- **Simplicidad en Scriban**: Las plantillas reciben datos planos, fáciles de entender y depurar.
- **Git-friendly**: Diffs limpios, merges manejables, versionado explícito.
- **Escalable**: 30+ elementos manejables, extensible a nuevas categorías.
- **Mantenible**: Añadir un campo al esquema es editar el POCO + defaults (si aplica). No requiere editar 30 ficheros si el default cubre el caso.
- **Explícito**: Sin magia. Defaults referenciados por nombre, campos derivados almacenados, versión explícita.
- **UI-friendly**: La estructura de carpetas se mapea naturalmente a un árbol en la UI.

### Negativas

- **Verbosidad del JSON**: Cada elemento repite la estructura completa. Mitigado por la UI como editor principal.
- **Merge de defaults**: Añade una capa de indirección. Mitigado por ser merge de un nivel, sin cascadas ni lógica.
- **Campos derivados almacenados**: `NameLower` y similares ocupan espacio en el JSON. Mitigado por ser pocos campos y aportar claridad.

### Riesgos mitigados

- **Defaults complejos**: Reglas estrictas (un nivel, sin lógica, sin cascadas) evitan el desborde de complejidad experimentado anteriormente.
- **Campos calculados problemáticos**: Almacenar campos derivados evita issues con nombres irregulares.
- **Monolito inmanejable**: Un fichero por elemento evita el problema de un JSON de 5000 líneas.
- **Versión implícita**: `fileFormatVersion` explícito desde el día 1 evita el anti-patrón de inferir versión por presencia/ausencia de campos.

## Referencias

- Especificación técnica: `.agents/proyecto/especificaciones/metadata-system.md`
- Modelo de proyecto: `.agents/proyecto/modelo-de-proyecto.md`
- Restricciones de Scriban: `.agents/proyecto/restricciones.md` (sección "Scriban: Solo métodos estáticos")
