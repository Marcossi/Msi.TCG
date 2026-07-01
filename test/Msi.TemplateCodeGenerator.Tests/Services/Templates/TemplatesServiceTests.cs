using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.Tests.Services.Templates;

public class TemplatesServiceTests
{
    private static ILogger<TemplatesService> Logger => NullLogger<TemplatesService>.Instance;

    [Fact]
    public void Constructor_Injects_Logger()
    {
        // Arrange & Act
        var service = new TemplatesService(Logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsSuccess_OnEmptyContent()
    {
        // Arrange
        var service = new TemplatesService(Logger);

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(string.Empty);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Result);
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsSuccess_OnValidTemplate()
    {
        // Arrange
        var service = new TemplatesService(Logger);
        string template = "Hola {{ Model.ProjectName }}";

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(template);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Result);
        Assert.Contains("Hola", result.Result);
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsFailure_OnSyntaxError()
    {
        // Arrange
        var service = new TemplatesService(Logger);
        string template = "{{ for x in y }}";

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(template);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessTemplateAsync_ReturnsSuccess_OnWhitespace()
    {
        // Arrange
        var service = new TemplatesService(Logger);

        // Act
        TemplateResult result = await service.ProcessTemplateAsync("   ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Result);
    }

    [Fact]
    public async Task ProcessTemplateAsync_RendersLoops()
    {
        // Arrange
        var service = new TemplatesService(Logger);
        string template = "{{~ for i in 1..3 ~}}{{ i }}{{~ end ~}}";

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(template);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("1", result.Result);
        Assert.Contains("2", result.Result);
        Assert.Contains("3", result.Result);
    }

    [Fact]
    public async Task ProcessTemplateAsync_RendersConditionals()
    {
        // Arrange
        var service = new TemplatesService(Logger);
        string template = "{{~ if true ~}}YES{{~ else ~}}NO{{~ end ~}}";

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(template);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("YES", result.Result);
    }

    [Fact]
    public async Task ProcessTemplateAsync_HandlesStringFunctions()
    {
        // Arrange
        var service = new TemplatesService(Logger);
        string template = "{{ \"hola\" | string.upcase }}";

        // Act
        TemplateResult result = await service.ProcessTemplateAsync(template);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("HOLA", result.Result);
    }
}
