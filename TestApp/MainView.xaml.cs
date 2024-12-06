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

        GLWpfControl.Start(new()
        {
            MajorVersion = 3,
            MinorVersion = 3,
        });

#if DEBUG
        Extensions.EnableDebug(false);
#endif
    }
}
