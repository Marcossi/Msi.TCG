using FluentAssertions;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Services.Commands;
using NSubstitute;

public sealed class CommandRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoActiveRoute_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns((ICommandRoute?)null);
        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveRoute_ExecutesCommand()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(true);
        route.ExecuteAsync("Save").Returns(Task.CompletedTask);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeTrue();
        await route.Received(1).ExecuteAsync("Save");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanExecuteReturnsFalse_DoesNotExecute()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(false);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeFalse();
        await route.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }

    [Fact]
    public void CanExecute_WithNoActiveRoute_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns((ICommandRoute?)null);
        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = registry.CanExecute("Save");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_WithActiveRoute_DelegatesToRoute()
    {
        // Arrange
        var route = Substitute.For<ICommandRoute>();
        route.CanExecute("Save").Returns(true);

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(route);

        var logger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, logger);

        // Act
        bool result = registry.CanExecute("Save");

        // Assert
        result.Should().BeTrue();
        route.Received(1).CanExecute("Save");
    }
}
