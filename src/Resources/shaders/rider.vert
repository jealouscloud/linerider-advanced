#version 120
uniform sampler2D u_bodytex;
uniform sampler2D u_limbtex;
uniform sampler2D u_sledtex;
attribute vec2 in_vertex;
attribute vec2 in_texcoord;
attribute float in_unit;
attribute vec4 in_color;

varying vec4 v_color;
varying vec2 v_texcoord; 
varying float v_unit;
void main() 
{
    gl_Position = gl_ModelViewProjectionMatrix * vec4(in_vertex, 0.0, 1.0);
    v_color = in_color;
    v_texcoord = in_texcoord;
    v_unit = in_unit;
}