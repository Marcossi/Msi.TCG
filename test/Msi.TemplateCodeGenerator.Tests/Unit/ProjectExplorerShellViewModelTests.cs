using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using NSubstitute;

namespace Msi.TemplateCodeGenerator.Tests.UI.Views.ProjectExplorer.ViewModels;

public class ProjectExplorerShellViewModelTests
{
    private sealed class StubTreeBuilder : IProjectTreeBuilder
    {
        public ObservableCollection<FileEntryViewModel> BuildFileTree(
            Msi.TemplateCodeGenerator.Models.Project project, string projectFilePath, IReadOnlySet<string> expandedPaths) =>
            new();
    }

    private static (ProjectExplorerShellViewModel ViewModel, IProjectService ProjectService, IDialogService DialogService, IProjectContext ProjectContext) CreateViewModelWithMocks()
    {
        var projectContext = Substitute.For<IProjectContext>();
        projectContext.IsProjectOpen.Returns(false);
        var projectService = Substitute.For<IProjectService>();
        var elementCatalog = Substitute.For<IElementCatalog>();
        var fileSystem = Substitute.For<IFileSystem>();
        var scriptEngine = Substitute.For<IScriptEngine>();
        var treeBuilder = new StubTreeBuilder();
        var scriptFinder = new Msi.TemplateCodeGenerator.Services.ProjectScriptFinder();
        var fileOperations = new Msi.TemplateCodeGenerator.Services.ProjectFileOperations();
        var stateService = Substitute.For<IProjectExplorerStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var inlineEditing = new Msi.TemplateCodeGenerator.Services.InlineEditingService(
            fileOperations,
            dialogService,
            NullLogger<Msi.TemplateCodeGenerator.Services.InlineEditingService>.Instance);
        var stateManager = new Msi.TemplateCodeGenerator.Services.ProjectExplorerStateManager(
            stateService,
            NullLogger<Msi.TemplateCodeGenerator.Services.ProjectExplorerStateManager>.Instance);
        var fileWatcher = Substitute.For<IFileWatcherService>();
        var messenger = new WeakReferenceMessenger();
        var navigationService = Substitute.For<INavigationService>();
        var logger = NullLogger<ProjectExplorerShellViewModel>.Instance;

        var viewModel = new ProjectExplorerShellViewModel(
            projectContext,
            projectService,
            elementCatalog,
            fileSystem,
            scriptEngine,
            treeBuilder,
            scriptFinder,
            fileOperations,
            inlineEditing,
            stateManager,
            fileWatcher,
            messenger,
            navigationService,
            dialogService,
            stateService,
            logger);

        return (viewModel, projectService, dialogService, projectContext);
    }

    #region CreateFileCommand Tests

    [Fact]
    public async Task CreateFileCommand_CallsProjectServiceCreateFileAsync()
    {
        // Arrange
        var (viewModel, projectService, _, projectContext) = CreateViewModelWithMocks();
        projectContext.IsProjectOpen.Returns(true);
        projectService.CreateFileAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new FileEntry { Name = "NuevoFichero.scriban", RelativePath = "folder/NuevoFichero.scriban", Type = FileType.Script });

        var parentFolder = new FileEntryViewModel("folder", "folder", FileType.Directory);
        viewModel.FileTree.Add(parentFolder);

        // Act
        await viewModel.CreateFileCommand.ExecuteAsync(parentFolder);

        // Assert
        await projectService.Received(1).CreateFileAsync("folder", "NuevoFichero.scriban");
    }

    [Fact]
    public async Task CreateFileCommand_WithNullParent_CreatesInRoot()
    {
        // Arrange
        var (viewModel, projectService, _, projectContext) = CreateViewModelWithMocks();
        projectContext.IsProjectOpen.Returns(true);
        projectService.CreateFileAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new FileEntry { Name = "NuevoFichero.scriban", RelativePath = "NuevoFichero.scriban", Type = FileType.Script });

        // Act
        await viewModel.CreateFileCommand.ExecuteAsync(null);

        // Assert
        await projectService.Received(1).CreateFileAsync("", "NuevoFichero.scriban");
    }

    #endregion

    #region CreateDirectoryCommand Tests

    [Fact]
    public async Task CreateDirectoryCommand_CallsProjectServiceCreateDirectoryAsync()
    {
        // Arrange
        var (viewModel, projectService, _, projectContext) = CreateViewModelWithMocks();
        projectContext.IsProjectOpen.Returns(true);
        projectService.CreateDirectoryAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new FileEntry { Name = "NuevaCarpeta", RelativePath = "folder/NuevaCarpeta", Type = FileType.Directory });

        var parentFolder = new FileEntryViewModel("folder", "folder", FileType.Directory);
        viewModel.FileTree.Add(parentFolder);

        // Act
        await viewModel.CreateDirectoryCommand.ExecuteAsync(parentFolder);

        // Assert
        await projectService.Received(1).CreateDirectoryAsync("folder", "NuevaCarpeta");
    }

    [Fact]
    public async Task CreateDirectoryCommand_WithNullParent_CreatesInRoot()
    {
        // Arrange
        var (viewModel, projectService, _, projectContext) = CreateViewModelWithMocks();
        projectContext.IsProjectOpen.Returns(true);
        projectService.CreateDirectoryAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new FileEntry { Name = "NuevaCarpeta", RelativePath = "NuevaCarpeta", Type = FileType.Directory });

        // Act
        await viewModel.CreateDirectoryCommand.ExecuteAsync(null);

        // Assert
        await projectService.Received(1).CreateDirectoryAsync("", "NuevaCarpeta");
    }

    #endregion

    #region ConfirmRenameCommand Tests

    [Fact]
    public async Task ConfirmRenameCommand_RenamesEntry_WhenNameChanged()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var entry = new FileEntryViewModel("oldName.scriban", "oldName.scriban", FileType.Script)
        {
            IsEditing = true,
            EditingName = "newName.scriban"
        };
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.ConfirmRenameCommand.ExecuteAsync(entry);

        // Assert
        await projectService.Received(1).RenameAsync("oldName.scriban", "newName.scriban");
    }

    [Fact]
    public async Task ConfirmRenameCommand_DoesNotRename_WhenNameUnchanged()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var entry = new FileEntryViewModel("sameName.scriban", "sameName.scriban", FileType.Script)
        {
            IsEditing = true,
            EditingName = "sameName.scriban"
        };
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.ConfirmRenameCommand.ExecuteAsync(entry);

        // Assert
        await projectService.DidNotReceive().RenameAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ConfirmRenameCommand_DoesNotRename_WhenNameEmpty()
    {
        // Arrange
        var (viewModel, projectService, dialogService, _) = CreateViewModelWithMocks();
        var entry = new FileEntryViewModel("oldName.scriban", "oldName.scriban", FileType.Script)
        {
            IsEditing = true,
            EditingName = "   "
        };
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.ConfirmRenameCommand.ExecuteAsync(entry);

        // Assert
        await projectService.DidNotReceive().RenameAsync(Arg.Any<string>(), Arg.Any<string>());
        await dialogService.Received(1).ShowWarningAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region DeleteCommand Tests

    [Fact]
    public async Task DeleteCommand_DeletesEntry_WhenUserConfirms()
    {
        // Arrange
        var (viewModel, projectService, dialogService, _) = CreateViewModelWithMocks();
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        var entry = new FileEntryViewModel("test.scriban", "test.scriban", FileType.Script);
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(entry);

        // Assert
        await dialogService.Received(1).ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>());
        await projectService.Received(1).DeleteAsync("test.scriban");
    }

    [Fact]
    public async Task DeleteCommand_DoesNotDelete_WhenUserCancels()
    {
        // Arrange
        var (viewModel, projectService, dialogService, _) = CreateViewModelWithMocks();
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var entry = new FileEntryViewModel("test.scriban", "test.scriban", FileType.Script);
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(entry);

        // Assert
        await dialogService.Received(1).ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>());
        await projectService.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }

    #endregion

    #region DuplicateCommand Tests

    [Fact]
    public async Task DuplicateCommand_DuplicatesEntry_WithCorrectPath()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var entry = new FileEntryViewModel("test.scriban", "test.scriban", FileType.Script);
        viewModel.FileTree.Add(entry);

        // Act
        await viewModel.DuplicateCommand.ExecuteAsync(entry);

        // Assert
        await projectService.Received(1).DuplicateAsync("test.scriban");
    }

    #endregion

    #region MoveCommand Tests

    [Fact]
    public async Task MoveCommand_MovesEntry_ToTargetDirectory()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var sourceEntry = new FileEntryViewModel("test.scriban", "test.scriban", FileType.Script);
        var targetFolder = new FileEntryViewModel("folder", "folder", FileType.Directory);
        viewModel.FileTree.Add(sourceEntry);
        viewModel.FileTree.Add(targetFolder);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(("test.scriban", targetFolder));

        // Assert
        await projectService.Received(1).MoveAsync("test.scriban", "folder");
    }

    [Fact]
    public async Task MoveCommand_MovesEntry_ToParentFolder_WhenTargetIsFile()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var sourceEntry = new FileEntryViewModel("source.scriban", "source.scriban", FileType.Script);
        var targetFile = new FileEntryViewModel("target.json", "folder/target.json", FileType.Data);
        viewModel.FileTree.Add(sourceEntry);
        viewModel.FileTree.Add(targetFile);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(("source.scriban", targetFile));

        // Assert
        await projectService.Received(1).MoveAsync("source.scriban", "folder");
    }

    [Fact]
    public async Task MoveCommand_DoesNotMove_WhenTargetIsSameParentFolder()
    {
        // Arrange
        var (viewModel, projectService, _, _) = CreateViewModelWithMocks();
        var sourceEntry = new FileEntryViewModel("test.scriban", "folder/test.scriban", FileType.Script);
        var targetFolder = new FileEntryViewModel("folder", "folder", FileType.Directory);
        viewModel.FileTree.Add(sourceEntry);
        viewModel.FileTree.Add(targetFolder);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(("folder/test.scriban", targetFolder));

        // Assert
        await projectService.DidNotReceive().MoveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion
}
