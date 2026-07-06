# Alcance y dominio

> Definición del alcance funcional del producto, límites del dominio y criterios de exclusión.

## Propósito

**Msi.TemplateCodeGenerator** es una aplicación de escritorio para Windows que permite generar código fuente de forma automatizada mediante plantillas Scriban. La aplicación gestiona proyectos que contienen plantillas y modelos de datos, renderiza el resultado en tiempo real y exporta el código generado.

## Alcance funcional

### Funcionalidades implementadas (Fase 1-3)

1. **Gestión de proyectos**
   - Crear, abrir, cerrar, guardar y guardar como proyectos
   - Serialización/deserialización en formato JSON (JSONC con soporte de comentarios en lectura)
   - Contexto de proyecto activo (IProjectContext) con estado centralizado

2. **Editor de plantillas Scriban**
   - Edición de archivos `.scriban` en pestañas independientes
   - Vista previa en tiempo real con debounce (1s de inactividad)
   - Confirmación de guardado al cerrar documentos con cambios pendientes
   - Soporte para múltiples pestañas con instancias independientes (Scoped)

3. **Explorador de proyectos**
   - Visualización jerárquica de archivos del proyecto
   - Iconos y colores por tipo de archivo
   - Actualización automática al abrir/cerrar proyectos

4. **Navegación con docking**
   - Layout con panel izquierdo (ProjectExplorer) y área de documentos (pestañas)
   - Paneles redimensionables con Dock.Avalonia
   - Navegación basada en IDs con WeakReferenceMessenger

5. **Configuración**
   - Pantalla de settings (esqueleto funcional)

6. **Infraestructura**
   - Logging con Serilog (Console + File)
   - Sistema de mensajería desacoplada (WeakReferenceMessenger)
   - Inyección de dependencias con Microsoft.Extensions.DependencyInjection
   - Tests unitarios (xUnit + NSubstitute)

### Funcionalidades planificadas

1. **Secciones recursivas**
   - Modelo `Section` con datos específicos y plantillas
   - Subsecciones arbitrariamente profundas
   - Propagación de datos de padres a hijos

2. **FileWatcher**
   - Vigilancia de cambios en la carpeta del proyecto
   - Actualización automática del explorador de archivos

3. **Gestión de modelos de datos**
   - Definición de clases C# como fuente de plantillas
   - Referenciado de ensamblados

4. **Migración de formato JSON**
   - Soporte para versiones intermedias del formato
   - Evaluación de migración a JSON5 para preservar comentarios

## Límites del dominio

### Qué NO hace la aplicación

1. **No es un IDE completo** — No ofrece autocompletado, syntax highlighting avanzado, debug de código generado o integración con compiladores.

2. **No genera código directamente** — La aplicación renderiza plantillas Scriban pero no exporta archivos al sistema de ficheros automáticamente. El usuario debe copiar/pegar o implementar exportación futura.

3. **No soporta proyectos múltiples simultáneos** — Solo un proyecto abierto a la vez. Abrir uno nuevo cierra el anterior.

4. **No tiene sistema de plugins** — La extensión de funcionalidad se hace mediante modificación del código fuente, no mediante plugins cargados dinámicamente.

5. **No es multiplataforma** — Target exclusivo: Windows (`net10.0-windows`). No se planea port a macOS/Linux.

6. **No usa base de datos** — Toda la persistencia es en ficheros JSON en disco.

### Criterios de exclusión

- Autenticación de usuarios
- Sync en la nube o control de versiones
- Soporte para otros motores de plantillas (Handlebars, Razor, etc.)
- Generación de proyectos completos (solo ficheros individuales)
- Interfaz web (aplicación puramente de escritorio)
