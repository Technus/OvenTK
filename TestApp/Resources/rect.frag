#version 460 compatibility

layout(location = 0) in vec4 color;//from shader

layout(binding = 0) uniform Uniform{
    vec2 size;
    vec2 pos;
    vec2 digitPos;
    float scale;
    uint base;
    uint count;
    uint digitDiv;
    uint digitI;
};

void main()
{
    gl_FragColor = color;
}
