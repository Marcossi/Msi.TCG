# Schema: Workflow

## Descripción

Define un flujo de trabajo automatizado con actividades y transiciones.

## Propiedades obligatorias

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `Id` | string | Identificador único del workflow |
| `Name` | string | Nombre legible del workflow |
| `Type` | string | Debe ser "Workflow" |
| `Namespace` | string | Namespace C# donde se generará el código |

## Propiedades opcionales

| Nombre | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Description` | string | "" | Descripción del workflow |
| `MaxRetries` | int | 3 | Número máximo de reintentos en caso de error |
| `IsActive` | bool | true | Indica si el workflow está activo |
| `Activities` | array | [] | Lista de actividades del workflow |

## Ejemplo JSON

```json
{
  "Id": "wf-001",
  "Name": "OrderProcessing",
  "Type": "Workflow",
  "Properties": [
    { "Name": "Namespace", "Type": "string", "Value": "MyApp.Workflows" },
    { "Name": "Description", "Type": "string", "Value": "Procesa órdenes" },
    { "Name": "MaxRetries", "Type": "int", "Value": 3 },
    { "Name": "IsActive", "Type": "bool", "Value": true }
  ]
}
```

## Uso en scripts

```scriban
{{ for element in get_all_elements() }}
  {{ if element.Type == "Workflow" }}
    Workflow: {{ element.Name }}
    Namespace: {{ element.Get<string>("Namespace") }}
    MaxRetries: {{ element.Get<int>("MaxRetries") }}
  {{ end }}
{{ end }}
```
