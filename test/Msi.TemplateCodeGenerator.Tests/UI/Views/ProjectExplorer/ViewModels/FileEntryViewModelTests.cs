using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;

namespace Msi.TemplateCodeGenerator.Tests.UI.Views.ProjectExplorer.ViewModels;

public class FileEntryViewModelTests
{
    [Fact]
    public void SetError_SetsHasErrorAndErrorMessage()
    {
        FileEntryViewModel viewModel = new("file.json", "file.json", FileType.Data);

        viewModel.SetError("Invalid JSON");

        Assert.True(viewModel.HasError);
        Assert.Equal("Invalid JSON", viewModel.ErrorMessage);
    }

    [Fact]
    public void ClearError_ClearsHasErrorAndErrorMessage()
    {
        FileEntryViewModel viewModel = new("file.json", "file.json", FileType.Data);
        viewModel.SetError("Invalid JSON");

        viewModel.ClearError();

        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        FileEntryViewModel viewModel = new("test.scriban", "scripts/test.scriban", FileType.Script);

        Assert.Equal("test.scriban", viewModel.Name);
        Assert.Equal("scripts/test.scriban", viewModel.RelativePath);
        Assert.Equal(FileType.Script, viewModel.Type);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Empty, viewModel.ErrorMessage);
    }
}
