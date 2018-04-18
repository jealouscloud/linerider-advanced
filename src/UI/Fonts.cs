using System;
using Gwen;

namespace linerider.UI
{
    public class Fonts : IDisposable
    {
        public readonly Font Default;
        public readonly Font DefaultBold;
        public Fonts(Font defaultf, Font boldf)
        {
            Default = defaultf;
            DefaultBold = boldf;
        }
        public void Dispose()
        {
            Default.Dispose();
            DefaultBold.Dispose();
        }
    }
}
