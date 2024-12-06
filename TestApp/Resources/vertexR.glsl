#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 aVertice; //Attribs
layout (location = 1) in vec4 aXYAngleRatio;
layout (location = 2) in vec4 aColor;

layout (location = 0) out vec4 color; //to next shader

layout (binding = 0) uniform Uniform {
    vec2 size;
    vec2 scale;
    vec2 pos;
    vec2 digitPos;
    uint base;
    uint count;
    uint digitDiv;
};

vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    float z = gl_InstanceID/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    vec4 egg = vec4(aVertice.xy, z, 1.0);
    egg.xy += aXYAngleRatio.xy;
    egg.xy -= pos.xy;
    egg.xy *= viewPort.xy;
    gl_Position = egg;

    color.agrb = aColor;
}
