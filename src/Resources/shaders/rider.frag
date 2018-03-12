#version 120
uniform sampler2D u_bodytex;
uniform sampler2D u_bodydeadtex;
uniform sampler2D u_armtex;
uniform sampler2D u_legtex;
uniform sampler2D u_sledtex;
uniform sampler2D u_brokensledtex;

varying float v_unit;
varying vec4 v_color;
varying vec2 v_texcoord; 

void main()
{
    vec4 color;
    if (v_unit == 0.0)
        color = vec4(1.0, 1.0, 1.0, 1.0);
    else if (v_unit == 1.0)
        color = texture2D(u_bodytex, v_texcoord);
    else if (v_unit == 2.0)
        color = texture2D(u_bodydeadtex, v_texcoord);
    else if (v_unit == 3.0)
        color = texture2D(u_armtex, v_texcoord);
    else if (v_unit == 4.0)
        color = texture2D(u_legtex, v_texcoord);
    else if (v_unit == 5.0)
        color = texture2D(u_sledtex, v_texcoord);
    else if (v_unit == 6.0)
        color = texture2D(u_brokensledtex, v_texcoord);
    else
        color = vec4(1.0, 0.0, 0.0, 1.0);//invalid, show red
    gl_FragColor = color * v_color;
}