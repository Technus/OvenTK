#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 sVertice; //rectangle vertices
layout (location = 1) in vec3 dXYAngle; //Postion and rotation
layout (location = 2) in uvec2 dIdProg; //Id (and programming number)

layout (location = 0) out vec2 texurePosition; //to next shader

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

#define M_PI 3.1415926535897932384626433832795

const vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

void main()
{
    const float z = (gl_InstanceID+base)/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    
    float w = dXYAngle[2];//get rotation in radians, then below ensure text is upright
    w+=M_PI*1.5;
    w=float(mod(w,M_PI));
    w-=M_PI/2;

    const mat2 A = mat2(cos(w), -sin(w),
                        sin(w),  cos(w));//compute rotation matrix

    uint id = dIdProg[digitI];//Pick which number to render
    id/=digitDiv;//get digit from number
    id%=10;//get digit from number

    const uint xFlag = uint(gl_VertexID % 2);//compute in which corner we are
    const uint yFlag = uint(gl_VertexID / 2);//compute in which corner we are
    texurePosition.xy = vec2(xFlag, yFlag);//set texture position output (from 1 row of 16 chars) 
    texurePosition.xy /= texSize;//scale from texture size to normalized

    gl_Position = vec4((sVertice.xy + digitPos.xy) * A, z, 1.0);//apply rotation matrix to the relative coords
    gl_Position.xy += dXYAngle.xy;//move to absolute coords
    gl_Position.xy -= pos.xy;//move viewport
    gl_Position.xy *= viewPort.xy;//scale viewport
}
