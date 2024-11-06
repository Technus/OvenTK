#version 460 compatibility

layout(location = 0) in vec2 texPos;//from shader

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

//samplers sample in range: [0,1],[0,1]
layout(binding = 0) uniform sampler2D diffuseTex;//binds to texture unit

void main()
{
    gl_FragColor = texture(diffuseTex, texPos);
}
