#version 120
uniform vec4 u_color;
uniform float u_scale;

attribute vec2 in_vertex;
attribute vec2 in_circle;
attribute float in_ratio;
attribute vec4 in_color;
attribute float in_scale;

varying vec2 v_circle;
varying float v_ratio;
varying vec4 v_color;
varying float v_scale;
void main() 
{
    gl_Position = gl_ModelViewProjectionMatrix * vec4(in_vertex,0.0,1.0);
    v_circle = in_circle;
    v_ratio = in_ratio;
    v_scale = in_scale;
    // alpha channel is priority
    // if equal, prefer vertex color
    if (in_color.a >= u_color.a)
        v_color = in_color;
    else
        v_color = u_color;
}