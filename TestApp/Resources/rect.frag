#version 460 compatibility

layout(location = 0) in vec4 color;//color from rect.vert

void main()
{
    gl_FragColor = color;//output to screen
}
