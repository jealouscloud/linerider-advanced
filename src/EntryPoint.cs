using System;
using System.IO;
using System.Reflection;
namespace linerider
{
    public static class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
#if DEBUG
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "debug")
                {
                    Program.IsDebugged = true;
                }
            }
#endif
            Program.Run();
        }
    }
}
