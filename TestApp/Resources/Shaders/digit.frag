#version 460 compatibility

layout(location = 0) in vec2 texurePosition;//texture position from digit.vert

layout(binding = 0) uniform sampler2D fontTexure;//font texture

void main()
{
    gl_FragColor = texture(fontTexure, texurePosition);//output texture to screen
}
