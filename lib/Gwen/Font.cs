using System;

namespace Gwen
{
    /// <summary>
    /// Represents font resource.
    /// </summary>
    public class Font : IDisposable
    {
        /// <summary>
        /// Font face name. Exact meaning depends on renderer.
        /// </summary>
        public string FaceName { get; set; }

        /// <summary>
        /// Font size.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Enables or disables font smoothing (default: disabled).
        /// </summary>
        public bool Smooth { get; set; }

        //public bool Bold { get; set; }
        //public bool DropShadow { get; set; }
        
        /// <summary>
        /// This should be set by the renderer if it tries to use a font where it's null.
        /// </summary>
        public object RendererData { get; set; }

        /// <summary>
        /// This is the real font size, after it's been scaled by Renderer.Scale()
        /// </summary>
        public float RealSize { get; set; }

        private readonly Renderer.RendererBase m_Renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        public Font(Renderer.RendererBase renderer)
            : this(renderer, "Arial", 10)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="renderer">Renderer to use.</param>
        /// <param name="faceName">Face name.</param>
        /// <param name="size">Font size.</param>
        public Font(Renderer.RendererBase renderer, string faceName, int size = 10)
        {
            m_Renderer = renderer;
            FaceName = faceName;
            Size = size;
            Smooth = false;
            //Bold = false;
            //DropShadow = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            m_Renderer.FreeFont(this);
            GC.SuppressFinalize(this);
        }
        
#if DEBUG
        ~Font()
        {
            throw new InvalidOperationException(String.Format("IDisposable object finalized: {0}", GetType()));
            //Debug.Print(String.Format("IDisposable object finalized: {0}", GetType()));
        }
#endif

        /// <summary>
        /// Duplicates font data (except renderer data which must be reinitialized).
        /// </summary>
        /// <returns></returns>
        public Font Copy()
        {
            Font f = new Font(m_Renderer, FaceName);
            f.Size = Size;
            f.RealSize = RealSize;
            f.RendererData = null; // must be reinitialized
            //f.Bold = Bold;
            //f.DropShadow = DropShadow;

            return f;
        }
    }
}
