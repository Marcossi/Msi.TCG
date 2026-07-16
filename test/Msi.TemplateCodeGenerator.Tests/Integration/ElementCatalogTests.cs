using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Project;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services;

public class ElementCatalogTests : IDisposable
{
    private readonly string _tempDir;

    public ElementCatalogTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static ElementCatalog CreateCatalog(string projectPath)
    {
        ProjectContext context = new();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { FolderPath = projectPath }, projectPath);
        FileSystem fileSystem = new(NullLogger<FileSystem>.Instance);
        IMessenger messenger = new WeakReferenceMessenger();
        ILogger<ElementCatalog> logger = NullLogger<ElementCatalog>.Instance;
        return new ElementCatalog(context, fileSystem, messenger, logger);
    }

    [Fact]
    public async Task ReloadAsync_WithValidJson_LoadsElements()
    {
        string json = """
        {
          "Id": "wf-001",
          "Name": "OrderProcessing",
          "Type": "Workflow",
          "Properties": [
            { "Name": "Namespace", "Type": "string", "Value": "MyApp.Workflows" }
          ]
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "workflow.json"), json);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        IEnumerable<Element> all = catalog.GetAll().ToList();
        Assert.Single(all);

        Element? element = catalog.GetById("wf-001");
        Assert.NotNull(element);
        Assert.Equal("OrderProcessing", element.Name);
    }

    [Fact]
    public async Task ReloadAsync_WithInvalidJson_LogsErrorAndContinues()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "invalid.json"), "{ invalid json }");

        string validJson = """
        {
          "Id": "test-1",
          "Name": "Test",
          "Type": "Test",
          "Properties": []
        }
        """;
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "valid.json"), validJson);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        Assert.Single(catalog.GetAll());
        IReadOnlyList<LoadError> errors = catalog.GetLoadErrors();
        Assert.Single(errors);
        Assert.EndsWith("invalid.json", errors[0].FilePath);
    }

    [Fact]
    public async Task ReloadAsync_WithDuplicateId_LogsErrorAndIgnoresDuplicate()
    {
        string json1 = """
        {
          "Id": "dup-1",
          "Name": "First",
          "Type": "Test",
          "Properties": []
        }
        """;
        string json2 = """
        {
          "Id": "dup-1",
          "Name": "Second",
          "Type": "Test",
          "Properties": []
        }
        """;

        Directory.CreateDirectory(Path.Combine(_tempDir, "sub"));
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "first.json"), json1);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "sub", "second.json"), json2);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        Assert.Single(catalog.GetAll());
        Assert.Single(catalog.GetLoadErrors());
    }

    [Fact]
    public async Task ReloadAsync_WithMissingRequiredFields_LogsError()
    {
        string json = """
        {
          "Id": "",
          "Name": "Test",
          "Type": "Test",
          "Properties": []
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "empty-id.json"), json);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        Assert.Empty(catalog.GetAll());
        Assert.Single(catalog.GetLoadErrors());
    }

    [Fact]
    public async Task GetByType_ReturnsOnlyMatchingElements()
    {
        string workflowJson = """
        {
          "Id": "wf-1",
          "Name": "WF",
          "Type": "Workflow",
          "Properties": []
        }
        """;
        string viewJson = """
        {
          "Id": "v-1",
          "Name": "V",
          "Type": "Vista",
          "Properties": []
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "wf.json"), workflowJson);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v.json"), viewJson);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        IEnumerable<Element> workflows = catalog.GetByType("Workflow").ToList();
        Assert.Single(workflows);
        Assert.Equal("wf-1", workflows.First().Id);
    }

    [Fact]
    public async Task ReloadAsync_WithVariousTypes_DeserializesCorrectly()
    {
        string json = """
        {
          "Id": "elem-1",
          "Name": "MultiType",
          "Type": "Test",
          "Properties": [
            { "Name": "StrProp", "Type": "string", "Value": "hello" },
            { "Name": "IntProp", "Type": "int", "Value": 42 },
            { "Name": "BoolProp", "Type": "bool", "Value": true },
            { "Name": "DoubleProp", "Type": "double", "Value": 3.14 },
            { "Name": "ArrayProp", "Type": "array", "Value": [1, 2, 3] },
            { "Name": "ObjectProp", "Type": "object", "Value": {"key": "val"} }
          ]
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "multi.json"), json);

        ElementCatalog catalog = CreateCatalog(_tempDir);

        await catalog.ReloadAsync();

        Element? element = catalog.GetById("elem-1");
        Assert.NotNull(element);
        Assert.Equal("hello", element.Get<string>("StrProp"));
        Assert.Equal(42, element.Get<int>("IntProp"));
        Assert.True(element.Get<bool>("BoolProp"));
        Assert.Equal(3.14, element.Get<double>("DoubleProp"));
        Assert.IsType<List<object?>>(element.Get<object>("ArrayProp"));
        Assert.IsType<Dictionary<string, object?>>(element.Get<object>("ObjectProp"));
    }

    #region Messaging Tests

    [Fact]
    public async Task ReloadAsync_PublishesElementCatalogReloadedMessage()
    {
        // Arrange
        string json1 = """
        {
          "Id": "elem-1",
          "Name": "Element1",
          "Type": "Workflow",
          "Properties": []
        }
        """;
        string json2 = """
        {
          "Id": "elem-2",
          "Name": "Element2",
          "Type": "Vista",
          "Properties": []
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "elem1.json"), json1);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "elem2.json"), json2);

        var messenger = new WeakReferenceMessenger();
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { FolderPath = _tempDir }, _tempDir);
        var fileSystem = new FileSystem(NullLogger<FileSystem>.Instance);
        var catalog = new ElementCatalog(context, fileSystem, messenger, NullLogger<ElementCatalog>.Instance);

        ElementCatalogReloadedMessage? receivedMessage = null;
        messenger.Register<ElementCatalogReloadedMessage>(this, (r, m) => receivedMessage = m);

        // Act
        await catalog.ReloadAsync();

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(2, receivedMessage.ElementCount);
        Assert.Empty(receivedMessage.Errors);
    }

    [Fact]
    public async Task ReloadAsync_WithErrors_PublishesMessageWithErrors()
    {
        // Arrange
        string validJson = """
        {
          "Id": "elem-1",
          "Name": "Element1",
          "Type": "Workflow",
          "Properties": []
        }
        """;
        string invalidJson = "{ invalid json }";

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "valid.json"), validJson);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "invalid.json"), invalidJson);

        var messenger = new WeakReferenceMessenger();
        var context = new ProjectContext();
        ((IProjectContextMutator)context).SetProject(new ProjectModel { FolderPath = _tempDir }, _tempDir);
        var fileSystem = new FileSystem(NullLogger<FileSystem>.Instance);
        var catalog = new ElementCatalog(context, fileSystem, messenger, NullLogger<ElementCatalog>.Instance);

        ElementCatalogReloadedMessage? receivedMessage = null;
        messenger.Register<ElementCatalogReloadedMessage>(this, (r, m) => receivedMessage = m);

        // Act
        await catalog.ReloadAsync();

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(1, receivedMessage.ElementCount);
        Assert.Single(receivedMessage.Errors);
    }

    #endregion
}
