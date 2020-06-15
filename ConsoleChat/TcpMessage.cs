using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleChat
{
    public enum TcpTypes : byte
    {
        Name = 0,
        Message = 1,
        History = 2,
        Date = 3,
        HistoryTask = 4
    };

    class TcpMessage
    {
        private byte Type = 5;
        private byte DataLength = 0;
        private byte[] Data = { };


        public TcpMessage(byte type, string data)
        {
            if (type == 0 || type == 1 || type == 2 || type == 3 || type == 4)
            {
                Data = Encoding.Unicode.GetBytes(data);
                DataLength = (byte)Data.Length;
                Type = type;
            }
        }


        public TcpMessage(int day, int hour, int minute, int second)
        {
            Type = 2;
            Array.Resize(ref Data, 16);
            Array.Copy(BitConverter.GetBytes(day), 0, Data, 0, 4);
            Array.Copy(BitConverter.GetBytes(hour), 0, Data, 4, 4);
            Array.Copy(BitConverter.GetBytes(minute), 0, Data, 8, 4);
            Array.Copy(BitConverter.GetBytes(second), 0, Data, 12, 4);
            DataLength = 16;
        }

        public TcpMessage(byte[] bytes)
        {
            if ((bytes[0] == 0 || bytes[0] == 1 || bytes[0] == 2 || bytes[0] == 3 || bytes[0] == 4) && bytes[1] == bytes.Length - 2)
            {
                Array.Resize(ref Data, bytes.Length - 2);
                Array.Copy(bytes, 2, Data, 0, bytes.Length - 2);
                DataLength = (byte)Data.Length;
                Type = bytes[0];
            }
        }

        public bool CheckMessage(ref byte type)
        {
            type = Type;
            if (Enum.IsDefined(typeof(TcpTypes), Type) && Data.Length == DataLength)
            {
                return true;
            }
            return false;
        }
        
        public string GetData()
        {
            return Encoding.Unicode.GetString(Data);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[DataLength + 2];
            bytes[0] = Type; bytes[1] = DataLength;
            Array.Copy(Data, 0, bytes, 2, DataLength);
            return bytes;
        }

        public DateTime GetDate()
        {
            if (Type == 2)
            {
                DateTime now = DateTime.Now;
                int d = BitConverter.ToInt32(Data, 0);
                int h = BitConverter.ToInt32(Data, 4);
                int m = BitConverter.ToInt32(Data, 8);
                int s = BitConverter.ToInt32(Data, 12);
                return new DateTime(now.Year, now.Month, d, h, m, s);
            }
            else
            {
                return DateTime.Now;
            }
        }
    }
}
