#version 460 compatibility

const vec4 LineColor = vec4(0,1,1,1);

//layout(location = 0) in vec4 color;//color from rect.vert

void main()
{
    gl_FragColor = LineColor;//output to screen
}
