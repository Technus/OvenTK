﻿#version 460 compatibility

layout (binding = 7) uniform isamplerBuffer dColor; //color

layout (location = 0) out vec4 gColor; //to next shader

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
#define SegmentCount 16
const int Segments = SegmentCount;
const float delta = 1.0 / float(Segments);//nSegments

//const int SegmentsMax = 30; // max_vertices = (SegmentsMax+1)*4;
//const int SegmentsMin = 3; // min number of segments per curve

layout(lines_adjacency) in;
layout(triangle_strip, max_vertices = 4*(SegmentCount+1)) out;

//in VertexData{
//    vec4 mColor;
//    vec4 mVertex;
//} VertexIn[4];

//out VertexData{
//    vec4 mColor;
//    vec2 mTexCoord; // fix before usage
//    vec4 mVertex; // to pass in case if we use fog effect
//} VertexOut;

vec2 toScreenSpace(vec4 vertex)
{
    return vec2( vertex.xy / vertex.w ) * Viewport;
}

float toZValue(vec4 vertex)
{
    return (vertex.z/vertex.w);
}

vec4 toBezier(float delta, int i, vec4 P0, vec4 P1, vec4 P2, vec4 P3)
{
    float t = delta * float(i);
    float t2 = t * t;
    float one_minus_t = 1.0 - t;
    float one_minus_t2 = one_minus_t * one_minus_t;
    return (P0 * one_minus_t2 * one_minus_t + P1 * 3.0 * t * one_minus_t2 + P2 * 3.0 * t2 * one_minus_t + P3 * t2 * t);
}

void drawSegment(vec2 points[4]/*, vec4 colors[4], float zValues[4], vec4 V[4]*/)
{
    const vec2 p0 = points[0];
    const vec2 p1 = points[1];
    const vec2 p2 = points[2];
    const vec2 p3 = points[3];

    /* perform naive culling */
    if( p1.x < -area.x || p1.x > area.x ) return;
    if( p1.y < -area.y || p1.y > area.y ) return;
    if( p2.x < -area.x || p2.x > area.x ) return;
    if( p2.y < -area.y || p2.y > area.y ) return;

    /* determine the direction of each of the 3 segments (previous, current, next) */
    const vec2 v0 = normalize( p1 - p0 );
    const vec2 v1 = normalize( p2 - p1 );
    const vec2 v2 = normalize( p3 - p2 );

    /* determine the normal of each of the 3 segments (previous, current, next) */
    const vec2 n0 = vec2( -v0.y, v0.x );
    const vec2 n1 = vec2( -v1.y, v1.x );
    const vec2 n2 = vec2( -v2.y, v2.x );

    /* determine miter lines by averaging the normals of the 2 segments */
    vec2 miter_a = normalize( n0 + n1 );	// miter at start of current segment
    vec2 miter_b = normalize( n1 + n2 ); // miter at end of current segment

    /* determine the length of the miter by projecting it onto normal and then inverse it */
    float an1 = dot(miter_a, n1);
    float bn1 = dot(miter_b, n2);
    if (an1==0) an1 = 1;
    if (bn1==0) bn1 = 1;
    float length_a = Thickness / an1;
    float length_b = Thickness / bn1;

    /* prevent excessively long miters at sharp corners */
    if( dot( v0, v1 ) < -MiterLimit ) {
        miter_a = n1;
        length_a = Thickness;

        /* close the gap */
        if( dot( v0, n1 ) > 0 ) {
            //VertexOut.mTexCoord = vec2( 0, 0 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( ( p1 + Thickness * n0 ) / Viewport, 0 /*zValues[1]*/, 1.0 );
            EmitVertex();

            //VertexOut.mTexCoord = vec2( 0, 0 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( ( p1 + Thickness * n1 ) / Viewport, 0 /*zValues[1]*/, 1.0 );
            EmitVertex();

            //VertexOut.mTexCoord = vec2( 0, 0.5 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( p1 / Viewport, 0.0, 1.0 );
            EmitVertex();

            EndPrimitive();
        }
        else {
            //VertexOut.mTexCoord = vec2( 0, 1 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( ( p1 - Thickness * n1 ) / Viewport, 0 /*zValues[1]*/, 1.0 );
            EmitVertex();

            //VertexOut.mTexCoord = vec2( 0, 1 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( ( p1 - Thickness * n0 ) / Viewport, 0 /*zValues[1]*/, 1.0 );
            EmitVertex();

            //VertexOut.mTexCoord = vec2( 0, 0.5 );
            //VertexOut.mColor = colors[1];
            //VertexOut.mVertex = V[1];
            gl_Position = vec4( p1 / Viewport, 0 /*zValues[1]*/, 1.0 );
            EmitVertex();

            EndPrimitive();
        }
    }
    if( dot( v1, v2 ) < -MiterLimit ) {
        miter_b = n1;
        length_b = Thickness;
    }
    // generate the triangle strip
    //VertexOut.mTexCoord = vec2( 0, 0 );
    //VertexOut.mColor = colors[1];
    //VertexOut.mVertex = V[1];
    gl_Position = vec4( ( p1 + length_a * miter_a ) / Viewport, 0 /*zValues[1]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 1 );
    //VertexOut.mColor = colors[1];
    //VertexOut.mVertex = V[1];
    gl_Position = vec4( ( p1 - length_a * miter_a ) / Viewport, 0 /*zValues[1]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 0 );
    //VertexOut.mColor = colors[2];
    //VertexOut.mVertex = V[2];
    gl_Position = vec4( ( p2 + length_b * miter_b ) / Viewport, 0 /*zValues[2]*/, 1.0 );
    EmitVertex();

    //VertexOut.mTexCoord = vec2( 0, 1 );
    //VertexOut.mColor = colors[2];
    //VertexOut.mVertex = V[2];
    gl_Position = vec4( ( p2 - length_b * miter_b ) / Viewport, 0 /*zValues[2]*/, 1.0 );
    EmitVertex();

    EndPrimitive();
}

//Takes lines_adjacency expands them to more lines_adjacency elements and transforms them to triangle strip
void main(void)
{
    ivec4 colorTexel = texelFetch(dColor, gl_PrimitiveIDIn);
    gColor.rgba = vec4(colorTexel.bgra)/255.0;

    /* cut segments number if larger or smaller than allowed */
    //int nSegments = (Segments > SegmentsMax)? SegmentsMax : Segments;
    //nSegments = (nSegments < SegmentsMin)? SegmentsMin: nSegments;

    // 4 control points
    vec4 B[4];
    B[0] = gl_in[0].gl_Position;
    B[1] = gl_in[1].gl_Position;
    B[2] = gl_in[2].gl_Position;
    B[3] = gl_in[3].gl_Position;

    // vertex format (will be passed to fragment shader for fogging effect)
    //vec4 V[4];
    //V[0] = VertexIn[0].mVertex;
    //V[1] = VertexIn[1].mVertex;
    //V[2] = VertexIn[2].mVertex;
    //V[3] = VertexIn[3].mVertex;

    // 4 attached colors
    //vec4 C[4];
    //C[0] = VertexIn[0].mColor;
    //C[1] = VertexIn[1].mColor;
    //C[2] = VertexIn[2].mColor;
    //C[3] = VertexIn[3].mColor;

    /* use the points to build a bezier line */
    vec4 Points[4]; // segments of curve in 3d
    vec2 points[4];// segments of curve in 2d
    //vec4 colors[4]; // interpolated colors
    //float zValues[4];
    //int j = 0; // bezier segment index for color interpolation
    for (int i=0; i<=Segments; ++i){//nSegments
        /* first point */
        if (i==0){
            Points[1] = toBezier(delta, i, B[0], B[1], B[2], B[3]);
            Points[2] = toBezier(delta, i+1, B[0], B[1], B[2], B[3]);
            Points[3] = toBezier(delta, i+2, B[0], B[1], B[2], B[3]);
            vec4 dir = normalize(Points[1] - Points[2]);
            Points[0] = Points[1] + dir * 0.1;

            points[0] = toScreenSpace(Points[0]);
            points[1] = toScreenSpace(Points[1]);
            points[2] = toScreenSpace(Points[2]);
            points[3] = toScreenSpace(Points[3]);
        }
        else if (i < Segments-1){//nSegments
            Points[0] = Points[1];
            Points[1] = Points[2];
            Points[2] = Points[3];
            Points[3] = toBezier(delta, i+2, B[0], B[1], B[2], B[3]);

            points[0] = points[1];
            points[1] = points[2];
            points[2] = points[3];
            points[3] = toScreenSpace(Points[3]);
        }
        /* last point */
        else {
            Points[0] = Points[1];
            Points[1] = Points[2];
            Points[2] = Points[3];
            vec4 dir = normalize(Points[2] - Points[1]);
            Points[3] = Points[2] + dir * 0.001;
            
            points[0] = points[1];
            points[1] = points[2];
            points[2] = points[3];
            points[3] = toScreenSpace(Points[3]);
        }

        /* color interpolation: define which bezier segment the point belongs to and then interpolate
          between the two colors of that segment */
        //if (i==0) colors[1] = C[0];
        //else colors[1] = colors[2];
        /* fraction p{i} is located between fraction p{j} and p{j+1} */
        //float pi = float(i+1) / float(nSegments);
        //if (pi >= float(j+1)/3.f) j++;
        //float pj = float(j)/3.f; // 4 bezier points means 3 segments between which points are plotted
        ////float pj1 = float(j+1)/3.f;
        //float a = (pi-pj) / (pj1-pj);
        //colors[2] = mix(C[j], C[j+1], a);

        

        //zValues[0] = toZValue(Points[0]);
        //zValues[1] = toZValue(Points[1]);
        //zValues[2] = toZValue(Points[2]);
        //zValues[3] = toZValue(Points[3]);

        drawSegment(points/*, colors, zValues, V*/);
    }
}