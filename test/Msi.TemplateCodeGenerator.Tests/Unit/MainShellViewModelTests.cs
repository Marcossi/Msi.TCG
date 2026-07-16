using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;
using NSubstitute;

namespace Msi.TemplateCodeGenerator.Tests.UI.Views.Shell.ViewModels;

public class MainShellViewModelTests
{
    private static MainShellViewModel CreateViewModel(
        IProjectService? projectService = null,
        IProjectContext? projectContext = null,
        ITemplatesService? templatesService = null,
        IDialogService? dialogService = null,
        INavigationService? navigationService = null)
    {
        return new MainShellViewModel(
            navigationService ?? Substitute.For<INavigationService>(),
            projectService ?? Substitute.For<IProjectService>(),
            projectContext ?? Substitute.For<IProjectContext>(),
            Substitute.For<IFileDialogService>(),
            Substitute.For<ICommandRegistry>(),
            templatesService ?? Substitute.For<ITemplatesService>(),
            dialogService ?? Substitute.For<IDialogService>(),
            Substitute.For<IApp>(),
            new WeakReferenceMessenger(),
            NullLogger<MainShellViewModel>.Instance);
    }

    [Fact]
    public async Task GenerateAllAsync_DelegatesToTemplatesService()
    {
        // Arrange
        ITemplatesService templatesService = Substitute.For<ITemplatesService>();
        templatesService.ExecuteAllScriptsAsync().Returns(new BatchExecutionResult
        {
            SuccessCount = 3,
            ErrorCount = 0,
            Errors = []
        });

        IProjectContext projectContext = Substitute.For<IProjectContext>();
        projectContext.IsProjectOpen.Returns(true);

        IDialogService dialogService = Substitute.For<IDialogService>();

        MainShellViewModel viewModel = CreateViewModel(
            projectContext: projectContext,
            templatesService: templatesService,
            dialogService: dialogService);

        // Act
        await viewModel.GenerateAllCommand.ExecuteAsync(null);

        // Assert
        await templatesService.Received(1).ExecuteAllScriptsAsync();
    }

    [Fact]
    public async Task GenerateAllAsync_ShowsSuccessMessage_WhenAllScriptsSucceed()
    {
        // Arrange
        ITemplatesService templatesService = Substitute.For<ITemplatesService>();
        templatesService.ExecuteAllScriptsAsync().Returns(new BatchExecutionResult
        {
            SuccessCount = 5,
            ErrorCount = 0,
            Errors = []
        });

        IProjectContext projectContext = Substitute.For<IProjectContext>();
        projectContext.IsProjectOpen.Returns(true);

        IDialogService dialogService = Substitute.For<IDialogService>();

        MainShellViewModel viewModel = CreateViewModel(
            projectContext: projectContext,
            templatesService: templatesService,
            dialogService: dialogService);

        // Act
        await viewModel.GenerateAllCommand.ExecuteAsync(null);

        // Assert
        await dialogService.Received(1).ShowInfoAsync(
            Arg.Is<string>(msg => msg.Contains("5 script(s)") && !msg.Contains("failed")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task GenerateAllAsync_ShowsErrorMessage_WhenSomeScriptsFail()
    {
        // Arrange
        ITemplatesService templatesService = Substitute.For<ITemplatesService>();
        templatesService.ExecuteAllScriptsAsync().Returns(new BatchExecutionResult
        {
            SuccessCount = 2,
            ErrorCount = 1,
            Errors = ["script.scriban: Syntax error"]
        });

        IProjectContext projectContext = Substitute.For<IProjectContext>();
        projectContext.IsProjectOpen.Returns(true);

        IDialogService dialogService = Substitute.For<IDialogService>();

        MainShellViewModel viewModel = CreateViewModel(
            projectContext: projectContext,
            templatesService: templatesService,
            dialogService: dialogService);

        // Act
        await viewModel.GenerateAllCommand.ExecuteAsync(null);

        // Assert
        await dialogService.Received(1).ShowInfoAsync(
            Arg.Is<string>(msg => msg.Contains("2 script(s)") && msg.Contains("1 script(s) failed") && msg.Contains("Syntax error")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task GenerateAllAsync_SetsStatusMessage_WhenExceptionOccurs()
    {
        // Arrange
        ITemplatesService templatesService = Substitute.For<ITemplatesService>();
        templatesService.ExecuteAllScriptsAsync().Returns(Task.FromException<BatchExecutionResult>(new InvalidOperationException("Test error")));

        IProjectContext projectContext = Substitute.For<IProjectContext>();
        projectContext.IsProjectOpen.Returns(true);

        IDialogService dialogService = Substitute.For<IDialogService>();

        MainShellViewModel viewModel = CreateViewModel(
            projectContext: projectContext,
            templatesService: templatesService,
            dialogService: dialogService);

        // Act
        await viewModel.GenerateAllCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Test error", viewModel.StatusMessage);
    }
}
