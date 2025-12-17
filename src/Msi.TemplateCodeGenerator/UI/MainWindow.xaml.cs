using System.Windows;

namespace Msi.TemplateCodeGenerator.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal partial class MainWindow : Window
{
    public MainWindow(MainShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Fusionamos los recursos de la aplicación con los de esta ventana.
        // Esto asegura que los DataTemplates definidos en App.xaml estén disponibles aquí.
        if (Application.Current.Resources.MergedDictionaries.Count > 0)
        {
            Resources.MergedDictionaries.Add(Application.Current.Resources);
        }
    }
}