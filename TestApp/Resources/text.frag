#version 460 compatibility

layout(location = 0) in vec2 texPos;
layout(binding = 1) uniform sampler2D diffuseTex;

void main()
{
    gl_FragColor = texture(diffuseTex, texPos);
    //gl_FragColor = vec4(0,0,1,1);
}
