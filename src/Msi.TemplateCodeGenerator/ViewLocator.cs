using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Msi.TemplateCodeGenerator.UI.Shared;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer;
using Msi.TemplateCodeGenerator.UI.Views.ProjectExplorer.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.Shell;
using Msi.TemplateCodeGenerator.UI.Views.Shell.ViewModels;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor;
using Msi.TemplateCodeGenerator.UI.Views.TemplateEditor.ViewModels;

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
            case MainShellViewModel: return new UI.Views.Shell.MainShellView();
            case TemplateEditorShellViewModel: return new UI.Views.TemplateEditor.TemplateEditorShellView();
            case ProjectExplorerShellViewModel: return new UI.Views.ProjectExplorer.ProjectExplorerShellView();

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
