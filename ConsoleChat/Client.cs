using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace ConsoleChat
{
    class Client
    {
        public IPAddress LocalAddress;
        public IPAddress NetBroadcastAddress;
        public string Name;

        private TcpListener Listener;
        private UdpClient UdpReceiver;
        const int UdpPort = 8001;
        const int TcpPort = 8002;
        const int RemoteUdpPort = 8003;
        const int RemoteTcpPort = 8004;

        private List<ConnectedUser> Connected;
        public List<IPAddress> UsersAddress;
        
        public StreamReader HisoryReader;
        public StreamWriter HisoryWriter;

        public DateTime ConnectionTime = DateTime.Now;


        public Client(string name)
        {
            /*try
            {
                HisoryReader = new StreamReader("history.txt", Encoding.Unicode);
                string line;
                while ((line = HisoryReader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
                HisoryReader.Close();
            }
            catch
            {
                Console.WriteLine("No history founded");
            }*/

            Name = name;
            UsersAddress = new List<IPAddress>();
            //LocalAddress = IPAddress.Parse(GetLocalIPAddress());
            LocalAddress = IPAddress.Parse("127.0.0.1");
            byte[] broad = LocalAddress.GetAddressBytes();
            broad[broad.Length - 1] = 255;
            NetBroadcastAddress = IPAddress.Parse("127.0.0.1");// new IPAddress(broad);
            Connected = new List<ConnectedUser>();
            Listener = new TcpListener(LocalAddress, TcpPort);
            UdpReceiver = new UdpClient(UdpPort);
            Console.WriteLine("Your address: {0}\nBroadcast address: {1}", LocalAddress.ToString(), NetBroadcastAddress.ToString());
            HisoryWriter = new StreamWriter("history.txt", false, Encoding.Unicode);
        }

        public void Working()
        {
            try
            {
                Thread receiveThread = new Thread(new ThreadStart(ReceiveUdpMessage));
                receiveThread.Start();
                Thread listenThread = new Thread(new ThreadStart(ListenTcp));
                listenThread.Start();
                SendUdpMessage(Name);
                Console.WriteLine("Wait 5 seconds...");
                Thread.Sleep(5000);
                if (Connected.Count > 0)
                {
                    ConnectedUser user = Connected[0];
                    for (int i = 1; i < Connected.Count; i++)
                    {
                        if (user.UserAge > Connected[i].UserAge)
                            user = Connected[i];
                    }
                    if (user.UserAge < ConnectionTime)
                        user.SendMessage((new TcpMessage(4, "")).GetBytes());
                }
                SendTcpMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SendUdpMessage(string name)
        {
            UdpClient sender = new UdpClient(); 
            IPEndPoint endPoint = new IPEndPoint(NetBroadcastAddress, RemoteUdpPort);
            try
            {
                UdpMessage msg = new UdpMessage(name);
                byte[] data = msg.GetBytes();
                sender.Send(data, data.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }

        private void ReceiveUdpMessage()
        {
            IPEndPoint remoteIp = null;
            try
            {
                while (true)
                {
                    byte[] rcvData = UdpReceiver.Receive(ref remoteIp); // получаем данные     
                    UdpMessage msg = new UdpMessage(rcvData);
                    if (msg.CheckMessage())
                    {
                        string userName = msg.GetName();

                        Console.WriteLine("User {0}[{1}] joined chat", userName, remoteIp.Address.ToString());
                        HisoryWriter.WriteLine("User {0}[{1}] joined chat", userName, remoteIp.Address.ToString());

                        //if (remoteIp.Address.Equals(LocalAddress))
                        //    continue;
                        try                 //новый пользователь в сети
                        {
                            UsersAddress.Add(remoteIp.Address);
                            TcpClient client = new TcpClient();
                            client.Connect(remoteIp.Address.ToString(), RemoteTcpPort);
                            ConnectedUser user = new ConnectedUser(client, this);
                            user.Name = userName;
                            Connected.Add(user);
                            Thread getMessageThread = new Thread(new ThreadStart(user.GetMessage));
                            getMessageThread.Start();
                            user.SendMessage(new TcpMessage(0, Name).GetBytes());
                            Thread.Sleep(100);
                            user.SendMessage(new TcpMessage(ConnectionTime.Day, ConnectionTime.Hour, ConnectionTime.Minute, ConnectionTime.Second).GetBytes());
                            
                            //отправка сообщения с указанием имени новому пользователю
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Closed UDP Receiver");
            }
            finally
            {
                if (UdpReceiver != null)
                    UdpReceiver.Close();
            }
        }

        private void ListenTcp()
        {
            Listener.Start();
            try 
            { 
                while (true)
                {
                    TcpClient client = Listener.AcceptTcpClient();
                    if (!UsersAddress.Contains(((IPEndPoint)client.Client.RemoteEndPoint).Address))
                    {
                        ConnectedUser user = new ConnectedUser(client, this);
                        Connected.Add(user);
                        Thread getMessageThread = new Thread(new ThreadStart(user.GetMessage));
                        getMessageThread.Start();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Closed TCP Listener");
            }
            finally
            {
                if (Listener != null)
                    Listener.Stop();
            }
        }

        private void SendTcpMessage()
        {
            while (true)
            {
                string msg = Console.ReadLine();
                Console.WriteLine("{0:g}:  {1}", DateTime.Now, msg);
                HisoryWriter.WriteLine("{0:g}  -  {1}[{2}] : {3}", DateTime.Now, Name, LocalAddress, msg);
                if (msg != "$0")
                {
                    TcpMessage tcpMessage = new TcpMessage(1, msg);
                    foreach (var user in Connected)
                    {
                        if (user.tcpClient.Connected)
                            user.SendMessage(tcpMessage.GetBytes());
                    }
                }
                else
                {
                    foreach (var user in Connected)
                    {
                        user.CloseConnection();
                    }
                    UdpReceiver.Close();
                    Listener.Stop();
                    HisoryWriter.Close();
                    return;
                }
            }
        }
    

        public void RemoveUser(ConnectedUser user)
        {
            UsersAddress.Remove(IPAddress.Parse(user.RemoteIP));
            Connected.Remove(user);
        }

        private static string GetLocalIPAddress()
        {
            string localIP;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            try
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint Point = socket.LocalEndPoint as IPEndPoint;
                localIP = Point.Address.ToString();
            }
            catch
            {
                IPEndPoint Point = socket.LocalEndPoint as IPEndPoint;
                localIP = Point.Address.ToString();
            }
            return localIP;
        }
    }
}