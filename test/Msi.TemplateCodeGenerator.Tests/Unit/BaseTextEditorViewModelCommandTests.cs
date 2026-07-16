using FluentAssertions;
using Microsoft.Extensions.Logging;
using Msi.TemplateCodeGenerator.Interfaces;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;
using NSubstitute;

public sealed class BaseTextEditorViewModelCommandTests
{
    [Fact]
    public void CanExecute_Save_WhenDirtyAndHasPath_ReturnsTrue()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.IsDirty = true;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_Save_WhenNotDirty_ReturnsFalse()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.IsDirty = false;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_Save_WhenNoPath_ReturnsFalse()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        vm.FilePath = string.Empty;
        vm.IsDirty = true;

        // Act
        bool canExecute = vm.CanExecute("Save");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_UnknownCommand_ReturnsFalse()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        // Act
        bool canExecute = vm.CanExecute("UnknownCommand");

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Save_WhenCanExecute_WritesFile()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.WriteTextAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        vm.FilePath = "C:\\test.scriban";
        vm.Content = "test content";
        vm.IsDirty = true;

        // Act
        await vm.ExecuteAsync("Save");

        // Assert
        await fileSystem.Received(1).WriteTextAsync("C:\\test.scriban", "test content");
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileSystem = Substitute.For<IFileSystem>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<BaseTextEditorViewModel>>();
        var vm = new TestTextEditorViewModel(fileSystem, dialogService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.ExecuteAsync("UnknownCommand"));
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
