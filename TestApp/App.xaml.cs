using ObjectLayoutInspector;
using OvenTK.Lib;
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

        var list = new List<int>
        {
            1,
            2
        };

        var l = TypeLayout.GetLayout<List<int>>();

        var s1 = list.ToSpanUnsafe();
        list.SetCountUnsafe(1);
        var s2 = list.ToSpanUnsafe();

        var window = new MainWindow();

        window.DataContext = new MainViewModel();
        MainWindow = window;
        MainWindow.Show();
    }
}

