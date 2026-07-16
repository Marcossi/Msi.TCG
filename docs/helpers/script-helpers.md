# Script Helpers

Funciones C# disponibles en scripts Scriban.

## get_all_elements()

Retorna todos los Elements del catálogo.

**Retorna:** `IEnumerable<Element>`

**Ejemplo:**
```scriban
{{ for element in get_all_elements() }}
  {{ element.Name }}
{{ end }}
```

## get_elements_by_type(type)

Retorna todos los Elements de un tipo específico.

**Parámetros:**
- `type` (string): Tipo del Element (ej: "Workflow")

**Retorna:** `IEnumerable<Element>`

**Ejemplo:**
```scriban
{{ for element in get_elements_by_type("Workflow") }}
  {{ element.Name }}
{{ end }}
```

## pascal_case(input)

Convierte un string a PascalCase.

**Parámetros:**
- `input` (string): String de entrada

**Retorna:** `string`

**Ejemplo:**
```scriban
{{ "order_processing" | pascal_case }}  → OrderProcessing
{{ "orderProcessing" | pascal_case }}   → OrderProcessing
```

## camel_case(input)

Convierte un string a camelCase.

**Parámetros:**
- `input` (string): String de entrada

**Retorna:** `string`

**Ejemplo:**
```scriban
{{ "OrderProcessing" | camel_case }}    → orderProcessing
{{ "order_processing" | camel_case }}   → orderProcessing
```

## write_to_file(path, content)

Escribe contenido a un fichero.

**Parámetros:**
- `path` (string): Ruta relativa a la raíz del proyecto
- `content` (string): Contenido a escribir

**Ejemplo:**
```scriban
{{ write_to_file("src/MyClass.cs", "public class MyClass {}") }}
```
