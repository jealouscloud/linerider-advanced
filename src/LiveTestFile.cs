using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace linerider
{
    class LiveTestFile
    {
        public static float GetValue(int index)
        {
            while (true)
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines("livetest.txt");
                    return float.Parse(lines[index]);
                }
                catch
                {
                }
            }
        }
        public static int GetValueInt(int index)
        {
            while (true)
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines("livetest.txt");
                    return int.Parse(lines[index]);
                }
                catch
                {
                }
            }
        }
    }
}
