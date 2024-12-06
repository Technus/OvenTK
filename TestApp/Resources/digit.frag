﻿#version 460 compatibility

layout(location = 0) in vec2 texPos;
layout(binding = 0) uniform sampler2D diffuseTex;

void main()
{
    gl_FragColor = texture(diffuseTex, texPos);
}
