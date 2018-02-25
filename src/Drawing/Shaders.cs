using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace linerider.Drawing
{
    public static class Shaders
    {
        private static Shader _lineshader = null;
        public static Shader LineShader
        {
            get
            {
                if (!_initialized)
                    Load();
                return _lineshader;
            }
        }
        private static Shader _ridershader = null;
        public static Shader RiderShader
        {
            get
            {
                if (!_initialized)
                    Load();
                return _ridershader;
            }
        }
        private static bool _initialized = false;
        public static void Load()
        {
            if (!_initialized)
            {
                _initialized = true;
                _lineshader = new Shader(
                    GameResources.simline_vert,
                    GameResources.simline_frag);
                    _ridershader = new Shader(
                    GameResources.rider_vert,
                    GameResources.rider_frag);
            }
        }
    }
}
