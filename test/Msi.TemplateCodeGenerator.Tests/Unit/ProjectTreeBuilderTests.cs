using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Msi.TemplateCodeGenerator.Models;
using Msi.TemplateCodeGenerator.Services;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using ProjectModel = Msi.TemplateCodeGenerator.Models.Project;

namespace Msi.TemplateCodeGenerator.Tests.Services;

public class ProjectTreeBuilderTests
{
    private static ProjectTreeBuilder CreateBuilder()
    {
        ILogger<ProjectTreeBuilder> logger = NullLogger<ProjectTreeBuilder>.Instance;
        ILogger<FileEntryViewModel> fileEntryLogger = NullLogger<FileEntryViewModel>.Instance;
        return new ProjectTreeBuilder(logger, fileEntryLogger);
    }

    [Fact]
    public void BuildFileTree_WithEmptyProject_ReturnsSingleNodeRoot()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>()
        };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", new HashSet<string>());

        Assert.Single(tree);
        Assert.Equal("test.scribanproj", tree[0].Name);
        Assert.Equal(FileType.Project, tree[0].Type);
        Assert.Empty(tree[0].Children);
    }

    [Fact]
    public void BuildFileTree_WithSingleFile_ReturnsRootWithOneChild()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>
            {
                new() { Name = "script.scriban", RelativePath = "script.scriban", Type = FileType.Script }
            }
        };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", new HashSet<string>());

        Assert.Single(tree);
        Assert.Single(tree[0].Children);
        Assert.Equal("script.scriban", tree[0].Children[0].Name);
        Assert.Equal(FileType.Script, tree[0].Children[0].Type);
    }

    [Fact]
    public void BuildFileTree_WithNestedStructure_BuildsCorrectHierarchy()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>
            {
                new() { Name = "sub", RelativePath = "sub", Type = FileType.Directory },
                new() { Name = "script.scriban", RelativePath = "sub/script.scriban", Type = FileType.Script }
            }
        };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", new HashSet<string>());

        Assert.Single(tree);
        FileEntryViewModel folder = tree[0].Children[0];
        Assert.Equal("sub", folder.Name);
        Assert.Equal(FileType.Directory, folder.Type);
        Assert.Single(folder.Children);
        Assert.Equal("script.scriban", folder.Children[0].Name);
    }

    [Fact]
    public void BuildFileTree_DirectoriesBeforeFiles_SortsCorrectly()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>
            {
                new() { Name = "z_file.scriban", RelativePath = "z_file.scriban", Type = FileType.Script },
                new() { Name = "a_dir", RelativePath = "a_dir", Type = FileType.Directory }
            }
        };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", new HashSet<string>());

        Assert.Equal(2, tree[0].Children.Count);
        Assert.Equal(FileType.Directory, tree[0].Children[0].Type);
        Assert.Equal(FileType.Script, tree[0].Children[1].Type);
    }

    [Fact]
    public void BuildFileTree_WithExpandedPaths_RestoresExpansionState()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>
            {
                new() { Name = "sub", RelativePath = "sub", Type = FileType.Directory },
                new() { Name = "script.scriban", RelativePath = "sub/script.scriban", Type = FileType.Script }
            }
        };

        HashSet<string> expandedPaths = new() { "sub" };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", expandedPaths);

        Assert.True(tree[0].Children[0].IsExpanded);
    }

    [Fact]
    public void BuildFileTree_ExcludesProjectFileFromChildren()
    {
        ProjectTreeBuilder builder = CreateBuilder();
        ProjectModel project = new()
        {
            Name = "Test",
            FolderPath = "/test",
            Files = new List<FileEntry>
            {
                new() { Name = "test.scribanproj", RelativePath = "test.scribanproj", Type = FileType.Other },
                new() { Name = "script.scriban", RelativePath = "script.scriban", Type = FileType.Script }
            }
        };

        ObservableCollection<FileEntryViewModel> tree = builder.BuildFileTree(
            project, "/test/test.scribanproj", new HashSet<string>());

        Assert.Single(tree);
        Assert.Single(tree[0].Children);
        Assert.NotEqual("test.scribanproj", tree[0].Children[0].Name);
    }
}
