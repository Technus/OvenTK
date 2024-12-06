#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 aVertice; //Attribs
layout (location = 1) in vec3 aXYAngleRatio;
layout (location = 2) in vec4 aColor;

layout (location = 0) out vec4 color; //to next shader

layout (binding = 0) uniform Uniform {
    vec2 size;
    vec2 pos;
    vec2 digitPos;
    float scale;
    uint base;
    uint count;
    uint digitDiv;
    uint digitI;
};

vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    float z = (gl_InstanceID+base)/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    
    float w = aXYAngleRatio[2];
    mat2 A = mat2(cos(w), -sin(w),
                  sin(w),  cos(w));
    vec4 egg = vec4(aVertice.xy * A, z, 1.0);
    egg.xy += aXYAngleRatio.xy;
    egg.xy -= pos.xy;
    egg.xy *= viewPort.xy;
    gl_Position = egg;

    color.rgba = aColor.bgra;
}
