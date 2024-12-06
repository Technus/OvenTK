using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OvenTK.Lib;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace OvenTK.TestApp;
/// <summary>
/// Example use of OvenTK
/// </summary>
public class MainViewModel : DependencyObject
{
    private readonly FrequencyCounter _fpsCounter = new();
    private readonly FrequencyCounter _tpsCounter = new();

    /// <summary>
    /// How many cpus should create new data
    /// </summary>
    private const int _cpus = 4;

    /// <summary>
    /// rectangular boxes count
    /// </summary>
    private const int _boxes = 1000;
    /// <summary>
    /// square containers count
    /// </summary>
    private const int _containers = 1000;
    /// <summary>
    /// Total count
    /// </summary>
    /// <remarks>must distribute equally across <see cref="_cpus"/></remarks>
    private const int _count = _boxes + _containers;

    /// <summary>
    /// Base for the box/container list
    /// </summary>
    private const int _base = 0;
    /// <summary>
    /// First box id
    /// </summary>
    private const int _boxBase = _base;
    /// <summary>
    /// First container id
    /// </summary>
    private const int _containerBase = _base + _boxes;

    private const int _boxWidth = 128;
    private const int _boxHeight = 64;

    /// <summary>
    /// hacky digit renderer digit width
    /// </summary>
    private const int _digitWidth = 18;
    /// <summary>
    /// hacky digit renderer digit height
    /// </summary>
    private const int _digitHeight = 32;
    /// <summary>
    /// hacky digit renderer digit left margin
    /// </summary>
    private const int _leftMargin = _digitWidth / 2 + 4;

    /// <summary>
    /// bitmap font renderer max text lenght per box/container
    /// </summary>
    private const int _textLen = 7;

    private static readonly Random _random = new();

    private BufferStorage _sBoxVertices, _sContainerVertices, _sDigitVertices, _sUnitRectVertices, _sRectIndices;
    private BufferData _dUniform, _dXYAngle, _dColor, _dIdProg, _dXYAngleBMChar;
    private VertexArray _vBox, _vContainer, _vDigits, _vText;
    private Texture _tDigits;
    private BitmapFont _fConsolas;
    private ShaderProgram _pRect, _pDigits, _pText;
    private bool _invalidated = true;
    private Uniform _uniform = new()
    {
        CameraScale = 1,
        InstanceCount = _count,//do not edit on runtime... or prepare code to hadnle it
    };
    private TripleBufferSimple<(PosRot[] xyar, int[] color, IdProg[] cip_, BitmapFont.BFChar[] text)> _tTriple;

    /// <summary>
    /// Box/Container position and rotation
    /// </summary>
    private struct PosRot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Rot { get; set; }
    }

    /// <summary>
    /// Box id and programming destination
    /// </summary>
    private struct IdProg
    {
        public int Id { get; set; }
        public int Program { get; set; }
    }

    /// <summary>
    /// Uniform for shader operations
    /// </summary>
    private struct Uniform
    {
        /// <summary>
        /// Viewport resolution
        /// </summary>
        public Vector2 Resolution { get; set; }
        /// <summary>
        /// Position of screen center
        /// </summary>
        public Vector2 CameraPosition { get; set; }
        /// <summary>
        /// Hacky digit render position relative offset from box
        /// </summary>
        public Vector2 DigitPosition { get; set; }
        /// <summary>
        /// Camera zoom
        /// </summary>
        public float CameraScale { get; set; }
        /// <summary>
        /// which insance from <see cref="_base"/> are we rendering
        /// </summary>
        public int InstanceBase { get; set; }
        /// <summary>
        /// how many instances are rendered
        /// </summary>
        public int InstanceCount { get; set; }
        /// <summary>
        /// Hacky digit render digit divider (modulo 10 is hardcoded in shader)
        /// </summary>
        public int DigitDiv { get; set; }
        /// <summary>
        /// Which number from <see cref="IdProg"/> are we rendering
        /// </summary>
        public int DigitIndex { get; set; }
    }

    public double FPS
    {
        get => (double)GetValue(FPSProperty);
        set => SetValue(FPSProperty, value);
    }
    public static readonly DependencyProperty FPSProperty =
        DependencyProperty.Register("FPS", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d));

    public double TPS
    {
        get => (double)GetValue(TPSProperty);
        set => SetValue(TPSProperty, value);
    }
    public static readonly DependencyProperty TPSProperty =
        DependencyProperty.Register("TPS", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d));

    /// <summary>
    /// Center screen X
    /// </summary>
    public double X
    {
        get => (double)GetValue(XProperty);
        set => SetValue(XProperty, value);
    }
    public static readonly DependencyProperty XProperty =
        DependencyProperty.Register("X", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d, (o, e) =>
        {
            if (o is not MainViewModel vm)
                return;
            vm._uniform.CameraPosition = new((float)(double)e.NewValue, vm._uniform.CameraPosition.Y);
            vm._invalidated = true;
        }));

    /// <summary>
    /// Center screen Y
    /// </summary>
    public double Y
    {
        get => (double)GetValue(MyPropertyProperty);
        set => SetValue(MyPropertyProperty, value);
    }
    public static readonly DependencyProperty MyPropertyProperty =
        DependencyProperty.Register("MyProperty", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d, (o, e) =>
        {
            if (o is not MainViewModel vm)
                return;
            vm._uniform.CameraPosition = new(vm._uniform.CameraPosition.X, (float)(double)e.NewValue);
            vm._invalidated = true;
        }));

    /// <summary>
    /// Zoom
    /// </summary>
    public double ScaleLog
    {
        get => (double)GetValue(ScaleLogProperty);
        set => SetValue(ScaleLogProperty, value);
    }
    public static readonly DependencyProperty ScaleLogProperty =
        DependencyProperty.Register("ScaleLog", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d, (o, e) =>
        {
            if (o is not MainViewModel vm)
                return;
            var scale = (float)Math.Pow(10, (double)e.NewValue);
            vm._uniform.CameraScale = scale;
            vm._invalidated = true;
        }));

    public MainViewModel()
    {
        GLSetup();
        Task.Run(() => DataWriteMapped().ConfigureAwait(false));
    }

    /// <summary>
    /// Writes some random data to render
    /// </summary>
    /// <param name="cpus"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DataWriteMapped(int cpus = _cpus, CancellationToken token = default)
    {
        if (_count % cpus is not 0)
            throw new InvalidOperationException();

        while (!token.IsCancellationRequested)
        {
            var sw = Stopwatch.GetTimestamp();

            //if (_triple.IsStale)
            {
                _tpsCounter.PushEvent();
                await Dispatcher.BeginInvoke(() => TPS = _tpsCounter.Frequency);

                using var w = _tTriple.Write();
                var b = w.Buffer;

                var batch = _count / cpus;
                var tasks = Enumerable.Range(0, cpus).Select(i => Task.Run(() =>//split to different CPUs
                {
                    var start = i * batch;
                    var end = start + batch;
                    for (int j = start; j < end; j++)
                    {
                        b.xyar[j] = new PosRot
                        {
                            X = (float)(_random.NextDouble() - 0.5) * 8000, 
                            Y = (float)(_random.NextDouble() - 0.5) * 8000, 
                            Rot = (float)((_random.NextDouble() - 0.5) * (Math.PI)),
                        };
                        b.color[j] = Color.FromArgb(255, 0, (int)(_random.NextDouble() / 2 * 255), (int)(_random.NextDouble() * 255)).ToArgb();
                        b.cip_[j] = new IdProg { 
                            Id = j, 
                            Program = j * 100 
                        };

                        //Special handling for font rendering, requires precomputing the offset text start vectors
                        var rotation = b.xyar[j].Rot;
                        var cos = Math.Cos(rotation);
                        var sin = Math.Sin(rotation);
                        var (dx, dy) = (-59,-24);//pre rotation
                        var (rdx, rdy) = (cos * dx - sin * dy, sin * dx + cos * dy);//post rotation
                        //then text is written to buffer array
                        _fConsolas.WriteLineTo(b.text.AsSpan(j * _textLen, _textLen), (float)(b.xyar[j].X+ rdx), (float)(b.xyar[j].Y+ rdy), rotation, _random.Next(10000000).ToString().AsSpan());
                    }
                }));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
            Debug.WriteLine($"Tick Time {td}");

            await Task.Delay(10, token).ConfigureAwait(false);
        }
    }

    private void GLSetup()
    {
        //default screen color
        GL.ClearColor(Color.Gray);

        //depth testing so layers dont overlap
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        //blending of alpha textures like the font so transparency works
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var textureCount = 0;

        //Add hacky digits texure
        _tDigits = Texture.CreateFrom(Application.GetResourceStream(new Uri(@"\Resources\digits.png", UriKind.Relative)).Stream);
        _tDigits.Use(textureCount++);

        //Create helper for bitmap font
        _fConsolas = BitmapFont.CreateFrom(
            Application.GetResourceStream(new Uri(@"\Resources\consolas32_.fnt", UriKind.Relative)).Stream,
            file => Application.GetResourceStream(new Uri(@$"\Resources\{file}", UriKind.Relative)).Stream);
        textureCount = _fConsolas.UseBase(textureCount);

        Debug.WriteLine("Used texture units {0}", textureCount);

        //Common indices for a rectangle
        _sRectIndices = BufferStorage.CreateFrom(Extensions.MakeRectIndices());
        //rectangular box vertices
        _sBoxVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(_boxWidth, _boxHeight));
        //square container vertices
        _sContainerVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(_boxWidth, _boxWidth));
        //just some dummy vertices for text renderer
        _sUnitRectVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(1, 1));
        //size of the hacky digit to render
        _sDigitVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(18, 32));

        //box/container data buffers
        _dXYAngle = BufferData.Create(_count * Unsafe.SizeOf<PosRot>(), BufferUsageHint.StreamDraw);
        _dColor = BufferData.Create(_count * Unsafe.SizeOf<int>(), BufferUsageHint.StreamDraw);
        _dIdProg = BufferData.Create(_count * Unsafe.SizeOf<IdProg>(), BufferUsageHint.StreamDraw);

        //Create 'fixed' size buffer for text
        _dXYAngleBMChar = BitmapFont.CreateBufferAligned(_count, _textLen, BufferUsageHint.DynamicDraw);

        //Triple buffer for data writing
        _tTriple = new(() => (new PosRot[_count], new int[_count], new IdProg[_count], new BitmapFont.BFChar[BitmapFont.InstanceCount(_dXYAngleBMChar)]));//will be streamed to _d*

        //a single instance data for render pipeline
        _dUniform = BufferData.CreateFrom(ref _uniform, BufferUsageHint.StreamDraw);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _dUniform);

        //render Input definitions
        _vBox = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sBoxVertices,         2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngle,             3, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dColor,               4, VertexAttribType.UnsignedByte, divisor: 1, normalize:true),
        ]);
        _vContainer = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sContainerVertices,   2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngle,             3, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dColor,               4, VertexAttribType.UnsignedByte, divisor: 1, normalize:true),
        ]);
        _vDigits = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sDigitVertices,       2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngle,             3, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dIdProg,                  2, VertexAttribType.UnsignedInt,  divisor: 1),
        ]);
        _vText = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sUnitRectVertices,    2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleBMChar,                4, VertexAttribType.Float,        divisor: 1),
        ]);

        //shader program definitions
        _pRect = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\rect.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\rect.frag", UriKind.Relative)).Stream),
        ]);

        _pDigits = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\digit.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\digit.frag", UriKind.Relative)).Stream),
        ]);

        _pText = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\text.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\text.frag", UriKind.Relative)).Stream),
        ]);
    }

    public void OnRender(TimeSpan t)
    {
        _fpsCounter.PushEvent();
        FPS = _fpsCounter.Frequency;

        var sw = Stopwatch.GetTimestamp();

        if (!_tTriple.IsStale)//if new data arrived
        {
            using var r = _tTriple.Read();//read last buffer from triple buffer
            var b = r.Value;

            //copy using orphaning
            _dXYAngle.Recreate(b.xyar);
            _dColor.Recreate(b.color);
            _dIdProg.Recreate(b.cip_);
            _dXYAngleBMChar.Recreate(b.text);

            _invalidated = true;//and invalidate
        }

        if (!_invalidated)//but actually render when invalidated otherwise keep same frame
            return;

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);//clear screen

        if (_uniform.CameraScale >= 0.0625)//draw boxes
        {
            _pRect.Use();
        
            //rect boxes
            _vBox.Use();
            _uniform.InstanceBase = _boxBase;
            _uniform.InstanceCount = _count;
            _dUniform.Recreate(ref _uniform);
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
                default, _boxes, _uniform.InstanceBase);
        
            //square containers
            _vContainer.Use();
            _uniform.InstanceBase = _containerBase;
            _uniform.InstanceCount = _count;
            _dUniform.Recreate(ref _uniform);
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
                default, _containers, _uniform.InstanceBase);
        }
        
        if (_uniform.CameraScale >= 0.25)//draw Id/Prog
        {
            _vDigits.Use();
            _pDigits.Use();
            _uniform.InstanceBase = _base;
            _uniform.InstanceCount = _count;

            _uniform.DigitIndex = 0;
            const int idDigits = 4;
            for (int i = 0; i < idDigits; i++)//foreach id digit
            {
                _uniform.DigitDiv = (int)Math.Pow(10, idDigits - i - 1);
                _uniform.DigitPosition = new(i * _digitWidth - _boxWidth / 2 + _leftMargin, +_digitHeight / 2);
                _dUniform.Recreate(ref _uniform);
                GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
                    default, _count, _uniform.InstanceBase);
            }

            //Prog hack is deprecated now
            //_uniform.DigitIndex = 1;
            //const int progDigits = 6;
            //for (int i = 0; i < progDigits; i++)//foreach prog digit
            //{
            //    _uniform.DigitDiv = (int)Math.Pow(10, progDigits - i - 1);
            //    _uniform.DigitPosition = new(i * _digitWidth - _boxWidth / 2 + _leftMargin, -_digitHeight / 2);
            //    _sUniform.Recreate(ref _uniform);
            //    GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
            //        default, _count, _uniform.InstanceBase);
            //}

            //draw prog using font instead
            _vText.Use();
            _pText.Use();
            _uniform.InstanceBase = _base - _textLen + 1;
            _uniform.InstanceCount = _count * _textLen;
            _dUniform.Recreate(ref _uniform);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, default, BitmapFont.InstanceCount(_dXYAngleBMChar));
        }

        var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
        Debug.WriteLine($"Draw Time {td}");
    }

    internal void OnResize(object sender, SizeChangedEventArgs e)
    {
        _uniform.Resolution = new()
        {
            X = (float)e.NewSize.Width,
            Y = (float)e.NewSize.Height,
        };
        _invalidated = true;//invalidate to force redraw
    }
}
