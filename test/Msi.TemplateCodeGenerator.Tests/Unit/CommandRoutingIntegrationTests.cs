using FluentAssertions;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Services.Commands;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;
using NSubstitute;

public sealed class CommandRoutingIntegrationTests
{
    [Fact]
    public async Task SaveCommand_FromShell_WhenEditorActive_SavesFile()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.WriteTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var editorVm = new TestTextEditorViewModel(fileSystem, dialogService, logger);
        editorVm.FilePath = "C:\\test.scriban";
        editorVm.Content = "test content";
        editorVm.IsDirty = true;

        var context = Substitute.For<ICommandContext>();
        context.ActiveRoute.Returns(editorVm);

        var registryLogger = Substitute.For<ILogger<CommandRegistry>>();
        var registry = new CommandRegistry(context, registryLogger);

        // Act
        bool result = await registry.ExecuteAsync("Save");

        // Assert
        result.Should().BeTrue();
        await fileSystem.Received(1).WriteTextAsync("C:\\test.scriban", "test content");
        editorVm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_FromShell_WhenNoEditorActive_ReturnsFalse()
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

    private sealed class TestTextEditorViewModel : BaseTextEditorViewModel
    {
        public TestTextEditorViewModel(
            IFileSystem fileSystem,
            IDialogService dialogService,
            ILogger<BaseTextEditorViewModel> logger)
            : base(fileSystem, dialogService, logger)
        {
        }
    }
}
