using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Project;
using Msi.TemplateCodeGenerator.Services.Templates;
using NSubstitute;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services.Templates;

public class ScriptEngineTests : IDisposable
{
    private readonly string _tempDir;

    public ScriptEngineTests()
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

    private static (ScriptEngine Engine, IElementCatalog Catalog) CreateEngine(string projectPath)
    {
        ProjectContext projectContext = new();
        ((IProjectContextMutator)projectContext).SetProject(new ProjectModel { FolderPath = projectPath }, projectPath);
        FileSystem fileSystem = new(NullLogger<FileSystem>.Instance);
        ILogger<ScriptOutputWriter> outputWriterLogger = NullLogger<ScriptOutputWriter>.Instance;
        ScriptOutputWriter outputWriter = new(fileSystem, projectContext, outputWriterLogger);
        IElementCatalog elementCatalog = Substitute.For<IElementCatalog>();
        ILogger<ScriptEngine> engineLogger = NullLogger<ScriptEngine>.Instance;
        ScriptEngine engine = new(outputWriter, elementCatalog, engineLogger);
        return (engine, elementCatalog);
    }

    [Fact]
    public async Task ExecuteAsync_WithSyntaxError_ReturnsFailure()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ for x in }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidSimpleScript_ReturnsSuccess()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ 1 + 1 }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_WithPascalCaseHelper_ReturnsSuccess()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ PascalCase \"hello_world\" }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithGetAllElements_ReturnsElements()
    {
        (ScriptEngine engine, IElementCatalog catalog) = CreateEngine(_tempDir);

        List<Element> elements = new()
        {
            new Element { Id = "test-1", Name = "Test1", Type = "Workflow" },
            new Element { Id = "test-2", Name = "Test2", Type = "Vista" }
        };
        catalog.GetAll().Returns(elements);

        string script = "{{ GetAllElements().size }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success, $"Script failed: {string.Join(", ", result.Errors)}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_WithWriteToFile_WritesFileToDisk()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ write_to_file \"output.txt\" \"hello content\" }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Single(result.Outputs);
        Assert.Equal("output.txt", result.Outputs[0].Path);
        Assert.Equal("hello content", result.Outputs[0].Content);

        string outputPath = Path.Combine(_tempDir, "output.txt");
        Assert.True(File.Exists(outputPath));
        string writtenContent = await File.ReadAllTextAsync(outputPath);
        Assert.Equal("hello content", writtenContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithWriteToFileInSubdirectory_CreatesDirectory()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ write_to_file \"sub/dir/output.txt\" \"nested content\" }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Single(result.Outputs);

        string outputPath = Path.Combine(_tempDir, "sub", "dir", "output.txt");
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleWriteToFile_CapturesAllOutputs()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = """
        {{ write_to_file "file1.txt" "content1" }}
        {{ write_to_file "file2.txt" "content2" }}
        {{ write_to_file "file3.txt" "content3" }}
        """;

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Equal(3, result.Outputs.Count);
        Assert.Equal("file1.txt", result.Outputs[0].Path);
        Assert.Equal("file2.txt", result.Outputs[1].Path);
        Assert.Equal("file3.txt", result.Outputs[2].Path);
    }

    [Fact]
    public async Task ExecuteAsync_WithRuntimeError_ReturnsFailure()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ undefined_variable.method() }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_WithConstantText_ReturnsRenderedContent()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "Hello, World!";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Equal("Hello, World!", result.RenderedContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithInterpolation_ReturnsRenderedContent()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = "{{ 2 + 2 }}";

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.Equal("4", result.RenderedContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithWriteToFile_ReturnsRenderedContentAndOutputs()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string script = """
        {{ capture content }}Hello, World!{{ end }}
        {{ write_to_file "output.txt" content }}
        """;

        ScriptExecutionResult result = await engine.ExecuteAsync(script, "test.scriban");

        Assert.True(result.Success);
        Assert.NotEmpty(result.RenderedContent);
        Assert.Single(result.Outputs);
        Assert.Equal("output.txt", result.Outputs[0].Path);
        Assert.Equal("Hello, World!", result.Outputs[0].Content);
    }

    #region ProcessPreviewAsync Tests

    [Fact]
    public async Task ProcessPreviewAsync_WithValidTemplate_ReturnsSuccess()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "Hello, {{ name }}!";

        ScriptExecutionResult result = await engine.ProcessPreviewAsync(template);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Outputs);
    }

    [Fact]
    public async Task ProcessPreviewAsync_WithSyntaxError_ReturnsFailure()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ for x in }}";

        ScriptExecutionResult result = await engine.ProcessPreviewAsync(template);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(result.Outputs);
    }

    [Fact]
    public async Task ProcessPreviewAsync_WithEmptyTemplate_ReturnsSuccess()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "";

        ScriptExecutionResult result = await engine.ProcessPreviewAsync(template);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal("", result.RenderedContent);
    }

    [Fact]
    public async Task ProcessPreviewAsync_WithInterpolation_ReturnsRenderedContent()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ 2 + 2 }}";

        ScriptExecutionResult result = await engine.ProcessPreviewAsync(template);

        Assert.True(result.Success);
        Assert.Equal("4", result.RenderedContent);
    }

    [Fact]
    public async Task ProcessPreviewAsync_WithWriteToFile_DoesNotWriteToDisk()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ write_to_file \"output.txt\" \"content\" }}";

        ScriptExecutionResult result = await engine.ProcessPreviewAsync(template);

        Assert.True(result.Success);
        Assert.Empty(result.Outputs);

        string outputPath = Path.Combine(_tempDir, "output.txt");
        Assert.False(File.Exists(outputPath));
    }

    #endregion

    #region ValidateSyntaxAsync Tests

    [Fact]
    public async Task ValidateSyntaxAsync_WithValidSyntax_ReturnsEmptyList()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ for x in items }}{{ x }}{{ end }}";

        IReadOnlyList<string> errors = await engine.ValidateSyntaxAsync(template);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithSyntaxError_ReturnsErrors()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ for x in }}";

        IReadOnlyList<string> errors = await engine.ValidateSyntaxAsync(template);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithEmptyTemplate_ReturnsEmptyList()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "";

        IReadOnlyList<string> errors = await engine.ValidateSyntaxAsync(template);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithConstantText_ReturnsEmptyList()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "Hello, World!";

        IReadOnlyList<string> errors = await engine.ValidateSyntaxAsync(template);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithMultipleErrors_ReturnsAllErrors()
    {
        (ScriptEngine engine, _) = CreateEngine(_tempDir);
        string template = "{{ for x in }}{{ end }} {{ if }}{{ end }}";

        IReadOnlyList<string> errors = await engine.ValidateSyntaxAsync(template);

        Assert.NotEmpty(errors);
        Assert.True(errors.Count >= 1);
    }

    #endregion
}
