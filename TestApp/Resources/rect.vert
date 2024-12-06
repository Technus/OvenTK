#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 sVertice; //rectangle vertices
layout (location = 1) in vec3 dXYAngle; //Postion and rotation
layout (location = 2) in vec4 dColor; //color

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

const vec2 viewPort = scale * 2 / size; //*2 due to [-1,1],[-1,1],[-1,1] viewport

// All components are in the range [0,1], including hue.
vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    
    float d = q.x - min(q.w, q.y);
    #define e 1.0e-10
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
    
// All components are in the range [0,1], including hue.
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    const float z = (gl_InstanceID+base)/float(count); //for depth testing

    //the gl_Position (clip-space output position) here must fall between [-1,1],[-1,1],[-1,1],[for normalization, usually: 1]
    
    const float w = dXYAngle[2];//get rotation in radians

    const mat2 A = mat2(cos(w), -sin(w),
                        sin(w),  cos(w));//compute rotation matrix

    color.rgba = dColor.bgra;//copy color to out

    gl_Position = vec4(sVertice.xy * A, z, 1.0);//apply rotation matrix to the relative coords
    gl_Position.xy += dXYAngle.xy;//move to absolute coords
    gl_Position.xy -= pos.xy;//move viewport
    gl_Position.xy *= viewPort.xy;//scale viewport
}
