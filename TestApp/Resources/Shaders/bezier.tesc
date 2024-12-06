#version 460 compatibility

layout( vertices=4 ) out;

const int NumSegments = 8;
const int NumStrips = 1;

void main()
{
	// Pass along the vertex position unmodified
	gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;//could read all in this invocation but can only write to correct one
	// Define the tessellation levels
	gl_TessLevelOuter[0] = float(NumStrips);
	gl_TessLevelOuter[1] = float(NumSegments);
}