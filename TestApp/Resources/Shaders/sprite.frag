#version 460 compatibility

layout(location = 0) in vec2 texurePosition;//texture position from sprite.vert

layout(binding = 3) uniform sampler2D spriteTexure;//sprite texture

void main()
{
    gl_FragColor = texture(spriteTexure, texurePosition);
}
