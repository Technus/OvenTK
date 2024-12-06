using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OvenTK.Lib;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OvenTK.Lib.Storage;
#pragma warning disable CS8618, S1450

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

    private const int _beziers = 1000;

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
    /// <summary>
    /// max amounts of sprites per box/container
    /// </summary>
    private const int _spritesPerInstance = 4;

    [ThreadStatic]
    private static Random? _random;
    private static Random Random => _random ??= new();

    private BufferStorage _sBoxVertices, _sContainerVertices, _sDigitVertices, _sUnitRectVertices, _sRectIndices;
    private BufferData _dUniform, _dXYAngle, _dColor, _dIdProg, _dXYAngleBFChar, _dXYAngleSSSprite, _dXYBezier, _dColorBezier, _dComputeDataIn, _dComputeDataOut;
    private VertexArray _vBox, _vContainer, _vDigits, _vText, _vSprite, _vBezier, _vBezier2, _vPolyline2;
    private Texture _tDigits, _tLines;
    private TextureBuffer _bComputeTexIn, _bComputeTexOut, _bColorBezier;
    private BitmapFont _fConsolas;
    private SpriteSheet<Sprite> _sSprites;
    private ShaderProgram _pRect, _pDigits, _pText, _pSprite, _pBezier, _pBezier2, _pPolyline2, _pCompute;
    private FrameBuffer _fbLines;
    private RenderBuffer _rbLines;
    private int _fbMain;
    private bool _invalidated = true;
    private Uniform _uniform = new()
    {
        CameraScale = 1,
        InstanceCount = _count,
    };
    private TripleBufferSimple<(PosRot[] xyar, ColorArgb[] color, IdProg[] cip_, BitmapFont.Char[] text, SpriteSheet.Sprite[] sprites, CubicBezierPatch[] beziers, ColorArgb[] bezierColors)> _tTriple;

    /// <summary>
    /// Box/Container position and rotation
    /// </summary>
    private struct PosRot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }
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
        /// Texture resolution
        /// </summary>
        public Vector2 TextureResolution { get; set; }
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

    private struct CubicBezierPatch
    {
        public Vector2 Point1 { get; set; }
        public Vector2 Point2 { get; set; }
        public Vector2 Point3 { get; set; }
        public Vector2 Point4 { get; set; }
    }

    /// <summary>
    /// swizzling to opengl would be then *.bgra
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct ColorArgb
    {
        [FieldOffset(3)]
        private byte a;
        [FieldOffset(2)]
        private byte r;
        [FieldOffset(1)]
        private byte g;
        [FieldOffset(0)]
        private byte b;
        [FieldOffset(0)]
        private int color;

        public int Color { readonly get => color; set => color = value; }

        public static implicit operator int(ColorArgb color) => color.Color;

        public static implicit operator ColorArgb(int color) => new() { Color = color };

        public static implicit operator Color(ColorArgb color) => System.Drawing.Color.FromArgb(color);

        public static implicit operator ColorArgb(Color color) => color.ToArgb();
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
        Task.Run(() => DataWrite().ConfigureAwait(false));
    }

    /// <summary>
    /// Writes some random data to render
    /// </summary>
    /// <param name="cpus"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DataWrite(int cpus = _cpus, CancellationToken token = default)
    {
        if (_count % cpus is not 0 || _beziers % cpus is not 0)
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
                var tasks = Enumerable.Range(0, cpus).Select(i => Task.Run(() =>//split to different CPUs
                {
                    ProcessBoxes();
                    ProcessBeziers();

                    void ProcessBoxes()
                    {
                        var batch = _count / cpus;
                        var start = i * batch;
                        var end = start + batch;
                        for (int j = start; j < end; j++)
                        {
                            b.xyar[j] = new PosRot
                            {
                                X = (float)(Random.NextDouble() - 0.5) * 8000,
                                Y = (float)(Random.NextDouble() - 0.5) * 8000,
                                Angle = (float)((Random.NextDouble() - 0.5) * (Math.PI)),
                            };
                            b.color[j] = Color.FromArgb(255, 0, (int)(Random.NextDouble() / 2 * 255), (int)(Random.NextDouble() * 255)).ToArgb();
                            b.cip_[j] = new IdProg
                            {
                                Id = j,
                                Program = j * 100
                            };

                            //Special handling for font rendering, requires precomputing the offset text start vectors
                            var rotation = b.xyar[j].Angle;
                            var cos = Math.Cos(rotation);
                            var sin = Math.Sin(rotation);
                            var (dx, dy) = (-59, -24);//pre rotation
                            var (rdx, rdy) = (cos * dx - sin * dy, sin * dx + cos * dy);//post rotation
                                                                                        //then text is written to buffer array
                            _fConsolas.WriteTextTo(b.text.AsSpan(j * _textLen, _textLen), (float)(b.xyar[j].X + rdx), (float)(b.xyar[j].Y + rdy), rotation, Random.Next(10000000).ToString().AsSpan());

                            b.sprites[j * _spritesPerInstance] = new()
                            {
                                X = b.xyar[j].X,
                                Y = b.xyar[j].Y,
                                Angle = b.xyar[j].Angle,
                                Id = (float)Random.GetRandom<Sprite>(.5f),
                            };
                        }
                    }
                    void ProcessBeziers()
                    {
                        var batchBezier = _beziers / cpus;
                        var start = i * batchBezier;
                        var end = start + batchBezier;
                        for (int j = start; j < end; j++)
                        {
                            b.bezierColors[j] = Color.FromArgb(255, 0, (int)(Random.NextDouble() / 2 * 255), (int)(Random.NextDouble() * 255));
                            b.beziers[j] = new()
                            {
                                Point2 = new()
                                {
                                    X = (float)(Random.NextDouble() - 0.5) * 8000,
                                    Y = (float)(Random.NextDouble() - 0.5) * 8000,
                                },
                                Point3 = new()
                                {
                                    X = (float)(Random.NextDouble() - 0.5) * 8000,
                                    Y = (float)(Random.NextDouble() - 0.5) * 8000,
                                },
                            };
                            b.beziers[j].Point1 = new()
                            {
                                X = b.beziers[j].Point2.X + (float)(Random.NextDouble() - 0.5) * 2000,
                                Y = b.beziers[j].Point2.Y + (float)(Random.NextDouble() - 0.5) * 2000,
                            };
                            b.beziers[j].Point4 = new()
                            {
                                X = b.beziers[j].Point3.X + (float)(Random.NextDouble() - 0.5) * 2000,
                                Y = b.beziers[j].Point3.Y + (float)(Random.NextDouble() - 0.5) * 2000,
                            };
                        }
                    }
                }));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
            Debug.WriteLine($"Tick Time {td}");

            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }

    private void GLSetup()
    {
        GL.Disable(EnableCap.CullFace);

        //default screen color
        GL.ClearColor(Color.Gray);

        //depth testing so layers/objects dont overlap
        GL.DepthFunc(DepthFunction.Lequal);

        //blending of alpha textures like the font so transparency works
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var textureCount = 0;

        //Add hacky digits texure
        _tDigits = Texture.CreateFrom(Application.GetResourceStream(new Uri(@"\Resources\Fonts\digits.png", UriKind.Relative)).Stream);
        _tDigits.Use(textureCount++);

        //Create helper for bitmap font
        _fConsolas = BitmapFont.CreateFrom(
            Application.GetResourceStream(new Uri(@"\Resources\Fonts\consolas32_.fnt", UriKind.Relative)).Stream,
            file => Application.GetResourceStream(new Uri(@$"\Resources\Fonts\{file}", UriKind.Relative)).Stream);
        textureCount = _fConsolas.UseBase(textureCount);

        //Create helper for sprite sheet
        _sSprites = SpriteSheet<Sprite>.CreateFrom(
            sprite => Application.GetResourceStream(new Uri(@$"\Resources\Sprites\{sprite.GetDescription()}", UriKind.Relative)).Stream);
        textureCount = _sSprites.UseBase(textureCount);

        //Compute buffers
        _dComputeDataIn = BufferData.Create(32 * 1 * 1 * 32 * 1 * 1 * Unsafe.SizeOf<float>() * 4, BufferUsageHint.DynamicRead);
        _bComputeTexIn = TextureBuffer.CreateFrom(_dComputeDataIn, SizedInternalFormat.Rgba32f);
        _bComputeTexIn.UseImage(textureCount++, TextureAccess.ReadOnly, SizedInternalFormat.Rgba32f);

        _dComputeDataOut = BufferData.Create(32 * 1 * 1 * 32 * 1 * 1 * Unsafe.SizeOf<float>() * 4, BufferUsageHint.DynamicRead);
        _bComputeTexOut = TextureBuffer.CreateFrom(_dComputeDataOut, SizedInternalFormat.Rgba32f);
        _bComputeTexOut.UseImage(textureCount++, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);

        //line/bezier
        _dXYBezier = BufferData.Create(_beziers * Unsafe.SizeOf<CubicBezierPatch>(), BufferUsageHint.StreamDraw);
        _dColorBezier = BufferData.Create(_beziers * Unsafe.SizeOf<ColorArgb>(), BufferUsageHint.StreamDraw);
        _bColorBezier = TextureBuffer.CreateFrom(_dColorBezier, SizedInternalFormat.Rgba8ui);
        _bColorBezier.Use(textureCount++);

        _tLines = Texture.Create(1, 1, SizedInternalFormat.Rgba8, TextureTarget.Texture2D, Texture.MaxLevel_NoMip);

        Debug.WriteLine("Used texture units {0}", textureCount);
        _rbLines = RenderBuffer.Create(1, 1, RenderbufferStorage.Rgba8, RenderbufferTarget.Renderbuffer);
        //Output buffers for layers
        _fbLines = FrameBuffer.Create([
            FrameBufferAtt.CreateFrom(_rbLines, FramebufferAttachment.ColorAttachment0),
        ]);

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
        _dColor = BufferData.Create(_count * Unsafe.SizeOf<ColorArgb>(), BufferUsageHint.StreamDraw);
        _dIdProg = BufferData.Create(_count * Unsafe.SizeOf<IdProg>(), BufferUsageHint.StreamDraw);

        //Create 'fixed' size buffer for text
        _dXYAngleBFChar = BitmapFont.CreateBufferAligned(_count, _textLen, BufferUsageHint.DynamicDraw);
        //Create 'fixed' size buffer for sprites
        _dXYAngleSSSprite = SpriteSheet.CreateBuffer(_count * _spritesPerInstance, BufferUsageHint.DynamicDraw);

        //Triple buffer for data writing
        _tTriple = new(() => (
                new PosRot[_count],
                new ColorArgb[_count],
                new IdProg[_count],
                BitmapFont.MakeArray(_dXYAngleBFChar),
                SpriteSheet.MakeArray(_dXYAngleSSSprite),
                new CubicBezierPatch[_beziers],
                new ColorArgb[_beziers]))
            ;//will be streamed to _d*

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
            VertexArrayAttrib.Create(_dIdProg,              2, VertexAttribType.UnsignedInt,  divisor: 1),
        ]);
        _vText = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sUnitRectVertices,    2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleBFChar,       4, VertexAttribType.Float,        divisor: 1),
        ]);
        _vSprite = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sUnitRectVertices,    2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleSSSprite,     4, VertexAttribType.Float,        divisor: 1),
        ]);
        _vBezier = VertexArray.Create([
            VertexArrayAttrib.Create(_dXYBezier,            2, VertexAttribType.Float),
        ]);
        _vBezier2 = VertexArray.Create([
            VertexArrayAttrib.Create(_dXYBezier,            2, VertexAttribType.Float),
        ]);
        _vPolyline2 = VertexArray.Create([
            VertexArrayAttrib.Create(_dXYBezier,            2, VertexAttribType.Float),
        ]);

        //shader program definitions
        _pRect = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\rect.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\rect.frag", UriKind.Relative)).Stream),
        ]);
        _pDigits = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\digit.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\digit.frag", UriKind.Relative)).Stream),
        ]);
        _pText = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\text.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\text.frag", UriKind.Relative)).Stream),
        ]);
        _pSprite = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\sprite.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\sprite.frag", UriKind.Relative)).Stream),
        ]);
        _pBezier = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.TessControlShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier.tesc", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.TessEvaluationShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier.tese", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier.frag", UriKind.Relative)).Stream),
        ]);
        _pBezier2 = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier2.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.GeometryShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier2.geom", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\bezier2.frag", UriKind.Relative)).Stream),
        ]);
        _pPolyline2 = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\polyline2.vert", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.GeometryShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\polyline.geom", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\polyline2.frag", UriKind.Relative)).Stream),
        ]);
        _pCompute = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.ComputeShader, Application.GetResourceStream(new Uri(@"\Resources\Shaders\test.comp", UriKind.Relative)).Stream),
        ]);

        //a single instance data for render pipeline
        _dUniform = BufferData.CreateFrom(ref _uniform, BufferUsageHint.StreamDraw);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _dUniform);

        Compute();
    }

    /// <summary>
    /// Do some demo comppute
    /// </summary>
    public void Compute()
    {
        _pCompute.Use();
        using (var write = _dComputeDataIn.Map<float>(BufferAccess.WriteOnly))
        {
            write.Span[4] = Random.Next(2138);
            write.Span[5] = Random.Next(2138);
            write.Span[6] = Random.Next(2138);
            write.Span[7] = Random.Next(2138);
        }

        GL.DispatchCompute(32, 1, 1);//Usually best with 32 or 64 muliple
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);//good for reading on GPU side?
        
        //using var sync = Sync.Create(SyncCondition.SyncGpuCommandsComplete);
        //sync.WaitClient();

        using var read = _dComputeDataOut.Map<float>(BufferAccess.ReadOnly);
    }

    public void OnRender(TimeSpan t)
    {
        _fbMain = FrameBuffer.CurrentId();

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
            _dXYAngleBFChar.Recreate(b.text);
            _dXYAngleSSSprite.Recreate(b.sprites);
            _dXYBezier.Recreate(b.beziers);
            _dColorBezier.Recreate(b.bezierColors);

            _invalidated = true;//and invalidate
        }

        if (!_invalidated)//but actually render when invalidated otherwise keep same frame
            return;
        _invalidated = false;

        GL.Disable(EnableCap.DepthTest);

        _fbLines.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit);//clear screen

        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        _pBezier2.Use();
        _vBezier2.Use();

        _uniform.InstanceBase = default;
        _uniform.InstanceCount = _beziers;
        _dUniform.Recreate(ref _uniform);

        GL.DrawArrays(PrimitiveType.LinesAdjacency, 0, _beziers * 4);//*4 cause 4 points, since we do not draw same verices over and over it is not instanced rendering

        GL.Clear(ClearBufferMask.DepthBufferBit);//new layer

        _pPolyline2.Use();
        _vPolyline2.Use();
        GL.DrawArrays(PrimitiveType.Lines, 0, _beziers * 2 * 2);//*2 cause 2 points , *2 cause the source buffer is lineadjencies so 2x longer

        _fbLines.Use(FramebufferTarget.ReadFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbMain);
        GL.BlitFramebuffer(
            0, 0, (int)_uniform.Resolution.X, (int)_uniform.Resolution.Y, 
            0, 0, (int)_uniform.Resolution.X, (int)_uniform.Resolution.Y, 
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbMain);
        GL.Clear(ClearBufferMask.DepthBufferBit);//clear screen
        GL.Enable(EnableCap.DepthTest);

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
            _uniform.TextureResolution = new(16, 1);
            _uniform.InstanceBase = _base;
            _uniform.InstanceCount = _count;

            _uniform.DigitIndex = 0;
            const int idDigits = 4;
            for (int i = 0; i < idDigits; i++)//foreach id digit
            {
                _uniform.DigitDiv = (int)Math.Pow(10, idDigits - i - 1);
                _uniform.DigitPosition = new(i * _digitWidth - _boxWidth / 2 + _leftMargin, +_digitHeight / 2);
                _dUniform.Recreate(ref _uniform);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, default, _count);
            }

            //draw prog using font instead
            _vText.Use();
            _pText.Use();
            _uniform.TextureResolution = _fConsolas.GetPageResolution();
            _uniform.InstanceBase = _base - _textLen + 1;
            _uniform.InstanceCount = _count * _textLen;
            _dUniform.Recreate(ref _uniform);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, default, BitmapFont.InstanceCount(_dXYAngleBFChar));
        }

        GL.Disable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.DepthBufferBit);//new layer

        _vSprite.Use();
        _pSprite.Use();
        _uniform.TextureResolution = _sSprites.GetResolution();
        _uniform.InstanceBase = _base - _spritesPerInstance + 1;
        _uniform.InstanceCount = _count * _spritesPerInstance;
        _dUniform.Recreate(ref _uniform);
        GL.DrawElementsInstanced(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, default, SpriteSheet.InstanceCount(_dXYAngleSSSprite));

        var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
        Debug.WriteLine($"Draw Time {td}");
    }

    internal void OnResize(object sender, SizeChangedEventArgs e)
    {
        var x = Math.Max(1,(int)e.NewSize.Width);
        var y = Math.Max(1,(int)e.NewSize.Height);
        _uniform.Resolution = new()
        {
            X = x,
            Y = y,
        };
        Dispatcher.Invoke(() =>
        {
            Interlocked.Exchange(ref _tLines, Texture.Create(x, y, SizedInternalFormat.Rgba8, TextureTarget.Texture2D, Texture.MaxLevel_NoMip)).Dispose();
            Interlocked.Exchange(ref _rbLines, RenderBuffer.Create(x, y, RenderbufferStorage.Rgba8, RenderbufferTarget.Renderbuffer)).Dispose();
            Interlocked.Exchange(ref _fbLines, FrameBuffer.Create([
                FrameBufferAtt.CreateFrom(_rbLines, FramebufferAttachment.ColorAttachment0),
            ])).Dispose();
            if (_fbLines.CheckStatus() is not FramebufferStatus.FramebufferComplete)
                throw new InvalidOperationException();
        });
        _invalidated = true;//invalidate to force redraw
    }
}
