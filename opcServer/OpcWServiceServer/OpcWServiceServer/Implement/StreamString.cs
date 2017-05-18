using OpcWServiceServer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcWServiceServer.Implement
{
    public class StreamString: IStreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            //int len = 0;
            //len = ioStream.ReadByte();
            //if (len < 0) return null;
            //var mem = new MemoryStream();
            //ioStream.CopyTo(mem);
            //long k= mem.Length;
            //len = Convert.ToInt32(k);
            //byte[] inBuffer = new byte[len];
            //ioStream.Read(inBuffer, 0, len);

            int len = 0;
            len = ioStream.ReadByte() * 256;
            if (len < 0) return null;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);

            //int index =-1;
            //var value= streamEncoding.GetString(inBuffer);
            //if (value.StartsWith("{"))
            //{
            //    index = value.LastIndexOf("}");
            //}
            //else if (value.StartsWith("["))
            //{
            //    index = value.LastIndexOf("]");
            //}
            //if (index == -1) 
            //    return "";
            //value = value.Substring(0, index+1);
            //return value;
        }

        public int WriteString(string outString)
        {
            //byte[] outBuffer = streamEncoding.GetBytes(outString);
            //int len = outBuffer.Length;
            //if (len > UInt16.MaxValue)
            //{
            //    len = (int)UInt16.MaxValue;
            //}
            ////ioStream.WriteByte((byte)(len / 256));
            ////ioStream.WriteByte((byte)(len & 255));
            //ioStream.Write(outBuffer, 0, len);
            //ioStream.Flush();

            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }

}
