using OvenTK.Lib;
using OvenTK.Lib.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OvenTK.TestApp;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        //Very important prepare step...
        GLWpfControl.Start(new()
        {
            MajorVersion = 4,
            MinorVersion = 5,
        });
        FallbackFinalizer.FinalizeLater = static (id, action) => 
            Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Background, id);
#if DEBUG
        DebugExtensions.EnableDebug(false);
#endif
    }
}
