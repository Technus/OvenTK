using OpenTK.Graphics.OpenGL4;
using OvenTK.Lib;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

namespace OvenTK.TestApp;

public class MainViewModelOld : DependencyObject
{
    private readonly float[] _vertices =
    [
        0.5f,0.5f,
        0.5f,-0.5f,
        -0.5f,-0.5f,
        -0.5f,0.5f,
    ];
    private readonly byte[] _indices =
    [
        0,1,2,
        0,2,3,
    ];
    private const int _count = 100_000;
    private const int _cpus = 4;
    private readonly TripleBufferSimple<Vector4[][]> _triple = new(() => [new Vector4[_count], new Vector4[_count]]);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EggNog(float egg, float nog, float eggNog)
    {
        public Matrix4 projection = Matrix4.Identity;
        public Matrix4 view = Matrix4.Identity;
        public Vector3 color = new(egg, nog, eggNog);
        public float count = _count;
    }
    private EggNog _uniform = new(0.1f, 0.3f, 0.8f);

    private BufferBase[] _buffers;
    private VertexArray _vao;
    private Texture _texture;
    private ShaderProgram _shader;
    private readonly FrequencyCounter _fpsCounter = new();
    private readonly FrequencyCounter _tpsCounter = new();

    public double FPS
    {
        get => (double)GetValue(FPSProperty);
        set => SetValue(FPSProperty, value);
    }

    // Using a DependencyProperty as the backing store for FPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FPSProperty =
        DependencyProperty.Register("FPS", typeof(double), typeof(MainViewModelOld), new PropertyMetadata(0d));

    public double TPS
    {
        get { return (double)GetValue(TPSProperty); }
        set { SetValue(TPSProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TPSProperty =
        DependencyProperty.Register("TPS", typeof(double), typeof(MainViewModelOld), new PropertyMetadata(0d));

    public MainViewModelOld()
    {
        GLSetup();
        DataWriteMapped();
    }

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

                using var w = _triple.Write();
                var b = w.Buffer;
                var (b0, b1) = (b[0], b[1]);

                var batch = _count / cpus;
                var tasks = Enumerable.Range(0, cpus).Select(i => Task.Factory.StartNew(() =>
                {
                    var random = new Random();
                    var start = i * batch;
                    var end = start + batch;
                    for (int j = start; j < end; j++)
                    {
                        b0[j] = new Vector4((float)(random.NextDouble() - 0.5) * 2, (float)(random.NextDouble() - 0.5) * 2, 0f, 1f);
                        b1[j] = new Vector4((float)random.NextDouble() / 4, (float)random.NextDouble() / 2, (float)random.NextDouble(), (float)random.NextDouble());
                    }
                }));

                await Task.WhenAll(tasks);
            }
            await Task.Delay(10);

            var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
            Debug.WriteLine($"Tick Time {td}");
        }
    }

    [DllImport("msvcrt.dll", SetLastError = false)]
    static extern nint memcpy(nint dest, nint src, int count);

    public unsafe void OnRender(TimeSpan t)
    {
        var sw = Stopwatch.GetTimestamp();

        _fpsCounter.PushEvent();
        FPS = _fpsCounter.Frequency;
        //DataWriteMapped();

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //using var sync = Sync.Create();

        if (!_triple.IsStale)
        {
            using var r = _triple.Read();
            var b = r.Value;
            (_buffers[2] as BufferData)!.Recreate(b[0]);
            (_buffers[4] as BufferData)!.Recreate(b[1]);
        }

        /*
        using (var map = _buffers[2].MapPage<Vector4>(r.Id, 3, BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapInvalidateRangeBit))
        {
            //var span = _positions.AsSpan().Slice(r * _count, _count); //map.Span();
            //span.CopyTo(map.Span());

            fixed (void* src = b0)
            fixed (void* dest = map.Span)
                memcpy((nint)dest, (nint)src, _count);
        }

        using (var map = _buffers[4].MapPage<Vector4>(r.Id, 3, BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapInvalidateRangeBit))
        {
            //var span = _values.AsSpan().Slice(r * _count, _count); //map.Span();
            //span.CopyTo(map.Span());

            fixed (void* src = b1)
            fixed (void* dest = map.Span)
                memcpy((nint)dest, (nint)src, _count);
        }
        */
        GL.DrawElementsInstanced(PrimitiveType.Triangles, _buffers[1].DrawCount, _buffers[1].DrawType, default, _count);
        //sync.WaitClient();

        var td = Extensions.GetElapsedTime(sw, Stopwatch.GetTimestamp());
        Debug.WriteLine($"Draw Time {td}");
    }

    private void GLSetup()
    {
        GL.ClearColor(Color.LightGray);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        _buffers = [
            BufferStorage.CreateFrom(_vertices),
            BufferStorage.CreateFrom(_indices),
            BufferData.Create(_count * Unsafe.SizeOf<Vector4>(), BufferUsageHint.StreamDraw),
            BufferStorage.CreateFrom(ref _uniform),
            BufferData.Create(_count * Unsafe.SizeOf<Vector4>(), BufferUsageHint.StreamDraw),
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
