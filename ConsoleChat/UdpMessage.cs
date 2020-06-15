using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleChat
{
   
    public class UdpMessage
    {
        private byte NameLength = 0;
        private byte[] Name = { };

 
        public UdpMessage(string name)
        {
            Name = Encoding.Unicode.GetBytes(name);
            NameLength = (byte)Name.Length;            
        }

        public UdpMessage(byte[] bytes)
        {
            Array.Resize(ref Name, bytes.Length - 1);
            Array.Copy(bytes, 1, Name, 0, bytes.Length - 1);
            NameLength = (byte)Name.Length;
        }

        public bool CheckMessage()
        {
            return (Name.Length == NameLength);
        }

        public string GetName()
        {
            return Encoding.Unicode.GetString(Name);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[NameLength + 1];
            bytes[0] = NameLength;
            Array.Copy(Name, 0, bytes, 1, NameLength);
            return bytes;
        }
    }
}
