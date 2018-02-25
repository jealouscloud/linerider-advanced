#version 120
uniform float u_scale;
uniform int u_knobstate;
uniform bool u_alphachannel;

//basically u/v coordinates to the circle.
varying vec2 v_circle;
//the ratio height/width of the line
varying float v_ratio;
varying vec4 v_color;
const float radius = 0.5;
const float knobradius = 0.4;
float getedge(float rad)
{
    return rad - (rad / u_scale); 
}
void main()
{
    vec2 scaled = vec2(v_circle.x / v_ratio, v_circle.y);
    vec2 circ_center = vec2((1 / v_ratio) - 0.5,0.5);

    float leftdist = distance(scaled,circ_center);
    float rightdist = distance(scaled, vec2(0.5,0.5));
    float edgedist = min(leftdist, rightdist);
    float alpha;
    if (u_knobstate > 0 && edgedist < knobradius)
    {
        float knobedgediff = knobradius - getedge(knobradius);
        float step = (knobradius - edgedist) / knobedgediff;
        const vec3 showncolor = vec3(1,1,1);
        const vec3 lifelockcolor = vec3(1,0,0);
        vec3 knobcolor = showncolor;
        if (u_knobstate == 2)
            knobcolor = lifelockcolor;
        gl_FragColor = vec4(mix(v_color.rgb, knobcolor, min(step,1.0)), 1.0);
        return;

    }
    else if (scaled.x < circ_center.x && scaled.x > 0.5)//between circles
    {
        alpha = 1.0;
    }
    else
    {
        alpha = smoothstep(radius, getedge(radius), edgedist);
    }
    if (alpha == 0.0)
        discard;
    float a = alpha;
    if (u_alphachannel)
        a *= v_color.a;
    gl_FragColor = vec4(v_color.rgb, a);
}