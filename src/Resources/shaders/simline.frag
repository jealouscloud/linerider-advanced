#version 120
uniform float u_scale;
uniform int u_knobstate;
uniform bool u_alphachannel;
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
void main()
{
    v_ratio = v_linesize.x;
    v_scale = v_linesize.y;
    vec2 scaled = vec2(v_circle.x / v_ratio, v_circle.y);
    vec2 circ_center = vec2((1.0 / v_ratio) - 0.5, 0.5);
    float leftdist = distance(scaled,circ_center);
    float rightdist = distance(scaled, vec2(0.5, 0.5));
    float edgedist = min(leftdist, rightdist);
    float alpha;
    if ((u_knobstate > 0 && v_selectflags <= 0.0) && edgedist < knobradius)
    {
        float knobedgediff = knobradius - getedge(knobradius, 0.8);
        float step = (knobradius - edgedist) / knobedgediff;
        const vec3 lifelockcolor = vec3(1.0, 0.0, 0.0);
        vec3 knobcolor = u_knobcolor.rgb;
        if (u_knobstate == 2)
            knobcolor = lifelockcolor;
        gl_FragColor = vec4(mix(v_color.rgb, knobcolor, min(step, 1.0)), 1.0);
        return;
    }
    else if (scaled.x < circ_center.x && scaled.x > 0.5)//between circles
    {
        alpha = 1.0;
    }
    else
    {
        alpha = smoothstep(radius, getedge(radius, 1.0), edgedist);
    }
    if (alpha == 0.0)
        discard;
    float a = alpha;
    if (u_alphachannel)
        a *= v_color.a;
    vec3 color = v_color.rgb;
    if (v_selectflags == 1)
        color = mix(color,vec3(0.5,0.5,0.5), 0.25);
    gl_FragColor = vec4(color, a);
}