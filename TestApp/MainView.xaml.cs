using OpenTK.Graphics;
using OpenTK.Wpf;
using OvenTK.Lib;
using OvenTK.Lib.Utility;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace OvenTK.TestApp;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : UserControl
{
    private static readonly MethodInfo _create;

    static MainView()
    {
        var ass = typeof(GLWpfControl).Assembly;
        var type = ass.GetType("OpenTK.Wpf.DxGlContext");
        _create = type.GetMethod("GetOrCreateSharedOpenGLContext", BindingFlags.Static | BindingFlags.NonPublic);
    }


    public MainView()
    {
        InitializeComponent();

        DataContext = new MainViewModel();

        Loaded += MainView_Loaded;
    }

    private static void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        var view = (MainView)sender;
        var viewModel = (MainViewModel)view.DataContext;

        var settings = new GLWpfControlSettings()
        {
            MajorVersion = 4,
            MinorVersion = 6,
        };
        settings.ContextToUse = (IGraphicsContext)_create.Invoke(null, [settings]);

        if (!Extensions.IsNVDXInterop())
            return;

        view.GLWpfControl.Render += viewModel.OnRender;
        view.GLWpfControl.SizeChanged += viewModel.OnResize;

        //Very important prepare step...
        view.GLWpfControl.Start(settings);

        //Fix no autorendering...
        view.GLWpfControl.Visibility = Visibility.Hidden;
        view.GLWpfControl.Visibility = Visibility.Visible;

        //Initialize
        viewModel.GLSetup();

        //Fix no size set
        viewModel.OnResize(view.GLWpfControl.RenderSize);

        FallbackFinalizer.FinalizeLater = static (id, action) =>
            Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Background, id);
#if DEBUG
        DebugExtensions.EnableDebug(false);
#endif

        view.Loaded -= MainView_Loaded;
    }
}
