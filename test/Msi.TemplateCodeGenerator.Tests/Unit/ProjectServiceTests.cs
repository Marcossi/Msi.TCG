global using NSubstitute;

using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Services.Project;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services.Project;

public class ProjectServiceTests
{
    private static ILogger<ProjectService> Logger => NullLogger<ProjectService>.Instance;

    private static IFileWatcherService CreateFileWatcher()
    {
        return NSubstitute.Substitute.For<IFileWatcherService>();
    }

    private static ProjectService CreateServiceWithRealContext(
        IProjectSerializer? serializer = null,
        bool projectOpen = false,
        ProjectModel? project = null,
        string? projectPath = null)
    {
        var context = new ProjectContext();
        if (projectOpen)
        {
            ((IProjectContextMutator)context).SetProject(project ?? new ProjectModel { Name = "Test" }, projectPath ?? "C:\\test.json");
        }
        return new ProjectService(
            context,
            context,
            serializer ?? Substitute.For<IProjectSerializer>(),
            Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            Substitute.For<IFileSystem>(),
            Substitute.For<IProjectExplorerStateService>(),
            WeakReferenceMessenger.Default,
            Logger);
    }

    [Fact]
    public void Constructor_Injects_Logger()
    {
        var context = new ProjectContext();
        var service = new ProjectService(
            context,
            context,
            Substitute.For<IProjectSerializer>(),
            Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            Substitute.For<IFileSystem>(),
            Substitute.For<IProjectExplorerStateService>(),
            WeakReferenceMessenger.Default,
            Logger);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task OpenProjectAsync_Throws_OnEmptyPath()
    {
        var service = CreateServiceWithRealContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenProjectAsync(string.Empty));
    }

    [Fact]
    public async Task OpenProjectAsync_Throws_OnNullPath()
    {
        var service = CreateServiceWithRealContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenProjectAsync(null!));
    }

    [Fact]
    public async Task SaveProjectAsync_Throws_WhenNoProjectOpen()
    {
        var service = CreateServiceWithRealContext(projectOpen: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsync());
    }

    [Fact]
    public async Task SaveProjectAsync_Throws_WhenPathNotSet()
    {
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { Name = "Test" }, null!);
        var serializer = NSubstitute.Substitute.For<IProjectSerializer>();
        var service = new ProjectService(context, context, serializer, NSubstitute.Substitute.For<IElementCatalog>(), CreateFileWatcher(), NSubstitute.Substitute.For<IFileSystem>(), NSubstitute.Substitute.For<IProjectExplorerStateService>(), WeakReferenceMessenger.Default, Logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsync());
    }

    [Fact]
    public async Task SaveProjectAsAsync_Throws_WhenNoProjectOpen()
    {
        var service = CreateServiceWithRealContext(projectOpen: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsAsync("C:\\new.json"));
    }

    [Fact]
    public async Task SaveProjectAsAsync_Throws_OnEmptyPath()
    {
        var service = CreateServiceWithRealContext(
            projectOpen: true,
            project: new ProjectModel { Name = "Test" },
            projectPath: "C:\\old.json");

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveProjectAsAsync(string.Empty));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnEmptyPath()
    {
        var service = CreateServiceWithRealContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync(string.Empty, "MyProject"));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnEmptyName()
    {
        var service = CreateServiceWithRealContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync("C:\\project.json", string.Empty));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnNullName()
    {
        var service = CreateServiceWithRealContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync("C:\\project.json", null!));
    }

    [Fact]
    public async Task CloseProjectAsync_CleansContext()
    {
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { Name = "Test" }, "C:\\test.json");
        var service = new ProjectService(context, context, NSubstitute.Substitute.For<IProjectSerializer>(), NSubstitute.Substitute.For<IElementCatalog>(), CreateFileWatcher(), NSubstitute.Substitute.For<IFileSystem>(), NSubstitute.Substitute.For<IProjectExplorerStateService>(), WeakReferenceMessenger.Default, Logger);

        await service.CloseProjectAsync();

        Assert.Null(context.CurrentProject);
        Assert.Null(context.CurrentProjectPath);
    }

    [Fact]
    public async Task SaveProjectAsync_CallsSerializer()
    {
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { Name = "Test" }, "C:\\test.json");
        var serializer = NSubstitute.Substitute.For<IProjectSerializer>();
        var service = new ProjectService(context, context, serializer, NSubstitute.Substitute.For<IElementCatalog>(), CreateFileWatcher(), NSubstitute.Substitute.For<IFileSystem>(), NSubstitute.Substitute.For<IProjectExplorerStateService>(), WeakReferenceMessenger.Default, Logger);

        await service.SaveProjectAsync();

        await serializer.Received(1).SaveAsync(context.CurrentProject!, "C:\\test.json");
    }

    #region Messaging Tests

    [Fact]
    public async Task OpenProjectAsync_PublishesProjectOpenedMessage()
    {
        // Arrange
        var messenger = new WeakReferenceMessenger();
        var context = new ProjectContext();
        var serializer = Substitute.For<IProjectSerializer>();
        var project = new ProjectModel { Name = "Test", FolderPath = "C:\\" };
        serializer.LoadAsync(Arg.Any<string>()).Returns(Task.FromResult(project));
        
        var service = new ProjectService(
            context,
            context,
            serializer,
            Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            Substitute.For<IFileSystem>(),
            Substitute.For<IProjectExplorerStateService>(),
            messenger,
            Logger);

        ProjectOpenedMessage? receivedMessage = null;
        messenger.Register<ProjectOpenedMessage>(this, (r, m) => receivedMessage = m);

        // Act
        await service.OpenProjectAsync("C:\\test.json");

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal("C:\\test.json", receivedMessage.ProjectPath);
    }

    [Fact]
    public async Task CloseProjectAsync_PublishesProjectClosedMessage()
    {
        // Arrange
        var messenger = new WeakReferenceMessenger();
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { Name = "Test" }, "C:\\test.json");
        
        var service = new ProjectService(
            context,
            context,
            Substitute.For<IProjectSerializer>(),
            Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            Substitute.For<IFileSystem>(),
            Substitute.For<IProjectExplorerStateService>(),
            messenger,
            Logger);

        bool messageReceived = false;
        messenger.Register<ProjectClosedMessage>(this, (r, m) => messageReceived = true);

        // Act
        await service.CloseProjectAsync();

        // Assert
        Assert.True(messageReceived);
    }

    [Fact]
    public async Task SaveProjectAsync_PublishesProjectSavedMessage()
    {
        // Arrange
        var messenger = new WeakReferenceMessenger();
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { Name = "Test" }, "C:\\test.json");
        var serializer = Substitute.For<IProjectSerializer>();
        
        var service = new ProjectService(
            context,
            context,
            serializer,
            Substitute.For<IElementCatalog>(),
            CreateFileWatcher(),
            Substitute.For<IFileSystem>(),
            Substitute.For<IProjectExplorerStateService>(),
            messenger,
            Logger);

        ProjectSavedMessage? receivedMessage = null;
        messenger.Register<ProjectSavedMessage>(this, (r, m) => receivedMessage = m);

        // Act
        await service.SaveProjectAsync();

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal("C:\\test.json", receivedMessage.ProjectPath);
    }

    #endregion
}
