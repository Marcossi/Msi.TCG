using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;

namespace Msi.TemplateCodeGenerator.UI.ProjectExplorer;

/// <summary>
/// ViewModel del explorador de proyectos que refleja el estado del contexto actual.
/// Se suscribe a mensajes de cambios de proyecto para actualizar automáticamente la UI.
/// </summary>
internal partial class ProjectExplorerShellViewModel : BaseViewModel
{
    private readonly IProjectContext _projectContext;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private string _projectName = "sin solución";

    public ProjectExplorerShellViewModel(IProjectContext projectContext, IMessenger messenger)
    {
        _projectContext = projectContext;
        _messenger = messenger;

        // Suscribirse a eventos de proyecto
        _messenger.Register<ProjectOpenedMessage>(this, (recipient, message) => ((ProjectExplorerShellViewModel)recipient).RefreshProjectContext());
        _messenger.Register<ProjectClosedMessage>(this, (recipient, message) => ((ProjectExplorerShellViewModel)recipient).RefreshProjectContext());
        _messenger.Register<ProjectSavedMessage>(this, (recipient, message) => ((ProjectExplorerShellViewModel)recipient).RefreshProjectContext());

        // Inicializar estado
        RefreshProjectContext();
    }

    [RelayCommand]
    private void RefreshProjectContext()
    {
        IsProjectOpen = _projectContext.IsProjectOpen;

        if (!IsProjectOpen)
        {
            ProjectName = "sin solución";
            return;
        }

        // Usar el nombre del proyecto directamente desde el modelo de dominio
        ProjectName = _projectContext.CurrentProject?.Name ?? "sin solución";
    }
}
