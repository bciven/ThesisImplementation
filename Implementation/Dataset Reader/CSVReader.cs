using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Implementation.Dataset_Reader
{
    public static class CsvReader
    {
        public static double ReadDoubleValue(string line, int i)
        {
            var values = line.Split(new string[] { "," }, StringSplitOptions.None);
            return Convert.ToDouble(values[i]);
        }

        public static int ReadIntValue(string line, int i)
        {
            var values = line.Split(new string[] { "," }, StringSplitOptions.None);
            return Convert.ToInt32(values[i]);
        }

        public static string ReadStringValue(string line, int i)
        {
            var values = line.Split(new string[] { "," }, StringSplitOptions.None);
            return Convert.ToString(values[i]);
        }
    }
}
