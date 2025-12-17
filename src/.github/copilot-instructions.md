# Instrucciones para GitHub Copilot

## Propósito del Proyecto

**Msi.TemplateCodeGenerator** es una herramienta de generación de código diseñada para automatizar la creación de código altamente repetitivo en proyectos de software mediante un **editor de plantillas Scriban con preview en tiempo real**.

### Objetivo Principal
Cuando en un proyecto es necesario generar múltiples archivos de manera repetitiva en distintas secciones del código (clases CRUD, DTOs, ViewModels, servicios, etc.), en lugar de hacerlo manualmente, esta aplicación permite:

1. **Definir plantillas reutilizables** utilizando el motor de plantillas **Scriban**
2. **Cargar modelos de datos personalizados** desde DLLs de C# que alimentan las plantillas
3. **Editar plantillas con preview en tiempo real** viendo la salida generada instantáneamente
4. **Validar plantillas** mostrando errores de sintaxis y ejecución en tiempo real
5. **Generar múltiples archivos** automáticamente aplicando las plantillas con diferentes datos de entrada
6. **Distribuir el código generado** en las ubicaciones correctas del proyecto destino

### Casos de Uso Típicos
- Generar entidades, repositorios y servicios para múltiples tablas de base de datos
- Crear ViewModels y Vistas para múltiples pantallas CRUD
- Generar DTOs, mappers y validadores para APIs REST
- Crear tests unitarios repetitivos para múltiples componentes
- Cualquier escenario donde se necesite replicar una estructura de código con ligeras variaciones

## Arquitectura del Sistema

### Editor de Plantillas
El editor consta de **dos paneles principales**:

1. **Panel de Edición**: Área de texto donde el usuario escribe/edita las plantillas Scriban
2. **Panel de Preview**: Muestra en tiempo real:
   - La salida generada al aplicar la plantilla con el modelo actual
   - Errores de sintaxis de Scriban
   - Errores de ejecución (propiedades inexistentes, errores de tipo, etc.)
   - Warnings y sugerencias

### Sistema de Proyectos de Plantillas
Un **proyecto de plantillas** consiste en:

1. **Conjunto de Plantillas**: Múltiples archivos `.scriban` que definen las transformaciones de código
2. **Modelo de Datos (DLL)**: Una o más DLLs de C# que contienen:
   - **Clases de Modelo**: POCOs con las propiedades que se usarán en las plantillas
   - **Funciones Personalizadas**: Métodos estáticos o de instancia que pueden invocarse desde las plantillas
   - **Configuración**: Información adicional necesaria para la generación de código

### Flujo de Trabajo
1. El usuario crea un proyecto de plantillas
2. Desarrolla una DLL de C# con su modelo de datos y funciones auxiliares
3. Carga la DLL en el proyecto de plantillas
4. El sistema refleja las clases y funciones disponibles para usar en las plantillas
5. El usuario crea/edita plantillas Scriban que referencian el modelo
6. El editor muestra en tiempo real la salida generada en el panel de preview
7. Una vez validadas, las plantillas pueden ejecutarse para generar archivos múltiples

## Motor de Plantillas (Scriban)

### Características
-   **Motor de Plantillas**: Se utiliza Scriban como motor de plantillas por su potencia, flexibilidad y seguridad.
-   **Sintaxis**: Las plantillas usan la sintaxis de Scriban (similar a Liquid) con marcadores `{{` y `}}` para expresiones y `{%` y `%}` para lógica de control.
-   **Funciones Personalizadas**: El usuario puede registrar funciones de C# que estarán disponibles desde las plantillas Scriban.
-   **Modelo de Objetos**: Las plantillas tienen acceso a las clases y propiedades definidas en las DLLs cargadas.

### Ejemplo de Flujo
```csharp
// En la DLL del usuario (MyModels.dll):
public class EntityModel
{
    public string Name { get; set; }
    public List<PropertyModel> Properties { get; set; }
}

public static class TemplateHelpers
{
    public static string ToPascalCase(string input) { ... }
    public static string ToCamelCase(string input) { ... }
}
```

```scriban
{{# Plantilla Scriban que usa el modelo #}}
public class {{ entity.name }}
{
    {{~ for prop in entity.properties ~}}
    public {{ prop.type }} {{ prop.name | to_pascal_case }} { get; set; }
    {{~ end ~}}
}
```

## Configuración

-   **Fichero de Configuración**: La aplicación utilizará un fichero `appsettings.json` para su configuración general.
-   **Proyectos de Plantillas**: Cada proyecto de plantillas tendrá su propio archivo de configuración (ej. `project.json`) con:
    - Rutas a las DLLs de modelo
    - Lista de plantillas del proyecto
    - Configuración de salida (rutas de generación, nombres de archivos, etc.)
-   **Almacenamiento de Plantillas**: Las plantillas Scriban se almacenan como archivos `.scriban` en el sistema de archivos.

## Flujos de Trabajo de Desarrollo

-   **Construcción**: Utiliza el comando estándar de .NET: `dotnet build`.
-   **Ejecución**: Para ejecutar la aplicación, usa `dotnet run --project Msi.TemplateCodeGenerator/Msi.TemplateCodeGenerator.csproj`.
-   **Depuración**: La depuración se puede realizar directamente desde Visual Studio Code o Visual Studio.

## Convenciones y Patrones

-   **Patrón de Diseño**: Se prefiere el patrón Modelo-Vista-Modelo de Vista (MVVM) para la capa de GUI (WPF), para separar la UI de la lógica de presentación.
-   **Implementación MVVM**: Se utiliza **CommunityToolkit.Mvvm** (anteriormente conocido como Microsoft MVVM Toolkit) como biblioteca oficial para implementar el patrón MVVM. Esta librería proporciona:
    - **Source Generators**: Genera automáticamente código boilerplate mediante atributos
    - **[ObservableProperty]**: Genera propiedades observables automáticamente desde campos privados
    - **[RelayCommand]**: Genera comandos ICommand automáticamente desde métodos
    - **ObservableObject**: Clase base que implementa INotifyPropertyChanged
    - **ObservableValidator**: Para validación con anotaciones de datos
    - **Messenger**: Sistema de mensajería débilmente acoplado para comunicación entre ViewModels
    - **Performance optimizada**: Mínimo overhead y código generado eficiente
-   **Inyección de Dependencias (IoC)**: Se utilizará un contenedor de inyección de dependencias (`Microsoft.Extensions.DependencyInjection`) para gestionar el ciclo de vida de los servicios y las dependencias.
    -   **Constructores Primarios**: Se utilizarán **Primary Constructors** (C# 12+) para la inyección de dependencias en clases (especialmente ViewModels y Servicios) para reducir el código boilerplate.
    -   Los servicios se registrarán en el arranque de la aplicación y se resolverán mediante inyección en el constructor.
-   **Nomenclatura e Idioma**: El código (nombres de variables, clases, métodos, etc.) debe estar en **inglés**, siguiendo las convenciones estándar de C# y .NET. Sin embargo, los **comentarios** deben escribirse en **español** para facilitar la comprensión y el mantenimiento del equipo.
-   **Usings Implícitos**: El proyecto utiliza `<ImplicitUsings>enable</ImplicitUsings>`, por lo que no es necesario añadir `using` comunes en cada fichero.
-   **Nulabilidad**: El código debe ser seguro para nulos (`<Nullable>enable</Nullable>`). Se deben usar tipos nullable (`?`) cuando sea apropiado.
-   **Formato de Código**: La indentación se realiza con **4 espacios**, no con tabuladores, según se define en el fichero `.editorconfig` del proyecto.

### Ejemplos de Uso de CommunityToolkit.Mvvm

```csharp
// ViewModel con propiedades observables y comandos
public partial class TemplateEditorViewModel : ObservableObject
{
    // Campo privado - el source generator creará la propiedad pública TemplateContent
    [ObservableProperty]
    private string _templateContent = string.Empty;
    
    // Campo privado con notificación a otras propiedades
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private bool _isValid;
    
    // Propiedad calculada
    public bool HasErrors => !IsValid;
    
    // Comando simple - el source generator creará SaveCommand
    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveTemplateAsync();
    }
    
    // Comando con CanExecute - genera SaveCommand y notifica cuando puede ejecutarse
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await SaveTemplateAsync();
    }
    
    private bool CanSave() => !string.IsNullOrEmpty(TemplateContent);
}
```

### Nota Importante sobre Source Generators
Al utilizar `CommunityToolkit.Mvvm`, muchas propiedades y comandos **no existen explícitamente en el código fuente** del ViewModel, sino que son generados en tiempo de compilación.
- Si ves un campo `[ObservableProperty] private string _name;`, asume que existe una propiedad pública `Name`.
- Si ves un método `[RelayCommand] private void Save()`, asume que existe un comando público `SaveCommand`.
- **No intentes añadir manualmente** estas propiedades o comandos si ya están definidos los campos/métodos con sus atributos correspondientes.

## Estructura y Nomenclatura de la UI (WPF)

-   **Carpetas por Funcionalidad (Feature Folders)**: La UI se organiza en carpetas dentro del proyecto `Msi.TemplateCodeGenerator/UI/`, donde cada carpeta representa una funcionalidad principal de la aplicación (ej. `TemplateEditor`, `Settings`).
-   **Vista Principal (Shell)**: El `UserControl` principal que actúa como contenedor para una funcionalidad se nombra con el sufijo `ShellView`. Este es el punto de entrada que se cargará en el área de navegación principal. Ejemplo: `TemplateEditorShellView.xaml`.
-   **ViewModel Principal**: El ViewModel correspondiente a la vista principal sigue la convención con el sufijo `ViewModel`. Ejemplo: `TemplateEditorViewModel.cs`.
-   **Controles Secundarios**: Otros `UserControl` o componentes reutilizables dentro de una carpeta de funcionalidad usan el sufijo `View` (ej. `TemplateListView.xaml`, `PreviewPanelView.xaml`).
-   **XAML y Tiempo de Diseño**:
    -   **DataContext en Diseño**: Para habilitar IntelliSense en XAML sin causar errores por la inyección de dependencias en los constructores, se debe usar `d:DataContext` con `d:DesignInstance`.
    -   **IsDesignTimeCreatable=False**: Se debe establecer `IsDesignTimeCreatable=False` para evitar que el diseñador intente instanciar el ViewModel (lo cual fallaría por falta de servicios inyectados).
    -   **Ejemplo**: `d:DataContext="{d:DesignInstance Type=local:MyViewModel, IsDesignTimeCreatable=False}"`

## Dependencias Clave

-   **WPF**: El proyecto se basa en el SDK de .NET con la propiedad `<UseWPF>true</UseWPF>`.
-   **Scriban**: Motor de plantillas para la generación de código. Soporta funciones personalizadas y modelos de objetos complejos.
-   **CommunityToolkit.Mvvm**: Biblioteca oficial de Microsoft para implementar el patrón MVVM con source generators. Proporciona atributos como `[ObservableProperty]` y `[RelayCommand]` que generan automáticamente el código boilerplate necesario.
-   **Microsoft.Extensions.DependencyInjection**: Contenedor de IoC para inyección de dependencias.
-   **Microsoft.Extensions.Hosting**: Para configurar el host de la aplicación con servicios y logging.
-   **NLog**: Biblioteca de logging para registrar la actividad de la aplicación, errores y otros eventos importantes en ficheros de texto. Se integrará a través de las abstracciones de `Microsoft.Extensions.Logging`.
-   **System.Reflection**: Para cargar dinámicamente las DLLs de usuario y explorar sus tipos, métodos y propiedades.

## Arquitectura de Componentes

### Capas Principales
1. **UI (WPF)**: Interfaz gráfica de usuario con patrón MVVM
   - Editor de texto con syntax highlighting para Scriban
   - Panel de preview en tiempo real
   - Gestión de proyectos de plantillas
   
2. **Core/Services**: Lógica de negocio, procesamiento de plantillas y generación de código
   - Motor de renderizado Scriban
   - Carga y reflexión de DLLs
   - Validación de plantillas
   - Generación de archivos
   
3. **Infrastructure**: Acceso a archivos, configuración y persistencia
   - Sistema de archivos para plantillas
   - Serialización de configuración de proyectos
   - Logging

### Servicios Principales (a implementar)

- **ITemplateService**: Gestión y renderizado de plantillas Scriban
  - Compilar plantillas Scriban
  - Renderizar con modelo de datos
  - Capturar y reportar errores de sintaxis y ejecución
  
- **IAssemblyLoaderService**: Carga dinámica de DLLs de usuario
  - Cargar DLLs mediante reflexión
  - Explorar tipos y métodos disponibles
  - Registrar funciones personalizadas en el contexto de Scriban
  - Gestionar dominios de aplicación (AppDomain) para aislar DLLs
  
- **IModelBindingService**: Vinculación entre modelos de C# y contexto de Scriban
  - Registrar clases de modelo en el contexto de plantillas
  - Mapear funciones personalizadas a funciones de Scriban
  - Validar que las propiedades referenciadas existan
  
- **IPreviewService**: Generación de preview en tiempo real
  - Renderizar plantillas incrementalmente mientras se edita
  - Debouncing para evitar renderizados excesivos
  - Reporte de errores y warnings en tiempo real
  
- **IFileGeneratorService**: Generación y escritura de archivos en el sistema
  - Aplicar plantillas a múltiples modelos
  - Escribir archivos generados en las ubicaciones correctas
  - Gestionar conflictos de archivos existentes
  
- **IProjectService**: Gestión de proyectos de plantillas
  - Crear, abrir, guardar y cerrar proyectos
  - Gestionar lista de plantillas del proyecto
  - Gestionar referencias a DLLs de modelo
  
- **IValidationService**: Validación de plantillas y datos de entrada
  - Validar sintaxis de Scriban
  - Validar que el modelo tiene las propiedades referenciadas
  - Validar configuración de salida

## Buenas Prácticas

-   **Separación de Responsabilidades**: Mantener la UI (Vistas) separada de la lógica de negocio (ViewModels y Services).
-   **Testabilidad**: Los servicios deben diseñarse para ser fácilmente testeables mediante inyección de dependencias.
-   **Manejo de Errores**: Todos los errores deben ser capturados, registrados con NLog y presentados al usuario de manera amigable.
-   **Validación**: Validar siempre los datos de entrada antes de procesar plantillas o generar archivos.
-   **Async/Await**: Operaciones de I/O (lectura/escritura de archivos, procesamiento de plantillas, carga de DLLs) deben ser asíncronas.
-   **Seguridad con DLLs**: Al cargar DLLs externas, considerar:
     - Validación de firmas digitales (opcional)
     - Aislamiento mediante AppDomain o AssemblyLoadContext
     - Manejo seguro de excepciones en código de usuario
-   **Performance del Preview**: El preview en tiempo real debe ser eficiente:
     - Implementar debouncing para evitar renderizados continuos
     - Cachear resultados de compilación de plantillas cuando sea posible
     - Renderizar de forma asíncrona para no bloquear la UI
-   **Uso de Source Generators**: Aprovechar los source generators de CommunityToolkit.Mvvm para reducir código boilerplate:
     - Usar `[ObservableProperty]` en lugar de implementar manualmente propiedades con INotifyPropertyChanged
     - Usar `[RelayCommand]` en lugar de crear manualmente implementaciones de ICommand
     - Marcar las clases como `partial` para que los source generators puedan extenderlas

Al agregar nuevas características, asegúrate de mantener la coherencia con estas convenciones y la arquitectura establecida.
