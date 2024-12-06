using OpenTK.Graphics.OpenGL4;
using OvenTK.Lib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace OvenTK.TestApp;

public class MainViewModel : DependencyObject
{
    private readonly float[] _vertices =
    [
        0.5f,0.5f,
        0.5f,-0.5f,
        -0.5f,-0.5f,
        -0.5f,0.5f,
    ];
    private readonly ushort[] _indices =
    [
        0,1,2,
        0,2,3,
    ];
    private const int _count = 12_000_000;
    private const int _cpus = 5;
    private const int _pages = 3;
    private TripleBufferCollaborative _triple = new();
    private readonly Vector4[] _positions = new Vector4[_count * _pages];
    private readonly Vector4[] _values = new Vector4[_count * _pages];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EggNog(float egg, float nog, float eggNog)
    {
        public Vector3 color = new(egg, nog, eggNog);
        public Matrix4 projection = Matrix4.Identity;
        public Matrix4 view = Matrix4.Identity;
    }
    private EggNog _uniform = new(0.1f, 0.3f, 0.8f);

    private BufferBase[] _buffers;
    private VertexArray _vao;
    private Texture _texture;
    private ShaderProgram _shader;
    private readonly FpsCounter _fpsCounter = new();
    private readonly FpsCounter _tpsCounter = new();

    public double FPS
    {
        get => (double)GetValue(FPSProperty);
        set => SetValue(FPSProperty, value);
    }

    // Using a DependencyProperty as the backing store for FPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FPSProperty =
        DependencyProperty.Register("FPS", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d));

    public double TPS
    {
        get { return (double)GetValue(TPSProperty); }
        set { SetValue(TPSProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TPSProperty =
        DependencyProperty.Register("TPS", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d));

    public MainViewModel()
    {
        GLSetup();
        DataWriteMapped();
    }

    public async Task DataWriteMapped(int cpus = _cpus, CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            if (_count % cpus is not 0)
                throw new InvalidOperationException();

            _tpsCounter.PushFrame();
            await Dispatcher.BeginInvoke(() => TPS = _tpsCounter.FPS);

            var w = _triple.BeginUpdate();

            var offset = w * _count;
            var batch = _count / cpus;

            var tasks = Enumerable.Range(0, cpus).Select(i => Task.Factory.StartNew(() =>
            {
                var random = new Random();
                var start = offset + i * batch;
                var end = start + batch;
                for (int j = start; j < end; j++)
                {
                    _positions[j] = new Vector4((float)(random.NextDouble() - 0.5) * 2, (float)(random.NextDouble() - 0.5) * 2, 0f, 1f);
                    _values[j] = new Vector4((float)random.NextDouble() / 4, (float)random.NextDouble() / 2, (float)random.NextDouble(), (float)random.NextDouble());
                }
            }));

            await Task.WhenAll(tasks);

            if (_triple.FinishUpdate())
                Debug.WriteLine("CPU TOO FAST");
        }
    }

    public void OnRender(TimeSpan t)
    {

        _fpsCounter.PushFrame();
        FPS = _fpsCounter.FPS;
        //DataWriteMapped();

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //using var sync = Sync.Create();
        var r = _triple.BeginRead();

        using (var map = _buffers[2].MapPage<Vector4>(r, 3, BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit))
        {
            var span = _positions.AsSpan().Slice(r * _count, _count); //map.Span();
            span.CopyTo(map.Span());
        }

        using (var map = _buffers[4].MapPage<Vector4>(r, 3, BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit))
        {
            var span = _values.AsSpan().Slice(r * _count, _count); //map.Span();
            span.CopyTo(map.Span());
        }

        if (_triple.FinishRead())
            Debug.WriteLine("GPU TOO FAST");
        GL.DrawElementsInstancedBaseInstance(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, default, _count, r * _count);
        //sync.WaitClient();
    }

    private void DataSetup()
    {
        var rand = new Random();
        for (int i = 0; i < _count; i++)
        {
            _positions[i] = new Vector4((float)(rand.NextDouble() - 0.5) * 2, (float)(rand.NextDouble() - 0.5) * 2, 0f, 1f);
        }
        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = new Vector4((float)rand.NextDouble() / 4, (float)rand.NextDouble() / 2, (float)rand.NextDouble(), (float)rand.NextDouble());
        }
    }

    private void GLSetup()
    {
        GL.ClearColor(Color.LightGray);

        _buffers = [
            BufferData.CreateFrom(_vertices),
            BufferData.CreateFrom(_indices),
            BufferData.CreateFrom(_positions, BufferUsageHint.DynamicDraw),
            BufferData.CreateFrom(ref _uniform),
            BufferData.CreateFrom(_values, BufferUsageHint.DynamicDraw),
        ];

        _vao = VertexArray.Create(_buffers[1], [
            VertexArrayAttrib.Create(_buffers[0], 2, VertexAttribType.Float, sizeof(float)*2, false),
            VertexArrayAttrib.Create(_buffers[2], 4, VertexAttribType.Float, sizeof(float)*4, false, 1),
            VertexArrayAttrib.Create(_buffers[4], 4, VertexAttribType.Float, sizeof(float)*4, false, 1),
        ]);
        _vao.Use();

        _texture = Texture.CreateFrom(
            Application.GetResourceStream(new Uri(@"\Resources\tower1.png", UriKind.Relative)).Stream);
        _texture.Use(0);

        _shader = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\vertex.glsl", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\fragment.glsl", UriKind.Relative)).Stream),
        ]);
        _shader.Use();

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _buffers[3]);
    }
}
