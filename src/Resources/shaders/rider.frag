#version 120
uniform sampler2D u_bodytex;
uniform sampler2D u_limbtex;
uniform sampler2D u_sledtex;

varying float v_unit;
varying vec4 v_color;
varying vec2 v_texcoord; 

void main()
{
    vec4 color;
    if (v_unit == 0)
        color = vec4(1.0,1.0,1.0,1.0);
    else if (v_unit == 1)
        color = texture2D(u_bodytex, v_texcoord);
    else if (v_unit == 2)
        color = texture2D(u_limbtex, v_texcoord);
    else if (v_unit == 3)
        color = texture2D(u_sledtex, v_texcoord);
    else
        color = vec4(1.0,0.0,0.0,1.0);//invalid, show red
    gl_FragColor = color * v_color;
}