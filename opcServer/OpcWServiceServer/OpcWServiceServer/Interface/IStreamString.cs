using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcWServiceServer.Interface
{
    public interface IStreamString
    {
        string ReadString();
        int WriteString(string outString);
    }
}
