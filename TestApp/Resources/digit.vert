#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 aVertice; //Attribs
layout (location = 1) in vec3 aXYAngleRatio;
layout (location = 2) in uvec2 aIdProgram_;

layout (location = 0) out vec2 texPos; //to next shader

layout (binding = 0) uniform Uniform {
    vec2 size;
    vec2 scale;
    vec2 pos;
    vec2 digitPos;
    uint base;
    uint count;
    uint digitDiv;
    uint digitI;
};

#define M_PI 3.1415926535897932384626433832795

vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    float z = gl_InstanceID/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    float w = aXYAngleRatio[2];
    w+=M_PI*1.5;
    w=float(mod(w,M_PI));
    w-=M_PI/2;
    mat2 A = mat2(cos(w), -sin(w),
                  sin(w),  cos(w));
    vec4 egg = vec4((aVertice.xy + digitPos.xy) * A, z, 1.0);
    egg.xy += aXYAngleRatio.xy;
    egg.xy -= pos.xy;
    egg.xy *= viewPort.xy;
    gl_Position = egg;

    uint id = aIdProgram_[digitI];
    id/=digitDiv;
    id%=10;

    uint xFlag = uint(gl_VertexID % 2);
    uint yFlag = uint(gl_VertexID / 2);
    texPos.x = (xFlag+id)/16.0;
    texPos.y = yFlag;
}
