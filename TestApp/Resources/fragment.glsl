#version 460 compatibility

layout(location = 0) in vec2 texPos;//from shader
layout(location = 1) in vec4 color;//to next shader

layout(binding = 0) uniform Uniform{
    vec3 eggNog;
    mat4 projection;
    mat4 view;
};

//samplers sample in range: [0,1],[0,1]
layout(binding = 0) uniform sampler2D diffuseTex;//binds to texture unit

void main()
{
    //vec4 colorzz = texture(diffuseTex, (texPos + 0.5) / 1.0);
    //gl_FragColor = colorzz;
    //gl_FragColor = vec4(eggNog.rgb, 1.0);
    gl_FragColor = color;
}
