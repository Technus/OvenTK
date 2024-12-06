#version 460 compatibility

layout (binding = 0) uniform Uniform {
    vec2 size;
    vec2 texSize;
    vec2 pos;
    vec2 digitPos;
    float scale;
    uint base;
    uint count;
    uint digitDiv;
    uint digitI;
};

const float Thickness = 4;
const vec2 Viewport = size;
const vec2 area = Viewport * 2;
const float MiterLimit = 0.25;

layout(lines) in;
layout(triangle_strip, max_vertices = 4) out;

//in VertexData{
//    vec4 mColor;
//} VertexIn[4];

//out VertexData{
//    vec2 mTexCoord;
//    vec4 mColor;
//} VertexOut;

vec2 toScreenSpace(vec4 vertex)
{
    return vec2( vertex.xy / vertex.w ) * Viewport;
}

float toZValue(vec4 vertex)
{
    return (vertex.z/vertex.w);
}

void drawSegment(vec2 points[2]/*, vec4 colors[4], float zValues[4]*/)
{
    const vec2 p1 = points[0];
    const vec2 p2 = points[1];

    /* perform naive culling */
    if( p1.x < -area.x || p1.x > area.x ) return;
    if( p1.y < -area.y || p1.y > area.y ) return;
    if( p2.x < -area.x || p2.x > area.x ) return;
    if( p2.y < -area.y || p2.y > area.y ) return;

    /* determine the direction of each of the 3 segments (previous, current, next) */
    const vec2 v1 = normalize( p2 - p1 );

    /* determine the normal of each of the 3 segments (previous, current, next) */
    const vec2 n1 = vec2( -v1.y, v1.x );

    /* determine miter lines by averaging the normals of the 2 segments */
    vec2 miter_a = n1; // miter at start of current segment
    vec2 miter_b = n1; // miter at end of current segment

    /* determine the length of the miter by projecting it onto normal and then inverse it */
    float length_a = Thickness;
    float length_b = Thickness;

    // generate the triangle strip
    //VertexOut.mTexCoord = vec2( 0, 0 );
    //VertexOut.mColor = colors[1];
    gl_Position = vec4( ( p1 + length_a * miter_a) / Viewport, 0 /*zValues[1]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 1 );
    //VertexOut.mColor = colors[1];
    gl_Position = vec4( ( p1 - length_a * miter_a) / Viewport, 0 /*zValues[1]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 0 );
    //VertexOut.mColor = colors[2];
    gl_Position = vec4( ( p2 + length_b * miter_b) / Viewport, 0 /*zValues[2]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 1 );
    //VertexOut.mColor = colors[2];
    gl_Position = vec4( ( p2 - length_b * miter_b) / Viewport, 0 /*zValues[2]*/, 1.0 );
    EmitVertex();

    EndPrimitive();
}

void main(void)
{
    // 4 points
    vec4 Points[2];
    Points[0] = gl_in[0].gl_Position;
    Points[1] = gl_in[1].gl_Position;

    // 4 attached colors
    //vec4 colors[4];
    //colors[0] = VertexIn[0].mColor;
    //colors[1] = VertexIn[1].mColor;
    //colors[2] = VertexIn[2].mColor;
    //colors[3] = VertexIn[3].mColor;

    // screen coords
    vec2 points[2];
    points[0] = toScreenSpace(Points[0]);
    points[1] = toScreenSpace(Points[1]);

    // deepness values
    //float zValues[4];
    //zValues[0] = toZValue(Points[0]);
    //zValues[1] = toZValue(Points[1]);
    //zValues[2] = toZValue(Points[2]);
    //zValues[3] = toZValue(Points[3]);

    drawSegment(points/*, colors, zValues*/);
}