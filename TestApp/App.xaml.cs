using System.Windows;

namespace OvenTK.TestApp;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow();
        var view = new MainView();
        var viewModel = new MainViewModel();
        view.DataContext = viewModel;
        view.GLWpfControl.Render += viewModel.OnRender;
        window.DataContext = view;
        MainWindow = window;

        MainWindow.Show();
    }
}

