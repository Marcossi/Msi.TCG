using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Msi.TemplateCodeGenerator.UI;
using Msi.TemplateCodeGenerator.UI.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.TemplateEditor;

namespace Msi.TemplateCodeGenerator;

/// <summary>
/// Dado un viewModel retorna la View que le corresponde
/// </summary>
[RequiresUnreferencedCode("Default implementation of ViewLocator involves reflection which may be trimmed away.", Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Dado un viewModel, retorna la view correspondiente si es posible
    /// </summary>
    public Control? Build(object? viewModel)
    {
        if (viewModel is null)
            return null;

        switch(viewModel)
        {
            case MainShellViewModel: return new MainShellView();
            case TemplateEditorShellViewModel: return new TemplateEditorShellView();
            case ProjectExplorerShellViewModel: return new ProjectExplorerShellView();

            default:
                // Implmentacion por convencion. Si el viewModel es <xxx>ViewModel, la view sera <xxx>View
                string name = viewModel.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
                Type? type = Type.GetType(name);
                if (type is not null)
                    return (Control)Activator.CreateInstance(type)!;

                return new TextBlock { Text = "Not Found: " + name };
        }
    }

    public bool Match(object? data)
    {
        return data is BaseViewModel;
    }
}
