# Modelo de proyecto

> Estructura conceptual del proyecto, secciones recursivas y su relación con el sistema de plantillas.

## Propósito

Este documento define la estructura conceptual del proyecto: un contenedor de secciones recursivas, donde cada sección tiene sus propios datos y plantillas.

## Estructura conceptual del proyecto

```
Proyecto
├── Datos globales (aplicables a todas las secciones)
└── Secciones []
    ├── Sección 1
    │   ├── Datos específicos
    │   ├── Plantillas []
    │   │   ├── Plantilla A
    │   │   └── Plantilla B
    │   └── Subsecciones []
    │       └── Subsección 1.1
    │           ├── Datos específicos
    │           ├── Plantillas []
    │           └── Subsecciones []
    └── Sección 2
        ├── Datos específicos
        └── Plantillas []
            └── Plantilla C
```

## Decisiones de diseño

### Solo un proyecto abierto a la vez

La aplicación carga **un solo proyecto** a la vez. Al abrir un proyecto nuevo, el anterior se cierra y se liberan todos sus recursos.

Analogía con Visual Studio: Abrir una solución dentro de una carpeta y todo su contenido, y luego cerrar la solución para abrir otra en otra parte del disco.

Implementación: `ProjectService.OpenProjectAsync()` actualiza `IProjectContext`; `ProjectService.CloseProjectAsync()` limpia `IProjectContext`.

### Secciones recursivas (planificado)

Las secciones pueden contener subsecciones, creando un árbol arbitrariamente profundo.

Regla: No hay límite de profundidad en la recursión de secciones.

### Datos por nivel (planificado)

Cada nivel del árbol (proyecto, sección, subsección) tiene sus propios datos:

- **Proyecto** → `GlobalData`: Aplicables a todas las secciones
- **Sección** → `Section.Data`: Aplicables a las plantillas de esta sección y sus hijos
- **Subsección** → `Subsection.Data`: Aplicables solo a las plantillas de esta subsección

### Propagación de datos (planificado)

Las plantillas acceden a los datos de **su sección y todos los padres**:

```
Plantilla en Subsección 1.1 accede a:
├── Project.GlobalData
├── Sección1.Data
└── Subsección1.1.Data
```

## Modelo de datos actual

### Project (Models/Project.cs)

```csharp
public class Project
{
    public string Name { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public List<FileEntry> Files { get; set; } = [];
    
    // TODO: Futuras propiedades del dominio
    // - ReferencedAssemblies (ensamblados referenciados)
    // - Configuration (configuración del proyecto)
}
```

`Project` es una entidad de dominio pura (POCO). No tiene lógica de operaciones.

### Secciones y plantillas (planificado, no implementado)

```csharp
public class Section
{
    public string Name { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<Template> Templates { get; set; } = new();
    public List<Section> Sections { get; set; } = new();
}

public class Template
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
```

## Relación con el sistema de plantillas

### Flujo de renderizado

1. Usuario selecciona una plantilla en una sección
2. `TemplateEditorShellViewModel` se abre en una pestaña
3. El editor accede a los datos de su sección y padres a través de `IProjectContext`
4. `ITemplatesService.ProcessTemplateAsync()` renderiza la plantilla con los datos

### Acceso a datos desde plantillas

Las plantillas Scriban acceden a los datos a través del modelo:

```scriban
// Plantilla en Subsección 1.1
// Accede a: Project.GlobalData + Sección1.Data + Subsección1.1.Data

// {{ model.ProjectName }} → de GlobalData
// {{ model.SectionSetting }} → de Section.Data
// {{ model.LocalSetting }} → de Subsection.Data
```

## Representación en la UI

### ProjectExplorer

El `ProjectExplorer` muestra el árbol de secciones y archivos:

```
📁 Proyecto
├── 📄 proyecto.scribanproj
├── 📁 Sección 1
│   ├── 📄 Plantilla A.scriban
│   ├── 📄 Plantilla B.scriban
│   └── 📁 Subsección 1.1
│       ├── 📄 Plantilla C.scriban
│       └── 📄 datos.json
└── 📁 Sección 2
    └── 📄 Plantilla D.scriban
```

### TemplateEditor

El `TemplateEditor` abre una pestaña por cada plantilla:

```
[Plantilla A] [Plantilla B] [Plantilla C] ← pestañas
```

Cada pestaña tiene:
- Contenido editado
- Undo/Redo stack
- Estado de dirty (cambios sin guardar)

## Estado actual

Implementado:
- Gestión de proyectos (abrir, cerrar, guardar)
- Explorador de archivos del proyecto
- Editor de plantillas Scriban con vista previa
- Navegación con paneles dockeables
- `Project` como entidad de dominio
- `ProjectService` con operaciones CRUD

Pendiente:
- **Secciones recursivas**: Modelo `Section` con datos y plantillas
- **GlobalData**: Datos globales del proyecto
- **Propagación de datos**: Las plantillas acceden a datos de padres y hijos
- **FileWatcher**: Vigilancia de cambios en la carpeta del proyecto
- **Serialización de secciones**: Guardar/cargar secciones y plantillas
