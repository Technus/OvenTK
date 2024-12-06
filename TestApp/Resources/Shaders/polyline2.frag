#version 460 compatibility

const vec4 LineColor = vec4(0,0,1,1);

layout(location = 0) in vec4 gColor;//color from polyline2.geom

void main()
{
    gl_FragColor = gColor;//output to screen
}
