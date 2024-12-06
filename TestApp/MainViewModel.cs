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
    private const int _count = 1_000_000;
    private readonly Vector4[] _positions = new Vector4[_count];
    private readonly Vector4[] _values = new Vector4[_count];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EggNog(float egg, float nog, float eggNog)
    {
        public Vector3 color = new(egg, nog, eggNog);
        public Matrix4 projection = Matrix4.Identity;
        public Matrix4 view = Matrix4.Identity;
    }
    private EggNog _uniform = new(0.1f, 0.3f, 0.8f);

    private BufferData[] _buffers;
    private VertexArray _vao;
    private Texture _texture;
    private Shader _shader;
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
        DataSetup();
        GLSetup();
        DataWrite();
    }

    public async Task DataWrite(CancellationToken token = default)
    {
        while(!token.IsCancellationRequested)
        {
            var delay = Task.Delay(100, token);
            var data = Dispatcher.InvokeAsync(() =>
            {
                DataSetup();

                _buffers[2].Write(_positions);
                _buffers[4].Write(_values);
            }).Task;
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

        GL.DrawElementsInstanced(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, default, _positions.Length);

        _fpsCounter.PushFrame();
        FPS = _fpsCounter.FPS;
    }

    private void DataSetup()
    {
        var rand = new Random();
        for (int i = 0; i < _positions.Length; i++)
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

        _texture = Texture.CreateFrom(Properties.Resources.tower1);

        _shader = Shader.LoadFromText(
          $$"""
      #version 460 compatibility
      //Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

      layout (location = 0) in vec2 aVertice; //Attribs
      layout (location = 1) in vec4 aInstancePosition;
      layout (location = 2) in vec4 aData;

      layout (location = 0) out vec2 texPos;//to next shader
      layout (location = 1) out vec4 color;//to next shader

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      void main()
      {
          //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
          gl_Position = vec4(aVertice, 0, 1.0);
          gl_Position.x /= 25;
          gl_Position.y /= 25;
          gl_Position.x += aInstancePosition.x;
          gl_Position.y += aInstancePosition.y;
          //gl_Position = projection * view * vec4(aVertice.x/25 + aInstancePosition.x, aVertice.y/25 + aInstancePosition.y, 0, 1.0);
          texPos.xy = gl_Position.xy;
          color = aData;
      }
      """,
          $$"""
      #version 460 compatibility
      
      layout (location = 0) in vec2 texPos;//from shader
      layout (location = 1) in vec4 color;//to next shader

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      //samplers sample in range: [0,1],[0,1]
      layout(binding = 0) uniform sampler2D diffuseTex;//binds to texture unit

      void main()
      {
          //vec4 colorzz = texture(diffuseTex, (texPos + 0.5) / 1.0);
          //gl_FragColor = colorzz;
          //gl_FragColor = vec4(eggNog.rgb, 1.0);
          gl_FragColor = color;
      }
      """);
    }
}
