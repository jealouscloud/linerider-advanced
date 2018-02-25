using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace linerider.Drawing
{
    public class Shader : IDisposable
    {
        private int _frag;
        private int _vert;
        private int _program;
        private Dictionary<string, int> _attributes = new Dictionary<string, int>();
        private Dictionary<string, int> _uniforms = new Dictionary<string, int>();
        public Shader(string vert, string frag)
        {
            _program = GL.CreateProgram();
            _frag = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_frag, frag);
            CompileShader(_frag);
            _vert = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vert, vert);
            CompileShader(_vert);

            GL.AttachShader(_program, _frag);
            GL.AttachShader(_program, _vert);
            GL.LinkProgram(_program);
            int linkstatus;
            GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out linkstatus);
            if (linkstatus == 0)
            {
                throw new Exception("Shader program link error: " + GL.GetProgramInfoLog(_program));
            }
            GL.ValidateProgram(_program);
        }
        public int GetAttrib(string attributename)
        {
            int ret;
            if (!_attributes.TryGetValue(attributename, out ret))
            {
                ret = GL.GetAttribLocation(_program, attributename);
                _attributes[attributename] = ret;
            }
            return ret;
        }
        public int GetUniform(string uniformname)
        {
            int ret;
            if (!_uniforms.TryGetValue(uniformname, out ret))
            {
                ret = GL.GetUniformLocation(_program, uniformname);
                _uniforms[uniformname] = ret;
            }
            return ret;
        }
        public void Use()
        {
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
                Debug.WriteLine("Shader Error: "+log);
                throw new Exception("Shader compile error: " + log);
            }
        }
        public void Dispose()
        {
            GL.DeleteShader(_vert);
            GL.DeleteShader(_frag);
            GL.DeleteProgram(_program);
        }
    }
}
