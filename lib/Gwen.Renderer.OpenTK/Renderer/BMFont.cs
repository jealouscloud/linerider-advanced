using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Gwen.Renderer
{
    public class BitmapFont : Gwen.Font
    {
        public BMFont fontdata;
        public Texture texture;
        public BitmapFont(RendererBase renderer, string bmfont, Texture tx)
        : base(renderer)
        {
            fontdata = new BMFont(bmfont);
            this.FaceName = fontdata.Face;
            this.Size = fontdata.FontSize;
            this.texture = tx;
        }
    }
    /// <summary>
    /// BMFont class made for loading files created with bmfont
    /// http://www.angelcode.com/products/bmfont/)
    /// Supports only the ascii character table.
    /// Expects the "text" output .fnt
    /// OpenGL library independent output.
    /// </summary>
    public class BMFont
    {
        private struct FontGlyph
        {
            public int id;
            public float x1;
            public float y1;
            public float x2;
            public float y2;
            public int width;
            public int height;
            public int xoffset;
            public int yoffset;
            public int xadvance;
            public sbyte[] kerning;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vertex
        {
            public int x, y;
            public float u, v;
        }
        ///Just a size class.
        ///Avoiding system.drawing completely.
        public struct Size
        {
            public int Width;
            public int Height;
        }
        public string Face;
        public int FontSize { get; private set; }
        public int LineHeight { get; private set; }
        private FontGlyph _invalid = new FontGlyph();
        private FontGlyph[] _glyphs = new FontGlyph[256];
        internal int _texWidth;
        internal int _texHeight;
        ///Creates a bitmap ASCII font
        public BMFont(string bmfont)
        {
            using (StringReader sr = new StringReader(bmfont))
            {
                string fullline;
                while ((fullline = sr.ReadLine()) != null)
                {
                    var line = fullline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Queue<string> lq = new Queue<string>();
                    foreach (var lword in line)
                    {
                        lq.Enqueue(lword);
                    }
                    string cword = lq.Dequeue();
                    if (cword == "info")
                    {
                        var facesubstr = "face=\"";
                        var faceidx1 = fullline.IndexOf(facesubstr) + facesubstr.Length;
                        var faceidx2 = fullline.IndexOf("\"", faceidx1);

                        var sizesubstr = "size=";
                        var sizeidx1 = fullline.IndexOf(sizesubstr) + sizesubstr.Length;
                        var sizeidx2 = fullline.IndexOf(" ", sizeidx1);

                        Face = fullline.Substring(faceidx1, faceidx2 - faceidx1);
                        FontSize = ParseInt(fullline.Substring(sizeidx1, sizeidx2 - sizeidx1));
                    }
                    else if (cword == "common")
                    {
                        while (lq.Count > 0)
                        {
                            cword = lq.Dequeue();
                            var key = extract_key(cword);
                            var val = extract_value(cword);
                            switch (key)
                            {
                                case "lineHeight":
                                    LineHeight = ParseInt(val);
                                    break;
                                case "scaleW":
                                    _texWidth = ParseInt(val);
                                    break;
                                case "scaleH":
                                    _texHeight = ParseInt(val);
                                    break;
                                case "pages":
                                    if (ParseInt(val) > 1)
                                        throw new Exception("Unsupported BMFont: > 1 pages");
                                    break;
                            }
                        }
                    }
                    else if (cword == "char")
                    {
                        FontGlyph glyph = new FontGlyph();
                        glyph.id = 0;
                        while (lq.Count > 0)
                        {
                            cword = lq.Dequeue();
                            var key = extract_key(cword);
                            var val = int.Parse(extract_value(cword));
                            switch (key)
                            {
                                case "id":
                                    glyph.id = val;
                                    break;
                                case "x":
                                    glyph.x1 = val / (float)_texWidth;
                                    break;
                                case "y":
                                    glyph.y1 = ((float)val / (float)_texHeight);
                                    break;
                                case "width":
                                    glyph.width = val;
                                    glyph.x2 = glyph.x1 + ((float)val / (float)_texWidth);
                                    break;
                                case "height":
                                    glyph.height = val;
                                    glyph.y2 = glyph.y1 + ((float)val / (float)_texHeight);
                                    break;
                                case "xoffset":
                                    glyph.xoffset = val;
                                    break;
                                case "yoffset":
                                    glyph.yoffset = val;
                                    break;
                                case "xadvance":
                                    glyph.xadvance = val;
                                    break;
                            }
                        }
                        if (glyph.id != 0)
                        {
                            if (glyph.id == -1)
                                _invalid = glyph;
                            else
                                _glyphs[glyph.id] = glyph;
                        }
                    }
                    else if (cword == "kerning")
                    {
                        int first = 0;
                        int second = 0;
                        int amount = 0;
                        while (lq.Count > 0)
                        {
                            cword = lq.Dequeue();
                            var key = extract_key(cword);
                            var val = int.Parse(extract_value(cword));
                            switch (key)
                            {
                                case "first":
                                    first = val;
                                    break;
                                case "second":
                                    second = val;
                                    break;
                                case "amount":
                                    amount = val;
                                    break;
                            }
                        }
                        if (first != 0 && second != 0 && amount != 0)
                        {
                            if (_glyphs[first].kerning == null)
                            {
                                _glyphs[first].kerning = new sbyte[256];
                            }
                            var glyph = _glyphs[first];
                            if (Math.Abs(amount) > 127)
                                throw new Exception("Unsupported kerning value");
                            glyph.kerning[second] = (sbyte)amount;
                        }
                    }
                }
            }
            for (int i = 0; i < _glyphs.Length; i++)
            {
                if (_glyphs[i].id == 0)
                {
                    switch ((char)i)
                    {
                        case '\r':
                        case '\n':
                            _glyphs[i] = CreateEmpty(i);
                            break;
                        case '\t':
                            _glyphs[i] = _glyphs[(int)' '];
                            _glyphs[i].xadvance *= 4;
                            break;
                        default:
                            //invalid may not be set
                            //but that's okay.
                            _glyphs[i] = _invalid;
                            break;
                    }
                }
            }
        }
        private FontGlyph CreateEmpty(int id)
        {
            FontGlyph ret = new FontGlyph();
            ret.id = id;
            return ret;
        }
        private int MeasureWordSplit(string input, int start, int px)
        {
            int width = 0;
            if (start == input.Length)
                return 0;
            for (int i = start; i < input.Length; i++)
            {
                var currentchar = input[i];
                var glyph = _glyphs[currentchar];
                int glyphwidth = glyph.xadvance;
                if (i > 0)
                {
                    var prev = _glyphs[input[i - 1]];
                    if (prev.kerning != null &&
                        currentchar < prev.kerning.Length)
                    {
                        var ker = prev.kerning[currentchar];
                        glyphwidth += ker;
                    }
                }
                if (width + glyphwidth >= px)
                {
                    return Math.Max(1, (i - start));
                }
                width += glyphwidth;
            }
            return input.Length - start;
        }
        public List<string> WordWrap(string input, int maxpx)
        {
            // this function isnt 100% for performance but i think thats okay.
            string[] originallines = input.Replace("\r\n", "\n").
            Split('\n');

            List<string> ret = new List<string>();
            foreach (var line in originallines)
            {
                var wordarr = line.Split(' ');
                List<string> words = new List<string>(wordarr.Length);
                foreach (var word in wordarr)
                {
                    if (MeasureText(word).Width >= maxpx)
                    {
                        int index = 0;
                        do
                        {
                            var linewidth = MeasureWordSplit(word, index, maxpx);
                            Debug.Assert(
                                linewidth != 0,
                                "word wrap split line width is zero");
                            words.Add(word.Substring(index, linewidth));
                            index += linewidth;
                        }
                        while (index != word.Length);
                    }
                    else
                    {
                        words.Add(word);
                    }
                }
                string seperator = string.Empty;
                StringBuilder linebuilder = new StringBuilder();
                for (int i = 0; i < words.Count; i++)
                {
                    var word = words[i];
                    var str = linebuilder.ToString();
                    var add = str + seperator + word;
                    if (MeasureText(add).Width < maxpx)
                    {
                        linebuilder.Append(seperator + word);
                    }
                    else
                    {
                        ret.Add(str);
                        linebuilder.Clear();
                        linebuilder.Append(word);
                        continue;
                    }
                    seperator = " ";
                }
                if (linebuilder.Length > 0)
                    ret.Add(linebuilder.ToString());
            }
            return ret;
        }
        private Vertex[] GetGlyphVerts(FontGlyph glyph, int x, int y)
        {
            Vertex[] ret = new Vertex[4];
            int rx = x + glyph.xoffset;
            int ry = y + glyph.yoffset;
            int w = glyph.width;
            int h = glyph.height;
            ret[0] = new Vertex() { x = rx, y = ry, u = glyph.x1, v = glyph.y1 };
            ret[1] = new Vertex() { x = rx + w, y = ry, u = glyph.x2, v = glyph.y1 };
            ret[2] = new Vertex() { x = rx + w, y = ry + h, u = glyph.x2, v = glyph.y2 };
            ret[3] = new Vertex() { x = rx, y = ry + h, u = glyph.x1, v = glyph.y2 };
            return ret;
        }
        private List<Vertex> GenerateTextInternal(int posx, int posy, string text, bool render, out Size size)
        {
            List<Vertex> ret = new List<Vertex>(text.Length * 4);
            int retwidth = 0;
            int retheight = LineHeight;
            int x = posx;
            int y = posy;
            for (int i = 0; i < text.Length; i++)
            {
                int charid = (int)text[i];
                if (charid > _glyphs.Length - 1)
                {
                    charid = 0;//unsupported character
                }
                if (text[i] == '\n')
                {
                    retheight += LineHeight;
                    y += LineHeight;
                    //fuck it, return to carriage.
                    x = posx;
                }
                var glyph = _glyphs[charid];
                if (render)
                {
                    ret.AddRange(GetGlyphVerts(glyph, x, y));
                }
                x += glyph.xadvance;
                //check for kerning
                if (i + 1 < text.Length)
                {
                    if (glyph.kerning != null)
                    {
                        char c = text[i + 1];
                        if (c < glyph.kerning.Length)
                        {
                            var ker = glyph.kerning[c];
                            x += ker;
                        }
                    }
                }
                retwidth = Math.Max(retwidth, x - posx);//in case of newline
            }
            size = new Size() { Width = retwidth, Height = retheight };
            return ret;
        }

        /// <summary>
        /// Generates a list of vertices for use in your opengl library.
        // It's expected for use in an orthogonal projection
        // Using GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
        // with the texture associated with this bmfont loaded.
        // Render as GL_QUADS
        /// </summary>
        public List<Vertex> GenerateText(int posx, int posy, string text)
        {
            Size size;
            return GenerateTextInternal(posx, posy, text, true, out size);
        }
        public Size MeasureText(string text)
        {
            Size ret;
            GenerateTextInternal(0, 0, text, false, out ret);
            return ret;
        }
        private static string extract_value(string cword)
        {
            return cword.Substring(cword.IndexOf('=') + 1);
        }
        private static string extract_key(string cword)
        {
            return cword.Substring(0, cword.IndexOf('='));
        }
        private static int ParseInt(string s)
        {
            return int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
