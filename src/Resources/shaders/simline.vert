#version 120
uniform vec4 u_color;
uniform float u_scale;

in vec2 in_vertex;
in vec2 in_circle;
in float in_ratio;
in vec4 in_color;

varying vec2 v_circle;
varying float v_ratio;
varying vec4 v_color;
void main() 
{
    gl_Position = gl_ModelViewProjectionMatrix * vec4(in_vertex,0.0,1.0);
    v_circle = in_circle;
    v_ratio = in_ratio;
    if (u_color.a < in_color.a)
        v_color = in_color;
    else
        v_color = u_color;
}