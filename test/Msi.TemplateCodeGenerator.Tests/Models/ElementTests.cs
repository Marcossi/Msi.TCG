using Msi.TemplateCodeGenerator.Models;

namespace Msi.TemplateCodeGenerator.Tests.Models;

public class ElementTests
{
    [Fact]
    public void Get_WithExistingProperty_ReturnsValue()
    {
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "Namespace", Type = "string", Value = "MyApp" }
            }
        };

        string result = element.Get<string>("Namespace");

        Assert.Equal("MyApp", result);
    }

    [Fact]
    public void Get_WithNonExistentProperty_ThrowsException()
    {
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>()
        };

        Assert.Throws<InvalidOperationException>(() => element.Get<string>("NonExistent"));
    }

    [Fact]
    public void Get_WithWrongType_ThrowsException()
    {
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "Count", Type = "int", Value = 42 }
            }
        };

        Assert.Throws<InvalidOperationException>(() => element.Get<string>("Count"));
    }

    [Fact]
    public void TryGet_WithExistingProperty_ReturnsTrueAndValue()
    {
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>
            {
                new() { Name = "IsActive", Type = "bool", Value = true }
            }
        };

        bool success = element.TryGet<bool>("IsActive", out bool value);

        Assert.True(success);
        Assert.True(value);
    }

    [Fact]
    public void TryGet_WithNonExistentProperty_ReturnsFalse()
    {
        Element element = new()
        {
            Id = "test-1",
            Name = "TestElement",
            Type = "Test",
            Properties = new List<ElementProperty>()
        };

        bool success = element.TryGet<string>("NonExistent", out string? value);

        Assert.False(success);
        Assert.Null(value);
    }
}
