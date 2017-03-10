using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace ClientSocket
{
    public class Client
    {
        public string Name { get; set; }
        public EndPoint Host { get; set; }
        public TcpClient Sock { get; set; }

        public Client(string Name, EndPoint Host, TcpClient Sock)
        {
            this.Name = Name;
            this.Host = Host;
            this.Sock = Sock;
        }

        public override string ToString()
        {
            return Name + " : " + Host.ToString();
        }

        ~Client()
        {
            Sock.GetStream().Close();
            Sock.Close();
        }
    }
}
