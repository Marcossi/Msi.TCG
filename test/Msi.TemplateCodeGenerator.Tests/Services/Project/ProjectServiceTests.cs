global using NSubstitute;

using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Services.Project;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services.Project;

public class ProjectServiceTests
{
    private static ILogger<ProjectService> Logger => NullLogger<ProjectService>.Instance;

    private static ProjectService CreateServiceWithRealContext(
        IProjectSerializer? serializer = null,
        bool projectOpen = false,
        ProjectModel? project = null,
        string? projectPath = null)
    {
        var context = new ProjectContext();
        if (projectOpen)
        {
            context.CurrentProject = project ?? new ProjectModel { Name = "Test" };
            context.CurrentProjectPath = projectPath ?? "C:\\test.json";
        }
        return new ProjectService(
            context,
            serializer ?? NSubstitute.Substitute.For<IProjectSerializer>(),
            WeakReferenceMessenger.Default,
            Logger);
    }

    [Fact]
    public void Constructor_Injects_Logger()
    {
        // Arrange & Act
        var context = new ProjectContext();
        var service = new ProjectService(
            context,
            NSubstitute.Substitute.For<IProjectSerializer>(),
            WeakReferenceMessenger.Default,
            Logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task OpenProjectAsync_Throws_OnEmptyPath()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenProjectAsync(string.Empty));
    }

    [Fact]
    public async Task OpenProjectAsync_Throws_OnNullPath()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.OpenProjectAsync(null!));
    }

    [Fact]
    public async Task SaveProjectAsync_Throws_WhenNoProjectOpen()
    {
        // Arrange
        var service = CreateServiceWithRealContext(projectOpen: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsync());
    }

    [Fact]
    public async Task SaveProjectAsync_Throws_WhenPathNotSet()
    {
        // Arrange
        var context = new ProjectContext();
        context.CurrentProject = new ProjectModel { Name = "Test" };
        context.CurrentProjectPath = null;
        var serializer = NSubstitute.Substitute.For<IProjectSerializer>();
        var service = new ProjectService(context, serializer, WeakReferenceMessenger.Default, Logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsync());
    }

    [Fact]
    public async Task SaveProjectAsAsync_Throws_WhenNoProjectOpen()
    {
        // Arrange
        var service = CreateServiceWithRealContext(projectOpen: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveProjectAsAsync("C:\\new.json"));
    }

    [Fact]
    public async Task SaveProjectAsAsync_Throws_OnEmptyPath()
    {
        // Arrange
        var service = CreateServiceWithRealContext(
            projectOpen: true,
            project: new ProjectModel { Name = "Test" },
            projectPath: "C:\\old.json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveProjectAsAsync(string.Empty));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnEmptyPath()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync(string.Empty, "MyProject"));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnEmptyName()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync("C:\\project.json", string.Empty));
    }

    [Fact]
    public async Task CreateNewProjectAsync_Throws_OnNullName()
    {
        // Arrange
        var service = CreateServiceWithRealContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateNewProjectAsync("C:\\project.json", null!));
    }

    [Fact]
    public async Task CloseProjectAsync_CleansContext()
    {
        // Arrange
        var context = new ProjectContext();
        context.CurrentProject = new ProjectModel { Name = "Test" };
        context.CurrentProjectPath = "C:\\test.json";
        var service = new ProjectService(context, NSubstitute.Substitute.For<IProjectSerializer>(), WeakReferenceMessenger.Default, Logger);

        // Act
        await service.CloseProjectAsync();

        // Assert
        Assert.Null(context.CurrentProject);
        Assert.Null(context.CurrentProjectPath);
    }

    [Fact]
    public async Task SaveProjectAsync_CallsSerializer()
    {
        // Arrange
        var context = new ProjectContext();
        context.CurrentProject = new ProjectModel { Name = "Test" };
        context.CurrentProjectPath = "C:\\test.json";
        var serializer = NSubstitute.Substitute.For<IProjectSerializer>();
        var service = new ProjectService(context, serializer, WeakReferenceMessenger.Default, Logger);

        // Act
        await service.SaveProjectAsync();

        // Assert
        await serializer.Received(1).SaveAsync(context.CurrentProject!, "C:\\test.json");
    }
}
