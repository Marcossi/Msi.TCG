using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.Messages;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.Services.Templates;
using NSubstitute;

namespace Msi.TemplateCodeGenerator.Tests.Services.Templates;

public class TemplatesServiceTests
{
    private static ILogger<TemplatesService> Logger => NullLogger<TemplatesService>.Instance;

    private static IScriptEngine CreateScriptEngine()
    {
        return Substitute.For<IScriptEngine>();
    }

    private static IFileSystem CreateFileSystem()
    {
        return new FileSystem(NullLogger<FileSystem>.Instance);
    }

    private static IProjectContext CreateProjectContext()
    {
        return Substitute.For<IProjectContext>();
    }

    private static IMessenger CreateMessenger()
    {
        return new WeakReferenceMessenger();
    }

    private static TemplatesService CreateService(IScriptEngine? scriptEngine = null)
    {
        return new TemplatesService(
            scriptEngine ?? CreateScriptEngine(),
            CreateFileSystem(),
            CreateProjectContext(),
            CreateMessenger(),
            Logger);
    }

    [Fact]
    public void Constructor_Injects_Logger()
    {
        TemplatesService service = CreateService();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsSuccess_OnEmptyContent()
    {
        IScriptEngine engine = CreateScriptEngine();
        TemplatesService service = CreateService(engine);

        TemplateResult result = await service.ProcessTemplateAsync(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Result);
        await engine.DidNotReceive().ProcessPreviewAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsSuccess_OnWhitespace()
    {
        IScriptEngine engine = CreateScriptEngine();
        TemplatesService service = CreateService(engine);

        TemplateResult result = await service.ProcessTemplateAsync("   ");

        Assert.True(result.IsSuccess);
        await engine.DidNotReceive().ProcessPreviewAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessTemplateAsync_DelegatesToScriptEngine()
    {
        IScriptEngine engine = CreateScriptEngine();
        engine.ProcessPreviewAsync("test content").Returns(new ScriptExecutionResult
        {
            Success = true,
            RenderedContent = "rendered output",
            Errors = [],
            Outputs = []
        });
        TemplatesService service = CreateService(engine);

        TemplateResult result = await service.ProcessTemplateAsync("test content");

        Assert.True(result.IsSuccess);
        Assert.Equal("rendered output", result.Result);
        await engine.Received(1).ProcessPreviewAsync("test content");
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsFailure_OnScriptEngineError()
    {
        IScriptEngine engine = CreateScriptEngine();
        engine.ProcessPreviewAsync("{{ bad syntax").Returns(new ScriptExecutionResult
        {
            Success = false,
            RenderedContent = string.Empty,
            Errors = ["Error 1", "Error 2"],
            Outputs = []
        });
        TemplatesService service = CreateService(engine);

        TemplateResult result = await service.ProcessTemplateAsync("{{ bad syntax");

        Assert.False(result.IsSuccess);
        Assert.Contains("Error 1", result.ErrorMessage);
        Assert.Contains("Error 2", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteScriptAsync_ReadsFileAndDelegatesToEngine()
    {
        IScriptEngine engine = CreateScriptEngine();
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ReadTextAsync("/path/script.scriban").Returns("script content");
        engine.ExecuteAsync("script content", "/path/script.scriban", false).Returns(new ScriptExecutionResult
        {
            Success = true,
            RenderedContent = "output",
            Errors = [],
            Outputs = []
        });

        TemplatesService service = new(engine, fileSystem, CreateProjectContext(), CreateMessenger(), Logger);

        ScriptExecutionResult result = await service.ExecuteScriptAsync("/path/script.scriban");

        Assert.True(result.Success);
        await fileSystem.Received(1).ReadTextAsync("/path/script.scriban");
        await engine.Received(1).ExecuteAsync("script content", "/path/script.scriban", false);
    }

    #region Messaging Tests

    [Fact]
    public async Task ExecuteScriptAsync_PublishesScriptExecutionCompletedMessage_OnSuccess()
    {
        IScriptEngine engine = CreateScriptEngine();
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ReadTextAsync("/path/script.scriban").Returns("script content");
        engine.ExecuteAsync("script content", "/path/script.scriban", false).Returns(new ScriptExecutionResult
        {
            Success = true,
            RenderedContent = "output",
            Errors = [],
            Outputs = []
        });

        WeakReferenceMessenger messenger = new();
        TemplatesService service = new(engine, fileSystem, CreateProjectContext(), messenger, Logger);

        ScriptExecutionCompletedMessage? receivedMessage = null;
        messenger.Register<ScriptExecutionCompletedMessage>(this, (r, m) => receivedMessage = m);

        await service.ExecuteScriptAsync("/path/script.scriban");

        Assert.NotNull(receivedMessage);
        Assert.Equal("/path/script.scriban", receivedMessage.ScriptPath);
        Assert.True(receivedMessage.Success);
        Assert.Empty(receivedMessage.Errors);
    }

    [Fact]
    public async Task ExecuteScriptAsync_PublishesScriptExecutionCompletedMessage_OnFailure()
    {
        IScriptEngine engine = CreateScriptEngine();
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ReadTextAsync("/path/script.scriban").Returns("bad script");
        engine.ExecuteAsync("bad script", "/path/script.scriban", false).Returns(new ScriptExecutionResult
        {
            Success = false,
            RenderedContent = string.Empty,
            Errors = ["Error 1", "Error 2"],
            Outputs = []
        });

        WeakReferenceMessenger messenger = new();
        TemplatesService service = new(engine, fileSystem, CreateProjectContext(), messenger, Logger);

        ScriptExecutionCompletedMessage? receivedMessage = null;
        messenger.Register<ScriptExecutionCompletedMessage>(this, (r, m) => receivedMessage = m);

        await service.ExecuteScriptAsync("/path/script.scriban");

        Assert.NotNull(receivedMessage);
        Assert.Equal("/path/script.scriban", receivedMessage.ScriptPath);
        Assert.False(receivedMessage.Success);
        Assert.Equal(2, receivedMessage.Errors.Count);
    }

    #endregion
}
