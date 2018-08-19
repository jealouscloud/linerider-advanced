#version 120
uniform float u_scale;
uniform int u_knobstate;
uniform bool u_alphachannel;
uniform bool u_overlay;
uniform vec4 u_knobcolor;
//basically u/v coordinates to the circle.
varying vec2 v_circle;
varying vec2 v_linesize;
varying vec4 v_color;
varying float v_selectflags;

float v_scale;
//the ratio height/width of the line
float v_ratio;

const float radius = 0.5;
const float knobradius = 0.4;
float getedge(float rad, float scale)
{
    return rad - (rad / (u_scale * v_scale * scale)); 
}
vec3 getknob(float edgedist)
{
    const vec3 lifelockcolor = vec3(1.0, 0.0, 0.0);
    float knobedgediff = knobradius - getedge(knobradius, 0.8);
    float step = (knobradius - edgedist) / knobedgediff;
    vec3 knobcolor = u_knobcolor.rgb;
    if (u_knobstate == 2)
        knobcolor = lifelockcolor;
    return mix(v_color.rgb, knobcolor, min(step, 1.0));
}
void main()
{
    v_ratio = v_linesize.x;
    v_scale = v_linesize.y;
    vec2 scaled = vec2(v_circle.x / v_ratio, v_circle.y);
    vec2 circ_center = vec2((1.0 / v_ratio) - 0.5, 0.5);
    float leftdist = distance(scaled,circ_center);
    float rightdist = distance(scaled, vec2(0.5, 0.5));
    float edgedist = min(leftdist, rightdist);
    vec4 color = vec4(v_color.rgb, 1.0);
    
    if (!u_overlay && 
        (u_knobstate > 0 && v_selectflags <= 0.0) && edgedist < knobradius)
        color.rgb = getknob(edgedist);

    if (scaled.x >= circ_center.x || scaled.x <= 0.5)
        color.a = smoothstep(radius, getedge(radius, 1.0), edgedist);

    if (u_alphachannel)
        color.a *= v_color.a;
    if (!u_overlay && v_selectflags == 1.0)
        color.rgb = mix(color.rgb,vec3(0.5, 0.5, 0.5), 0.25);
    gl_FragColor = color;
}