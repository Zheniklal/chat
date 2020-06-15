using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace ConsoleChat
{
    class ConnectedUser
    {
        public TcpClient tcpClient;
        public NetworkStream Stream = null;
        public string Name = "unknown";
        public string RemoteIP;
        public bool Flag = true;
        private Client MainHost = null;
        public DateTime UserAge = DateTime.Now;

        public ConnectedUser(TcpClient client, Client host)
        {
            try
            {
                MainHost = host;
                tcpClient = client;
                Stream = client.GetStream();
                RemoteIP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString();
                MainHost.UsersAddress.Add(IPAddress.Parse(RemoteIP));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Flag = false;
            }
        }


        public void GetMessage()
        {
            try
            {
                while (Flag)
                {

                    byte[] data = new byte[1024]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = Stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (Stream.DataAvailable);
                    data = Encoding.Unicode.GetBytes(builder.ToString());
                    TcpMessage tcpMessage = new TcpMessage(data);
                    byte type = 2;
                    if (tcpMessage.CheckMessage(ref type))
                    {
                        switch (type)
                        {
                            case 0:
                                {
                                    Name = tcpMessage.GetData();
                                    break;
                                }
                            case 1:
                                {
                                    Console.WriteLine("{0:g}  -  {1}[{2}] : {3}", DateTime.Now, Name, RemoteIP, tcpMessage.GetData());
                                    MainHost.HisoryWriter.WriteLine("{0:g}  -  {1}[{2}] : {3}", DateTime.Now, Name, RemoteIP, tcpMessage.GetData());
                                    break;
                                }
                            case 2:
                                {
                                    UserAge = tcpMessage.GetDate();
                                    break;

                                }
                            case 3:
                                {
                                    Console.WriteLine("{0}", tcpMessage.GetData());
                                    break;
                                }
                            case 4:
                                {
                                    MainHost.HisoryWriter.Close();
                                    MainHost.HisoryWriter.Dispose();
                                    try
                                    { 
                                        StreamReader HisoryReader = new StreamReader("history.txt", Encoding.Unicode);
                                        string line;
                                        while ((line = HisoryReader.ReadLine()) != null)
                                        {
                                            SendMessage((new TcpMessage(3, line)).GetBytes());
                                            Thread.Sleep(100);
                                        }
                                        HisoryReader.Close();
                                        
                                    }
                                    catch { }
                                    MainHost.HisoryWriter = new StreamWriter("history.txt", true, Encoding.Unicode);
                                    break;
                                }
                        }
                    }
                }
            }
            catch
            {
                if (tcpClient != null)
                {
                    Console.WriteLine("User {0}[{1}] leaved chat", Name, RemoteIP);
                    MainHost.HisoryWriter.WriteLine("User {0}[{1}] leaved chat", Name, RemoteIP);
                    tcpClient.Close();
                    MainHost.RemoveUser(this);
                }
                return;
            }
        }

        public void SendMessage(byte[] msg)
        {
            if (Stream != null)
            {
                Stream.Write(msg, 0, msg.Length);
            }
        }

        public void CloseConnection()
        {            
            tcpClient.Close();
            tcpClient = null;
            return;
        }
    }
}
