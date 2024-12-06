#version 460 compatibility

layout(location = 0) in vec4 color;

void main()
{
    gl_FragColor = color;
}
