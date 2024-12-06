#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0 ) in vec2 sVertice;

layout (binding = 0) uniform Uniform {
    vec2 size;
    vec2 texSize;
    vec2 pos;
    vec2 digitPos;
    float scale;
    uint base;
    uint count;
    uint digitDiv;
    uint digitI;
};

const vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    const float z = ((gl_VertexID/4)+base)/float(count); //for depth testing, there is no instance rendering done here so vertex id is generating it

	gl_Position = vec4(sVertice, 0, 1.0);
    gl_Position.xy -= pos.xy;//move viewport
    gl_Position.xy *= viewPort.xy;//scale viewport
}
