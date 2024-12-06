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
            MinorVersion = 3,
        });

#if DEBUG
        Extensions.EnableDebug(false);
#endif
    }
}
