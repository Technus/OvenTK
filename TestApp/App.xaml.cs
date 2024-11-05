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

        var view = new MainView();
        var viewModel = new MainViewModel();
        
        view.DataContext = viewModel;
        view.GLWpfControl.Render += viewModel.OnRender;
        view.GLWpfControl.SizeChanged += viewModel.OnResize;

        var window = new MainWindow();
        window.DataContext = view;
        MainWindow = window;
        MainWindow.Show();
    }
}

