using System;
using System.Collections.Generic;
namespace linerider.IO.SOL
{
    public class sol_track
    {
        public string filename;
        public string name;
        public List<Amf0Object> data;

        public override string ToString()
        {
            return name;
        }

        public object get_property(string name)
        {
            for (var i = 0; i < data.Count; i++)
            {
                if (data[i].name == name)
                    return data[i].data;
            }
            throw new Exception("No property of the name " + name + " was found.");
        }
    }
}
