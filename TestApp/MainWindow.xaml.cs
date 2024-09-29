using OpenTK.Graphics.OpenGL4;
using System.Windows;
using System.Drawing;
using OvenTK.Lib;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace OvenTK.TestApp;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        GLWpfControl.Start(new()
        {
            MajorVersion = 3,
            MinorVersion = 0,
        });

        DataSetup();
        GLSetup();

        GLWpfControl.Render += GLWpfControl_Render;
    }
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
    private const int _count = 1000;
    private readonly Matrix4[] _positions = new Matrix4[_count * 2];
    private readonly uint[] _values = new uint[_count];

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct EggNog(float egg, float nog, float eggNog)
    {
        public float egg = egg;
        public float nog = nog;
        public float eggNog = eggNog;
        public Matrix4 projection = Matrix4.Identity;
        public Matrix4 view = Matrix4.Identity;
    }
    private EggNog _uniform = new(0.1f, 0.3f, 0.8f);
    BufferData[] _buffers;
    VertexArray _vao;
    Texture _texture;
    Shader _shader;

    private void DataSetup()
    {
        var rand = new Random();
        for (int i = 0; i < _positions.Length; i++)
        {
            _positions[i] = Matrix4.CreateTranslation((float)rand.NextDouble(), (float)rand.NextDouble(), 0);
        }
        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = (uint)rand.Next(10000);
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
            BufferData.CreateFrom(_values),
        ];

        _vao = VertexArray.Create();

        GL.EnableVertexArrayAttrib(_vao, 0);
        GL.VertexArrayAttribFormat(_vao, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(_vao, 0, 0);
        GL.VertexArrayVertexBuffer(_vao, 0, _buffers[0], default, sizeof(float) * 2);
        GL.VertexArrayElementBuffer(_vao, _buffers[1]);

        GL.EnableVertexArrayAttrib(_vao, 1);
        GL.VertexArrayAttribFormat(_vao, 1, 16, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(_vao, 1, 1);
        GL.VertexArrayVertexBuffer(_vao, 1, _buffers[2], default, sizeof(float) * 16);
        GL.VertexArrayBindingDivisor(_vao, 1, 1);

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _buffers[3]);

        GL.EnableVertexArrayAttrib(_vao, 2);
        GL.VertexArrayAttribFormat(_vao, 2, 1, VertexAttribType.UnsignedInt, false, 0);
        GL.VertexArrayAttribBinding(_vao, 2, 2);
        GL.VertexArrayVertexBuffer(_vao, 2, _buffers[4], default, sizeof(uint));
        GL.VertexArrayBindingDivisor(_vao, 2, 1);

        _shader = Shader.LoadFromText(
          $$"""
      #version 460 compatibility
      //Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

      layout (location = 0) in vec2 aVertice;
      layout (location = 1) in mat4 aInstancePosition;
      layout (location = 2) in uint aData;

      layout (location = 0) out vec2 texPos;

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      void main()
      {
          //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
          //gl_Position = vec4(aVertice, 0, 1.0);
          //gl_Position.x /= 25;
          //gl_Position.y /= 25;
          //gl_Position.x += aInstancePosition.x;
          //gl_Position.y += aInstancePosition.y;
          gl_Position = projection * view * aInstancePosition * vec4(aVertice, 0, 1.0);
          texPos.xy = gl_Position.xy;
      }
      """,
          $$"""
      #version 460 compatibility
      out vec4 FragColor;
      
      layout(location = 0) in vec2 texPos;

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      //samplers sample in range: [0,1],[0,1]
      layout(binding = 0) uniform sampler2D diffuseTex;

      void main()
      {
          vec4 colorzz = texture(diffuseTex, (texPos + 0.5) / 1.0);
          //FragColor = colorzz;
          FragColor = vec4(eggNog.rgb, 1.0);
      }
      """);

        _texture = Texture.CreateFrom(Properties.Resources.tower1);
        _texture.Use(0);
    }

    private void GLWpfControl_Render(TimeSpan obj)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        GL.BindVertexArray(_vao);

        GL.DrawElementsInstanced(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, default, _positions.Length / 2);
    }
}