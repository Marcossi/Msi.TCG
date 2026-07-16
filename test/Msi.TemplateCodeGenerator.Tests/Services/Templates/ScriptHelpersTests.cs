using Msi.TemplateCodeGenerator.Services.Templates;

namespace Msi.TemplateCodeGenerator.Tests.Services.Templates;

public class ScriptHelpersTests
{
    [Theory]
    [InlineData("order_processing", "OrderProcessing")]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("my-long-name", "MyLongName")]
    [InlineData("some name here", "SomeNameHere")]
    public void PascalCase_WithSeparatedInput_ConvertsCorrectly(string input, string expected)
    {
        string result = ScriptHelpers.PascalCase(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("orderProcessing", "OrderProcessing")]
    [InlineData("hello", "Hello")]
    public void PascalCase_WithCamelCase_ConvertsCorrectly(string input, string expected)
    {
        string result = ScriptHelpers.PascalCase(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("OrderProcessing", "OrderProcessing")]
    [InlineData("Already", "Already")]
    public void PascalCase_WithPascalCase_ReturnsUnchanged(string input, string expected)
    {
        string result = ScriptHelpers.PascalCase(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PascalCase_WithNullOrEmpty_ReturnsInput(string? input)
    {
        string? result = ScriptHelpers.PascalCase(input!);

        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("OrderProcessing", "orderProcessing")]
    [InlineData("Hello", "hello")]
    [InlineData("order_processing", "orderProcessing")]
    public void CamelCase_ConvertsCorrectly(string input, string expected)
    {
        string result = ScriptHelpers.CamelCase(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CamelCase_WithNullOrEmpty_ReturnsInput(string? input)
    {
        string? result = ScriptHelpers.CamelCase(input!);

        Assert.Equal(input, result);
    }
}
