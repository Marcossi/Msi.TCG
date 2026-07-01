# TemplatesService

> Descripción detallada de TemplatesService. Procesa plantillas Scriban: parsea, valida sintaxis y renderiza con un modelo de datos.

## Ubicación

- **Carpeta**: `Services/Templates/`
- **Fichero principal**: `TemplatesService.cs`
- **Ficheros asociados**:
  - `TemplateResult.cs` → Resultado del procesamiento

## Dependencias

Ninguna dependencia inyectada. Es un servicio autocontenido que usa directamente la librería Scriban.

## Métodos

### ProcessTemplateAsync(string templateContent)

Procesa una plantilla Scriban y devuelve el resultado renderizado.

Flujo:
1. Si `templateContent` está vacío → devuelve `TemplateResult.Success(string.Empty)`
2. Parsea la plantilla con `Template.Parse()` → valida errores de sintaxis
3. Si hay errores de sintaxis → devuelve `TemplateResult.Failure()` con los mensajes de error
4. Obtiene modelo de datos (Dummy por ahora) → `GetDummyModel()`
5. Renderiza la plantilla con el modelo → `RenderTemplateAsync()`
6. Devuelve el resultado

Configuración de Scriban:
- `MemberRenamer = member => member.Name` → Mantiene nombres PascalCase (no convierte a snake_case)
- `MemberFilter = member => true` → Permite acceso a miembros en objetos CLR anidados

ScriptObject construido:
```csharp
var scriptObject = new ScriptObject();
scriptObject.Add("Model", BuildScriptObject(model));
context.PushGlobal(scriptObject);
```

Esto expone el modelo como `{{ Model.Propiedad }}` en la plantilla.

## TemplateResult

Fichero: `TemplateResult.cs`

Métodos estáticos:
- `TemplateResult.Success(string output)` → Resultado exitoso
- `TemplateResult.Failure(string error)` → Resultado con error

## Modelo de datos

Actualmente usa un modelo Dummy (`DummyTemplateModel`). En el futuro, se conectará con los modelos de datos reales referenciados por el proyecto.

## Estructura de ficheros

```
Services/Templates/
├── TemplatesService.cs       ← Procesamiento de plantillas
└── TemplateResult.cs         ← Resultado del procesamiento
```

Documentación externa de referencia: https://scriban.github.io/docs/
