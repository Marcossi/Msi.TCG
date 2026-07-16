# Recursos de prueba

> Carpeta `resources/` con proyectos de ejemplo para desarrollo y debugging.

## Propósito

La carpeta `resources/` contiene proyectos de ejemplo utilizados para desarrollo, pruebas y debugging de la aplicación.

Estos proyectos representan casos de uso reales del sistema de plantillas Scriban y permiten validar el comportamiento de la aplicación sin crear datos desde cero.

## Convención de referencia

Cuando se mencione:

- **"scripts de Scriban"** o **"plantillas de prueba"** → se refiere a los archivos `.scriban` dentro de `resources/`
- **"proyecto de ejemplo"** o **"proyecto de prueba"** → se refiere a las carpetas de proyecto dentro de `resources/`
- **"errores en el proyecto de ejemplo"** → validar contra la estructura y contenido de `resources/`

## Estructura actual

```
resources/
└── ProjectSample1/
    ├── TestProject.scribanproj
    ├── Model/
    │   └── Elements/
    │       ├── _defaults.json
    │       ├── Workflow.json
    │       ├── MainMenu.json
    │       └── CustomerQuery.json
    └── Templates/
        ├── Sample1.scriban
        ├── Sample2.scriban
        └── Sample3.scriban
```

### ProjectSample1

Proyecto de prueba básico que contiene:

- **TestProject.scribanproj**: Archivo de proyecto mínimo con formato JSON
- **Model/Elements/**: Datos de ejemplo en formato JSON (elementos del modelo de dominio)
- **Templates/**: Plantillas Scriban de ejemplo para pruebas de renderizado

## Uso durante el desarrollo

### Abrir el proyecto de ejemplo

1. Ejecutar la aplicación: `dotnet run --project src/Msi.TemplateCodeGenerator/Msi.TemplateCodeGenerator.csproj`
2. Abrir proyecto: seleccionar `resources/ProjectSample1/TestProject.scribanproj`
3. Explorar las plantillas en `Templates/` y los datos en `Model/Elements/`

### Debugging de errores

Cuando se reporten errores relacionados con:

- **Carga de proyectos**: validar contra `TestProject.scribanproj`
- **Renderizado de plantillas**: probar con `Sample1.scriban`, `Sample2.scriban`, `Sample3.scriban`
- **Modelo de datos**: verificar estructura contra los archivos JSON en `Model/Elements/`

### Agregar nuevos proyectos de ejemplo

Para agregar un nuevo proyecto de ejemplo:

1. Crear una carpeta bajo `resources/` con nombre descriptivo (ej: `ProjectSample2/`)
2. Incluir archivo `.scribanproj` válido
3. Agregar estructura de `Model/` y `Templates/` según sea necesario
4. Actualizar este documento para reflejar la nueva estructura

## Limitaciones

- Los proyectos en `resources/` son **solo para desarrollo y pruebas**
- No forman parte del paquete de distribución
- No deben contener datos sensibles o información real de usuarios
- Los cambios en estos archivos no afectan el comportamiento de la aplicación en producción

## Documentos relacionados

- `modelo-de-proyecto.md` — estructura conceptual del proyecto `.scribanproj`
- `especificaciones/fase-1-modelo-datos.md` — modelo Element + ElementProperty
- `especificaciones/fase-2-motor-scripts.md` — motor Scriban y helpers C#
