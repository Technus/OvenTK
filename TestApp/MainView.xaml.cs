using OvenTK.Lib;
using System.Windows.Controls;

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

#if DEBUG
        DebugExtensions.EnableDebug(false);
#endif
    }
}
