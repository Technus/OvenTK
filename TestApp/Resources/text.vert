#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

#define _charDataSize 4

layout (location = 0) in vec2 aVertice; //Attribs
layout (location = 1) in vec4 aXYAngleChar; //Attribs

layout (location = 0) out vec2 texPos; //to next shader

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

layout(binding = 2) uniform isamplerBuffer fontData;

#define M_PI 3.1415926535897932384626433832795

vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    float z = (gl_InstanceID+base)/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    float w = aXYAngleChar[2];
    w+=M_PI*1.5;
    w=float(mod(w,M_PI));
    w-=M_PI/2;
    mat2 A = mat2(cos(w), -sin(w),
                  sin(w),  cos(w));
    
    int id = int(aXYAngleChar[3]);

    ivec4 texCoordsForChar = texelFetch(fontData,id);
    
    uint xFlag = uint(gl_VertexID % 2);
    uint yFlag = uint(gl_VertexID / 2);
    
    ivec2 aVertice = ivec2(xFlag * texCoordsForChar[2], yFlag * texCoordsForChar[3]);

    texPos.xy = (texCoordsForChar.xy + aVertice.xy)/1024.0;

    vec4 egg = vec4(aVertice.xy * A, z, 1.0);
    egg.xy += aXYAngleChar.xy;
    egg.xy -= pos.xy;
    egg.xy *= viewPort.xy;
    gl_Position = egg;
}
