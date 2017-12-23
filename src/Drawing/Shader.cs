using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace linerider.Drawing
{
    class Shader
    {
        private int _program;
        public Shader(string vert, string frag)
        {
            _program = GL.CreateProgram();
            int fragshader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragshader, frag);
            CompileShader(fragshader);
            int vertshader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertshader, vert);

            GL.AttachShader(_program, fragshader);
            GL.AttachShader(_program, vertshader);
            GL.LinkProgram(_program);
            int linkstatus;
            GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out linkstatus);
            if (linkstatus == 0)
            {
                throw new Exception("Shader program link error: " + GL.GetProgramInfoLog(_program));
            }
            GL.ValidateProgram(_program);
        }
        public void Use()
        {
        //    GL.Uniform1(0, StaticRenderer.CircleTex);
            GL.UseProgram(_program);
        }
        public void Stop()
        {
            GL.UseProgram(0);
        }
        private void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            int status = 0;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                var log = GL.GetShaderInfoLog(shader);
                throw new Exception("Shader compile error: " + log);
            }
        }
    }
}
