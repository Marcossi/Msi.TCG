# Especificación técnica: Fase 3 - Integración con UI

> Integración del motor de scripts con la interfaz de usuario existente.
> Referencia: ADR-005, Fase 3

## Objetivo

Flujo completo funcional desde la UI: ProjectExplorer muestra scripts y datos, TemplateEditor ejecuta scripts, Generate/GenerateAll funcionan.

## Alcance

### Incluye
- `ProjectExplorer` muestra scripts (.scriban) y datos (.json)
- `TemplateEditor` usa nuevo motor para preview (primer output)
- Generate: ejecuta script con write_to_file a disco
- GenerateAll: ejecuta todos los scripts

### No incluye
- Combo de múltiples outputs
- Re-carga al modificar fichero
- Marcado de errores en UI
- FileWatcher

### Dependencias
- **Fase 2 completada**: `IScriptEngine` y helpers deben estar disponibles

## Cambios en ProjectExplorer

### ProjectExplorerShellViewModel

```csharp
namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

internal sealed partial class ProjectExplorerShellViewModel : BaseViewModel, ICommandRoute
{
    private readonly IProjectContext _projectContext;
    private readonly IProjectService _projectService;
    private readonly IElementCatalog _elementCatalog;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ProjectExplorerShellViewModel> _logger;
    
    [ObservableProperty]
    private ObservableCollection<FileEntryViewModel> _files = new();
    
    [ObservableProperty]
    private ObservableCollection<FileEntryViewModel> _scripts = new();
    
    public ProjectExplorerShellViewModel(
        IProjectContext projectContext,
        IProjectService projectService,
        IElementCatalog elementCatalog,
        IMessenger messenger,
        INavigationService navigationService,
        ILogger<ProjectExplorerShellViewModel> logger)
    {
        _projectContext = projectContext;
        _projectService = projectService;
        _elementCatalog = elementCatalog;
        _messenger = messenger;
        _navigationService = navigationService;
        _logger = logger;
        
        _messenger.Register<ProjectOpenedMessage>(this, async (recipient, message) =>
        {
            await LoadProjectFilesAsync();
        });
    }
    
    private async Task LoadProjectFilesAsync()
    {
        Files.Clear();
        Scripts.Clear();
        
        string projectPath = _projectContext.CurrentProject?.FolderPath ?? string.Empty;
        
        if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            return;
        
        // Cargar ficheros .json (datos)
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(projectPath, "*.json", SearchOption.AllDirectories);
        foreach (string jsonFile in jsonFiles)
        {
            string relativePath = Path.GetRelativePath(projectPath, jsonFile);
            Files.Add(new FileEntryViewModel(jsonFile, relativePath, FileType.Data));
        }
        
        // Cargar ficheros .scriban (scripts)
        IEnumerable<string> scribanFiles = Directory.EnumerateFiles(projectPath, "*.scriban", SearchOption.AllDirectories);
        foreach (string scribanFile in scribanFiles)
        {
            string relativePath = Path.GetRelativePath(projectPath, scribanFile);
            Scripts.Add(new FileEntryViewModel(scribanFile, relativePath, FileType.Script));
        }
        
        _logger.LogInformation("Loaded {FileCount} data files and {ScriptCount} scripts", 
            Files.Count, Scripts.Count);
        
        await Task.CompletedTask;
    }
}
```

### FileType enum

```csharp
namespace Msi.TemplateCodeGenerator.Models;

public enum FileType
{
    Unknown,
    Data,      // .json
    Script     // .scriban
}
```

### FileEntryViewModel (actualizado)

```csharp
namespace Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

internal sealed partial class FileEntryViewModel : BaseViewModel
{
    public string FullPath { get; }
    public string RelativePath { get; }
    public FileType FileType { get; }
    
    public string FileName => Path.GetFileName(FullPath);
    public string Icon => FileType switch
    {
        FileType.Data => "📄",
        FileType.Script => "📝",
        _ => "📄"
    };
    
    public FileEntryViewModel(string fullPath, string relativePath, FileType fileType)
    {
        FullPath = fullPath;
        RelativePath = relativePath;
        FileType = fileType;
    }
}
```

### ProjectExplorerShellView.axaml (actualizado)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels">
    
    <TreeView ItemsSource="{Binding Scripts}" x:Name="ScriptsTree">
        <TreeView.ItemTemplate>
            <TreeDataTemplate ItemsSource="{Binding}">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="{Binding Icon}" />
                    <TextBlock Text="{Binding FileName}" />
                </StackPanel>
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
    
    <!-- Separador -->
    <Grid.RowDefinitions>
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    
    <!-- Scripts -->
    <TextBlock Grid.Row="0" Text="Scripts" FontWeight="Bold" Margin="8" />
    <TreeView Grid.Row="0" ItemsSource="{Binding Scripts}" Margin="0,30,0,0">
        <TreeView.ItemTemplate>
            <TreeDataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="{Binding Icon}" />
                    <TextBlock Text="{Binding FileName}" />
                </StackPanel>
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
    
    <!-- Datos -->
    <TextBlock Grid.Row="2" Text="Data Files" FontWeight="Bold" Margin="8" />
    <TreeView Grid.Row="2" ItemsSource="{Binding Files}" Margin="0,30,0,0">
        <TreeView.ItemTemplate>
            <TreeDataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="{Binding Icon}" />
                    <TextBlock Text="{Binding FileName}" />
                </StackPanel>
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
</UserControl>
```

## Cambios en TemplateEditor

### TemplateEditorShellViewModel

```csharp
namespace Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

internal sealed partial class TemplateEditorShellViewModel : BaseTextEditorViewModel
{
    private readonly IScriptEngine _scriptEngine;
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly ICommandRoute _commandRoute;
    private readonly ILogger<TemplateEditorShellViewModel> _logger;
    
    [ObservableProperty]
    private string _previewContent = string.Empty;
    
    [ObservableProperty]
    private string _previewError = string.Empty;
    
    public TemplateEditorShellViewModel(
        IScriptEngine scriptEngine,
        IFileService fileService,
        IDialogService dialogService,
        ICommandRoute commandRoute,
        ILogger<TemplateEditorShellViewModel> logger)
    {
        _scriptEngine = scriptEngine;
        _fileService = fileService;
        _dialogService = dialogService;
        _commandRoute = commandRoute;
        _logger = logger;
    }
    
    protected override async Task OnContentChangedAsync()
    {
        await UpdatePreviewAsync();
    }
    
    private async Task UpdatePreviewAsync()
    {
        try
        {
            // Ejecutar script en modo preview (sin escribir a disco)
            // Por ahora, solo mostrar el primer output
            ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(Content, FilePath);
            
            if (result.Success)
            {
                if (result.Outputs.Count > 0)
                {
                    PreviewContent = result.Outputs[0].Content;
                    PreviewError = string.Empty;
                }
                else
                {
                    PreviewContent = string.Empty;
                    PreviewError = "Script generated no outputs";
                }
            }
            else
            {
                PreviewContent = string.Empty;
                PreviewError = string.Join("\n", result.Errors);
            }
        }
        catch (Exception ex)
        {
            PreviewContent = string.Empty;
            PreviewError = ex.Message;
            _logger.LogError(ex, "Error updating preview for {Path}", FilePath);
        }
    }
    
    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(Content, FilePath);
            
            if (result.Success)
            {
                _logger.LogInformation("Script {Path} generated {Count} files", 
                    FilePath, result.Outputs.Count);
                
                await _dialogService.ShowInfoAsync(
                    $"Generated {result.Outputs.Count} file(s)", 
                    "Generate Complete");
            }
            else
            {
                string errors = string.Join("\n", result.Errors);
                await _dialogService.ShowErrorAsync(
                    $"Script execution failed:\n{errors}", 
                    "Generate Error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating from script {Path}", FilePath);
            await _dialogService.ShowErrorAsync(ex.Message, "Generate Error");
        }
    }
}
```

### TemplateEditorShellView.axaml (actualizado)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Grid ColumnDefinitions="*,5,*">
        <!-- Editor -->
        <TextBox Grid.Column="0" 
                 Text="{Binding Content}" 
                 AcceptsReturn="True"
                 FontFamily="Consolas" />
        
        <!-- Splitter -->
        <GridSplitter Grid.Column="1" Width="5" />
        
        <!-- Preview -->
        <Grid Grid.Column="2" RowDefinitions="*,Auto,*">
            <TextBox Grid.Row="0" 
                     Text="{Binding PreviewContent}" 
                     IsReadOnly="True"
                     FontFamily="Consolas"
                     AcceptsReturn="True" />
            
            <!-- Error panel -->
            <Border Grid.Row="1" 
                    Background="#FFE0E0" 
                    IsVisible="{Binding PreviewError, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
                <TextBlock Text="{Binding PreviewError}" 
                           Foreground="Red" 
                           TextWrapping="Wrap"
                           Margin="8" />
            </Border>
            
            <!-- Toolbar -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="8" Margin="8">
                <Button Content="Generate" Command="{Binding GenerateCommand}" />
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

## GenerateAll en MainShell

### MainShellViewModel

```csharp
namespace Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;

internal sealed partial class MainShellViewModel : BaseViewModel
{
    private readonly IProjectService _projectService;
    private readonly INavigationService _navigationService;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IFileService _fileService;
    private readonly IScriptEngine _scriptEngine;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainShellViewModel> _logger;
    
    [RelayCommand]
    private async Task GenerateAllAsync()
    {
        try
        {
            string projectPath = _projectService.CurrentProject?.FolderPath ?? string.Empty;
            
            if (string.IsNullOrEmpty(projectPath))
            {
                await _dialogService.ShowWarningAsync("No project is currently open", "Generate All");
                return;
            }
            
            // Obtener todos los scripts
            IEnumerable<string> scriptPaths = Directory.EnumerateFiles(
                projectPath, "*.scriban", SearchOption.AllDirectories);
            
            int successCount = 0;
            int errorCount = 0;
            List<string> errors = new();
            
            foreach (string scriptPath in scriptPaths)
            {
                string scriptContent = await _fileService.ReadTextAsync(scriptPath);
                ScriptExecutionResult result = await _scriptEngine.ExecuteAsync(scriptContent, scriptPath);
                
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                    errors.Add($"{Path.GetFileName(scriptPath)}: {string.Join(", ", result.Errors)}");
                }
            }
            
            string message = $"Generated {successCount} script(s)";
            if (errorCount > 0)
            {
                message += $"\n\n{errorCount} script(s) failed:\n{string.Join("\n", errors)}";
            }
            
            await _dialogService.ShowInfoAsync(message, "Generate All Complete");
            
            _logger.LogInformation("GenerateAll completed: {Success} success, {Error} errors", 
                successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateAll");
            await _dialogService.ShowErrorAsync(ex.Message, "Generate All Error");
        }
    }
}
```

### MainShellView.axaml (menú actualizado)

```xml
<MenuItem Header="_Generate">
    <MenuItem Header="Generate _Current" Command="{Binding GenerateCommand}" />
    <MenuItem Header="Generate _All" Command="{Binding GenerateAllCommand}" />
</MenuItem>
```

## Registro en DI

No se requieren cambios en DI. Los servicios ya están registrados en Fases anteriores.

## Testing

### Integration tests

```csharp
public class ProjectExplorerIntegrationTests
{
    [Fact]
    public async Task LoadProjectFiles_WithScriptsAndData_LoadsCorrectly()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        await File.WriteAllTextAsync(Path.Combine(tempDir, "script.scriban"), "{{ test }}");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "data.json"), "{}");
        
        // Act
        var viewModel = CreateViewModel(tempDir);
        await viewModel.LoadProjectFilesAsync();
        
        // Assert
        viewModel.Scripts.ShouldHaveSingleItem();
        viewModel.Files.ShouldHaveSingleItem();
        
        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }
}

public class TemplateEditorIntegrationTests
{
    [Fact]
    public async Task UpdatePreview_WithValidScript_ShowsFirstOutput()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Content = "{{ write_to_file(\"test.cs\", \"public class Test {}\") }}";
        
        // Act
        await viewModel.UpdatePreviewAsync();
        
        // Assert
        viewModel.PreviewContent.ShouldBe("public class Test {}");
        viewModel.PreviewError.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task UpdatePreview_WithSyntaxError_ShowsError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Content = "{{ invalid syntax }}";
        
        // Act
        await viewModel.UpdatePreviewAsync();
        
        // Assert
        viewModel.PreviewContent.ShouldBeEmpty();
        viewModel.PreviewError.ShouldNotBeEmpty();
    }
}
```

## Criterios de aceptación

- [ ] `dotnet build` sin errores
- [ ] Abrir proyecto → ProjectExplorer muestra scripts y datos
- [ ] Editar script → preview muestra output generado
- [ ] Generate → ficheros aparecen en disco
- [ ] GenerateAll → todos los scripts se ejecutan
- [ ] Script con error → preview muestra mensaje de error (no crash)
- [ ] Menú "Generate All" ejecuta todos los scripts del proyecto
- [ ] Logs muestran operaciones de Generate y GenerateAll

## Flujo de usuario

1. **Abrir proyecto**: ProjectExplorer carga scripts (.scriban) y datos (.json)
2. **Editar script**: Usuario edita un .scriban en TemplateEditor
3. **Preview automático**: Cada cambio actualiza el preview (primer output)
4. **Generate**: Usuario hace clic en "Generate" → script se ejecuta y escribe a disco
5. **GenerateAll**: Usuario hace clic en "Generate All" → todos los scripts se ejecutan

## Pendiente de definir

- Combo de múltiples outputs en preview
- Re-carga automática al modificar fichero en disco
- Marcado de errores en ProjectExplorer (aspa roja)
- FileWatcher para detección de cambios
- Timeout de ejecución de scripts
