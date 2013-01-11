using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            ListenForChanges(9001);
        }

        public static void ListenForChanges(int port)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            Console.WriteLine("Listening for asset changes on port {0}.", port);

            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                HandleClient(client);
            }
        }

        static void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);

            while (client.Connected)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;

                Console.WriteLine(line);
            }

            client.Close();
        }
    }
}
