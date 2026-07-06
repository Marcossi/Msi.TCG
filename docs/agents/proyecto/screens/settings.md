# SettingsView

> Descripción detallada de SettingsView. Pantalla de configuración de la aplicación. Se muestra como un documento (pestaña) en el DocumentDock.

## Ubicación

- **Carpeta**: `UI/Views/Settings/`
- **Ficheros**:
  - `SettingsShellView.axaml` → Vista XAML
  - `SettingsShellView.axaml.cs` → Code-behind
  - `SettingsShellViewModel.cs` → ViewModel

## Pantalla

- **Tipo Dock**: Document
- **ID**: `SettingsId` (definido en `NavigationConstants.SettingsId`)

## Funcionalidad

Por definir. Esta pantalla está planificada para futuras configuraciones de la aplicación.

Actualmente muestra un placeholder: "Hello World! From: Settings Shell View Model"

## Registro en DI

- `SettingsShellViewModel` → `SettingsShellViewModel` → Singleton
