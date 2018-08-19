#version 120
uniform vec4 u_color;
uniform bool u_overlay;
uniform float u_scale;

attribute vec2 in_vertex;
attribute vec2 in_circle;
attribute float in_selectflags;
attribute vec4 in_color;
attribute vec2 in_linesize;

varying vec2 v_circle;
varying vec2 v_linesize;
varying vec4 v_color;
varying float v_selectflags;
void main() 
{
    gl_Position = gl_ModelViewProjectionMatrix * vec4(in_vertex,0.0,1.0);
    v_circle = in_circle;
    v_linesize = in_linesize;
    // alpha channel is priority
    // if equal, prefer vertex color
    if (u_overlay)
        v_color = vec4(0.5,0.5,0.5,0.5);
    else
        v_color = mix(u_color, in_color, float(in_color.a >= u_color.a));
    v_selectflags = in_selectflags;
}