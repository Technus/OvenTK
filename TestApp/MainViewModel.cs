using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OvenTK.Lib;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;

namespace OvenTK.TestApp;

public class MainViewModel : DependencyObject
{
    private readonly FrequencyCounter _fpsCounter = new();
    private readonly FrequencyCounter _tpsCounter = new();

    private const int _cpus = 4;
    private const int _countDefault = 20000;/// must distribute equally across <see cref="_cpus"/>

    private const int _boxWidth = 80;
    private const int _boxHeight = 50;

    private static Random _random = new();

    private BufferStorage _sBoxVertices, _sContainerVertices, _sDigitVertices, _sRectIndices;
    private BufferData _sUniform, _dXYAngleRatio, _dColor, _dIdProgram;
    private VertexArray _vBox, _vContainer, _vDigits;
    private Texture _tDigits;
    private ShaderProgram _pRect, _pDigits;
    private bool _invalidated = true;
    private Uniform _uniform = new()
    {
        CameraScale = new(1f, 1f),
        InstanceCount = _countDefault,
    };
    private TripleBufferSimple<(Vector4[] xyar, int[] color, IdProg[] cip_)> _dTtriple;


    private struct IdProg
    {
        public uint Id { get; set; }
        public uint Program { get; set; }
    }

    private struct Uniform
    {
        public Vector2 Resolution { get; set; }
        public Vector2 CameraScale { get; set; }
        public Vector2 CameraPosition { get; set; }
        public Vector2 DigitPosition { get; set; }
        public int InstanceBase { get; set; }
        public int InstanceCount { get; set; }
        public int DigitDiv { get; set; }
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


    public MainViewModel()
    {
        GLSetup();
        DataWriteMapped();
    }

    public async Task DataWriteMapped(int cpus = _cpus, CancellationToken token = default)
    {
        if (_uniform.InstanceCount % cpus is not 0)
            throw new InvalidOperationException();

        while (!token.IsCancellationRequested)
        {
            var sw = Stopwatch.GetTimestamp();

            //if (_triple.IsStale)
            {
                _tpsCounter.PushEvent();
                await Dispatcher.BeginInvoke(() => TPS = _tpsCounter.Frequency);

                using var w = _dTtriple.Write();
                var b = w.Buffer;

                var batch = _uniform.InstanceCount / cpus;
                var tasks = Enumerable.Range(0, cpus).Select(i => Task.Factory.StartNew(() =>
                {
                    var start = (uint)(i * batch);
                    var end = start + batch;
                    for (uint j = start; j < end; j++)
                    {
                        b.xyar[j] = new Vector4((float)(_random.NextDouble() - 0.5) * 2000, (float)(_random.NextDouble() - 0.5) * 2000, 0f, 0.5f);
                        b.color[j] = Color.FromArgb(255, 0, (int)(_random.NextDouble() / 2 * 255), (int)(_random.NextDouble() * 255)).ToArgb();
                        b.cip_[j] = new IdProg { Id = j, Program = j*10};
                    }
                }));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            var td = FrequencyCounter.GetElapsedTime(sw, Stopwatch.GetTimestamp());
            Debug.WriteLine($"Tick Time {td}");

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private void GLSetup()
    {
        GL.ClearColor(Color.LightGray);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _sRectIndices = BufferStorage.CreateFrom(Extensions.MakeRectIndices());
        _sBoxVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(_boxWidth, _boxHeight));
        _sContainerVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(_boxWidth, 80));
        _sDigitVertices = BufferStorage.CreateFrom(Extensions.MakeRectVertices(18,32));


        _dTtriple = new(() => (new Vector4[_uniform.InstanceCount], new int[_uniform.InstanceCount], new IdProg[_uniform.InstanceCount]));//will be streamed to _d*
        _dXYAngleRatio = BufferData.Create(_uniform.InstanceCount * Unsafe.SizeOf<Vector4>(), BufferUsageHint.StreamDraw);
        _dColor = BufferData.Create(_uniform.InstanceCount * Unsafe.SizeOf<int>(), BufferUsageHint.StreamDraw);
        _dIdProgram = BufferData.Create(_uniform.InstanceCount * Unsafe.SizeOf<IdProg>(), BufferUsageHint.StreamDraw);
        _sUniform = BufferData.CreateFrom(ref _uniform, BufferUsageHint.StreamDraw);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _sUniform);

        _vBox = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sBoxVertices,         2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleRatio,        4, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dColor,               4, VertexAttribType.UnsignedByte, divisor: 1, normalize:true),
        ]);
        _vContainer = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sContainerVertices,   2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleRatio,        4, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dColor,               4, VertexAttribType.UnsignedByte, divisor: 1, normalize:true),
        ]);
        _vDigits = VertexArray.Create(_sRectIndices, [
            VertexArrayAttrib.Create(_sDigitVertices,       2, VertexAttribType.Float),
            VertexArrayAttrib.Create(_dXYAngleRatio,        4, VertexAttribType.Float,        divisor: 1),
            VertexArrayAttrib.Create(_dIdProgram,           2, VertexAttribType.UnsignedInt,  divisor: 1),
        ]);

        _tDigits = Texture.CreateFrom(Application.GetResourceStream(new Uri(@"\Resources\digits.png", UriKind.Relative)).Stream);
        _tDigits.Use(0);

        _pRect = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\vertexR.glsl", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\fragmentR.glsl", UriKind.Relative)).Stream),
        ]);

        _pDigits = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\vertexD.glsl", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\fragmentD.glsl", UriKind.Relative)).Stream),
        ]);
    }

    public void OnRender(TimeSpan t)
    {
        _fpsCounter.PushEvent();
        FPS = _fpsCounter.Frequency;

        if (!_dTtriple.IsStale)
        {
            using var r = _dTtriple.Read();
            var b = r.Value;
            _dXYAngleRatio.Recreate(b.xyar);
            _dColor.Recreate(b.color);
            _dIdProgram.Recreate(b.cip_);
            _invalidated = true;
        }

        if (!_invalidated)
            return;

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


        _vBox.Use();
        _pRect.Use();
        GL.Disable(EnableCap.Blend);

        _uniform.InstanceBase = 0;
        _sUniform.Recreate(ref _uniform);

        GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, 
            default, _uniform.InstanceCount / 2, _uniform.InstanceBase);

        _vDigits.Use();
        _pDigits.Use();
        GL.Enable(EnableCap.Blend);

        const int idDigits = 4;
        for (int i = 0; i < idDigits; i++)//foreach container id digit
        {
            _uniform.DigitDiv = (int)Math.Pow(10,idDigits-i-1);
            _uniform.DigitPosition = new(i*18- _boxWidth/2+4, +_tDigits.Height/2);
            _sUniform.Recreate(ref _uniform);
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
                default, _uniform.InstanceCount / 2, _uniform.InstanceBase);
        }

        return;

        const int progDigits = 6;
        for (int i = 0; i < progDigits; i++)//foreach container id digit
        {

            _sUniform.Recreate(ref _uniform);
            GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType,
                default, _uniform.InstanceCount / 2, _uniform.InstanceBase);
        }

        _vBox.Use();
        _vContainer.Use();

        _uniform.InstanceBase = _uniform.InstanceCount / 2;
        _sUniform.Recreate(ref _uniform);

        GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _sRectIndices.DrawCount, _sRectIndices.DrawType, 
            default, _uniform.InstanceCount / 2, _uniform.InstanceBase);
    }

    internal void OnResize(object sender, SizeChangedEventArgs e)
    {
        _uniform.Resolution = new()
        {
            X = (float)e.NewSize.Width,
            Y = (float)e.NewSize.Height,
        };
        _invalidated = true;
    }
}
