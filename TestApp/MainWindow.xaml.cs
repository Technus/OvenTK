using OpenTK.Graphics.OpenGL4;
using System.Windows;
using System.Drawing;
using OvenTK.Lib;
using System.Runtime.InteropServices;

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

#if DEBUG
        Extensions.EnableDebug();
#endif

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
    private const int _count = 10000;
    private readonly Vector4[] _positions = new Vector4[_count];
    private readonly uint[] _values = new uint[_count];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EggNog(float egg, float nog, float eggNog)
    {
        public Vector3 color = new(egg, nog, eggNog);
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
            _positions[i] = new Vector4((float)(rand.NextDouble() - 0.5) * 2, (float)(rand.NextDouble() - 0.5) * 2, 0f, 1f);
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

        _vao = VertexArray.Create(_buffers[1], [
            VertexArrayAttrib.Create(_buffers[0], 2, VertexAttribType.Float, sizeof(float)*2, false),
            VertexArrayAttrib.Create(_buffers[2], 4, VertexAttribType.Float, sizeof(float)*4, false, 1),
            VertexArrayAttrib.Create(_buffers[4], 1, VertexAttribType.UnsignedInt, sizeof(uint), false, 1),
        ]);

        _texture = Texture.CreateFrom(Properties.Resources.tower1);
        _texture.Use(0);

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _buffers[3]);

        _shader = Shader.LoadFromText(
          $$"""
      #version 460 compatibility
      //Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

      layout (location = 0) in vec2 aVertice; //Attribs
      layout (location = 1) in vec4 aInstancePosition;
      layout (location = 2) in uint aData;

      layout (location = 0) out vec2 texPos;//to next shader

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      void main()
      {
          //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
          gl_Position = vec4(aVertice, 0, 1.0);
          gl_Position.x /= 25f;
          gl_Position.y /= 25f;
          gl_Position.x += aInstancePosition.x;
          gl_Position.y += aInstancePosition.y;
          //gl_Position = projection * view * vec4(aVertice.x/25f + aInstancePosition.x, aVertice.y/25f + aInstancePosition.y, 0, 1.0);
          texPos.xy = gl_Position.xy;
      }
      """,
          $$"""
      #version 460 compatibility
      
      layout(location = 0) in vec2 texPos;//from shader

      layout (binding = 0) uniform Uniform {
          vec3 eggNog;
          mat4 projection;
          mat4 view;
      };

      //samplers sample in range: [0,1],[0,1]
      layout(binding = 0) uniform sampler2D diffuseTex;//binds to texture unit

      void main()
      {
          vec4 colorzz = texture(diffuseTex, (texPos + 0.5) / 1.0);
          gl_FragColor = colorzz;
          //gl_FragColor = vec4(eggNog.rgb, 1.0);
      }
      """);
    }

    private void GLWpfControl_Render(TimeSpan obj)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        _vao.Use();

        GL.DrawElementsInstanced(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, default, _positions.Length);
    }
}