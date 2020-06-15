using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace ConsoleChat
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter name:");
            Client me = new Client(Console.ReadLine());
            me.Working();
            Console.ReadLine();
        }
    }
}
