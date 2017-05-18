using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcWServiceServer.Common
{
    public class Utils
    {
        public static void WriteFile(string filePath, string st)
        {
            File.AppendAllText(filePath, "\r\n" + DateTime.Now.ToString() + ", " + st);
        }
    }
}
