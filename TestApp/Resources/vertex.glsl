#version 460 compatibility
//Each group (in,out, uniform, uniform sampler2d, etc.) has own location layout

layout (location = 0) in vec2 aVertice; //Attribs
layout (location = 1) in vec4 aInstancePosition;
layout (location = 2) in vec4 aData;

layout (location = 0) out vec2 texPos; //to next shader
layout (location = 1) out vec4 color; //to next shader

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
