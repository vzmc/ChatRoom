//===========================================
// サーバー側の処理（async await採用）
//===========================================

using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using ClientSocket;

namespace Server
{
    public partial class Form1 : Form
    {
        public int listenport;              //サーバーlistenport
        private TcpListener listener;       //サーバーlistener
        private List<Client> clients;       //接続してくるClientを格納するList
        private Task listenTask;            //Listen実行用のTask
        private List<Task> clientTaskList;  //各Clientとやり取り用のタスクList
        private bool isCancel = false;

        public Form1()
        {
            InitializeComponent();
            clients = new List<Client>();
            clientTaskList = new List<Task>();
        }

        /// <summary>
        /// 終了時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            StopListen();

            base.OnClosed(e);
        }

        /// <summary>
        /// Listen開始
        /// </summary>
        /// <returns>ListenのTask</returns>
        private async Task StartListening()
        {
            listener = new TcpListener(IPAddress.Any, listenport);
            listener.Start();
            while (!isCancel)
            {
                try
                {
                    TcpClient clientsocket = await listener.AcceptTcpClientAsync();
                    clientTaskList.Add(ServiceClient(clientsocket));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// 対応のClientとやり取りする
        /// </summary>
        /// <param name="tcpClient">対象ClientのSocket</param>
        /// <returns></returns>
        private async Task ServiceClient(TcpClient tcpClient)
        {
            NetworkStream ns = tcpClient.GetStream();
            StreamReader sr = new StreamReader(ns, Encoding.Default);
            Client client = null;

            bool alive = true;

            while (alive && !isCancel)
            {
                string clientcommand = await sr.ReadLineAsync();
                string[] tokens = clientcommand.Split('|');

                //Clientからのメッセージを待ち、メッセージにより、対応の処理を行う
                if (tokens[0] == "CONNECT")     //接続
                {
                    string message = "";
                    EndPoint ep = tcpClient.Client.RemoteEndPoint;
                    client = new Client(tokens[1], ep, tcpClient);

                    //同じ名前がすんでに存在していると、NONAMEを返す、接続不可
                    if (clients.Find(x => x.Name == tokens[1]) != null)
                    {
                        SendToClient(client, "REPETITION");
                        alive = false;
                        break;
                    }

                    //繋いだメッセージを返す
                    message = "LISTEN|" + GetChatterList();
                    SendToClient(client, message);

                    //他のClientにこのClientが入ったメッセージを送信
                    foreach (var c in clients)
                    {
                        SendToClient(c, "JOIN|" + tokens[1]);
                    }

                    //このClientを追加
                    clients.Add(client);
                    clientlist.Items.Add(client.ToString());
                }
                if (tokens[0] == "CHAT")    //公開メッセージ発送
                {
                    foreach (var c in clients)
                    {
                        if (c != client)
                        {
                            SendToClient(c, clientcommand);
                        }
                    }

                    //成功のメッセージを返す
                    SendToClient(client, "SUCCESS");
                }
                if (tokens[0] == "PRIV")    //特定のClientに内緒メッセージ発送
                {
                    string dest = tokens[3];

                    Client destclient = clients.Find(x => x.Name == dest);

                    SendToClient(destclient, clientcommand);
                    SendToClient(client, "SUCCESS");
                }
                if (tokens[0] == "LEAVE")   //サーバーから切り離れる
                {
                    clients.Remove(client);
                    clientlist.Items.Remove(client.ToString());

                    foreach (var c in clients)
                    {
                        SendToClient(c, clientcommand);
                    }

                    alive = false;
                    break;
                }
            }

            sr.Close();
            ns.Close();
            tcpClient.Close();
        }

        /// <summary>
        /// Clientにメッセージを発送
        /// </summary>
        /// <param name="cl">対象になるClient</param>
        /// <param name="message">発送するメッセージ</param>
        private void SendToClient(Client cl, string message)
        {
            try
            {
                StreamWriter sw = new StreamWriter(cl.Sock.GetStream(), Encoding.Default);
                sw.AutoFlush = true;
                sw.WriteLine(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                cl.Sock.Close();
                clients.Remove(cl);
                clientlist.Items.Remove(cl.ToString());
            }
            finally
            {
                //
            }
        }

        /// <summary>
        /// 現在繋いているClientのNameを | 区切りとして繋いて、Stringを返す
        /// </summary>
        /// <returns></returns>
        private string GetChatterList()
        {
            string chatters = "";
            for (int n = 0; n < clients.Count; n++)
            {
                Client cl = clients[n];
                chatters += cl.Name;
                chatters += "|";
            }
            chatters.Trim('|');
            return chatters;
        }

        /// <summary>
        /// Listen開始Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;

            listenport = Convert.ToInt32(textPort.Text);

            //CheckForIllegalCrossThreadCalls = false;

            isCancel = false;
            listenTask = StartListening();
            MessageBox.Show("ListenStart！");
        }

        /// <summary>
        /// Listen終了Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            StopListen();

            MessageBox.Show("ListenStop！");
            button1.Enabled = true;
            button2.Enabled = false;
        }

        /// <summary>
        /// Listenを中止
        /// </summary>
        private void StopListen()
        {
            if (listenTask != null)
            {
                isCancel = true;
            }

            if (clients.Count > 0)
            {
                clients.ForEach(x =>
                    {
                        SendToClient(x, "QUIT");
                    });

                clients.Clear();
                clientlist.Items.Clear();
            }

            if (listener != null)
            {
                listener.Stop();
            }
        }
    }
}
