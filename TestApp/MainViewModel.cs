using OpenTK.Graphics.OpenGL4;
using OvenTK.Lib;
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
    private const int _count = 10_000_000;
    private int _page = 0;
    private readonly Vector4[] _positions = new Vector4[_count*3];
    private readonly Vector4[] _values = new Vector4[_count*3];

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

    public double FPS
    {
        get => (double)GetValue(FPSProperty);
        set => SetValue(FPSProperty, value);
    }

    // Using a DependencyProperty as the backing store for FPS.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FPSProperty =
        DependencyProperty.Register("FPS", typeof(double), typeof(MainViewModel), new PropertyMetadata(0d));

    public MainViewModel()
    {
        //DataSetup();
        GLSetup();
        //DataWriteWorker();
    }

    public void DataWriteMapped()
    {
        var rand = new Random();

        using (var map = _buffers[2].Map<Vector4>(BufferAccess.WriteOnly))
        {
            var span = map.Span();
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = new Vector4((float)(rand.NextDouble() - 0.5) * 2, (float)(rand.NextDouble() - 0.5) * 2, 0f, 1f);
            }
        }


        using (var map = _buffers[4].Map<Vector4>(BufferAccess.WriteOnly))
        {
            var span = map.Span();
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = new Vector4((float)rand.NextDouble() / 4, (float)rand.NextDouble() / 2, (float)rand.NextDouble(), (float)rand.NextDouble());
            }
        }
    }

    public async Task DataWriteWorker(CancellationToken token = default)
    {
        while(!token.IsCancellationRequested)
        {
            var delay = Task.Delay(1000, token);
            var data = Dispatcher.InvokeAsync(DataWriteMapped).Task;
            await Task.WhenAll(delay, data);
        }
    }

    public void OnRender(TimeSpan t)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        _vao.Use();

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _buffers[3]);

        _texture.Use(0);

        GL.DrawElementsInstanced(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, default, _count);

        _fpsCounter.PushFrame();
        FPS = _fpsCounter.FPS;

        DataWriteMapped();
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
            _values[i] = new Vector4((float)rand.NextDouble()/4, (float)rand.NextDouble()/2, (float)rand.NextDouble(), (float)rand.NextDouble());
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

        _texture = Texture.CreateFrom(
            Application.GetResourceStream(new Uri(@"\Resources\tower1.png", UriKind.Relative)).Stream);

        _shader = ShaderProgram.CreateFrom([
            Shader.CreateFrom(ShaderType.VertexShader, Application.GetResourceStream(new Uri(@"\Resources\vertex.glsl", UriKind.Relative)).Stream),
            Shader.CreateFrom(ShaderType.FragmentShader, Application.GetResourceStream(new Uri(@"\Resources\fragment.glsl", UriKind.Relative)).Stream),
        ]);
    }
}
