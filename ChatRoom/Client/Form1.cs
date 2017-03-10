//===========================================
// クライアント側（普通のThread採用）
//===========================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Client
{
    public partial class Form1 : Form
    {
        private int serverport;             //サーバーのPort番号
        private TcpClient clientsocket;     //サーバーとやり取り用のSocket
        private NetworkStream ns;           //NetworkStream
        private StreamReader sr;            
        private StreamWriter sw;
        private Thread connectThread = null;
        private Thread recThread = null;
        private string serveraddress;
        private string clientname;
        private bool connected = false;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            btn_stopconnect.Enabled = false;
            btn_send.Enabled = false;
            radioBtn_public.Checked = true;
            textBox_username.Text = "username";
            textBox_port.Text = "55555";
            textBox_ip.Text = "127.0.0.1";
        }

        protected override void OnClosed(EventArgs e)
        {
            QuitChat();
            if (recThread != null && recThread.IsAlive)
            {
                recThread.Abort();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// サーバーと接続
        /// </summary>
        private void CreateConnection()
        {
            label_show.Text = "Connecting";
            try
            {
                clientsocket = new TcpClient(serveraddress, serverport);
                ns = clientsocket.GetStream();
                sr = new StreamReader(ns, Encoding.Default);
                sw = new StreamWriter(ns, Encoding.Default);
                sw.AutoFlush = true;
                connected = true;
            }
            catch (Exception)
            {
                MessageBox.Show("サーバーに繋がりません！", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                label_show.Text = "Disconnect";
                textBox_ip.Enabled = true;
                textBox_port.Enabled = true;
                textBox_username.Enabled = true;
            }
        }

        /// <summary>
        /// サーバーとやり取りする
        /// </summary>
        /// <returns></returns>
        private bool StoreforServer()
        {
            try
            {
                string command = "CONNECT|" + textBox_username.Text;
                sw.WriteLine(command);

                string serverresponse = sr.ReadLine();
                serverresponse.Trim();

                string[] tokens = serverresponse.Split('|');

                if (tokens[0] == "LISTEN")
                {
                    label_show.Text = "Connected";
                    btn_stopconnect.Enabled = true;
                }
                else
                {
                    MessageBox.Show("この名前は他人とかぶっている", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    btn_connect.Enabled = true;
                    btn_send.Enabled = false;
                    textBox_ip.Enabled = true;
                    textBox_port.Enabled = true;
                    textBox_username.Enabled = true;

                    return false;
                }

                for (int n = 1; n < tokens.Length - 1; n++)
                {
                    onlineUser.Items.Add(tokens[n].Trim(new char[] { '\r', '\n' }));
                }

                Text = clientname + ": Connected";
            }
            catch (Exception)
            {
                MessageBox.Show("Disconnected！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return true;
        }

        /// <summary>
        /// メッセージを受け取り、対応の処理をする
        /// </summary>
        private void ReceiveChat()
        {
            bool alive = true;
            while (alive)
            {
                try
                {
                    string chatter = sr.ReadLine();
                    string time = DateTime.Now.ToLongTimeString();

                    string[] tokens = chatter.Split('|');

                    if (tokens[0] == "CHAT")        //公開メッセージ
                    {
                        allmessage.AppendText(time + "  ");
                        allmessage.AppendText(tokens[1].Trim() + "\r\n");
                        allmessage.AppendText(tokens[2].Trim() + "\r\n");
                    }
                    if (tokens[0] == "PRIV")        //内緒話
                    {
                        allmessage.AppendText(time + "  ");
                        allmessage.AppendText(tokens[1].Trim());
                        allmessage.AppendText("からの内緒話\r\n");
                        allmessage.AppendText(tokens[2] + "\r\n");      
                    }
                    if (tokens[0] == "JOIN")        //誰かの入室
                    {
                        allmessage.AppendText(time + "  ");
                        allmessage.AppendText(tokens[1].Trim());
                        allmessage.AppendText("がRoomに入りました\r\n");
                        string newcomer = tokens[1].Trim(new char[] { '\r', '\n' });
                        onlineUser.Items.Add(newcomer);
                    }
                    if (tokens[0] == "LEAVE")       //誰かの退室
                    {
                        allmessage.AppendText(time + "  ");
                        allmessage.AppendText(tokens[1].Trim());
                        allmessage.AppendText("が退室しました\r\n");

                        string leaver = tokens[1].Trim(new char[] { '\r', '\n' });

                        for (int n = 0; n < onlineUser.Items.Count; n++)
                        {
                            if (onlineUser.Items[n].ToString().CompareTo(leaver) == 0)
                            {
                                onlineUser.Items.RemoveAt(n);
                            }
                        }
                    }
                    if (tokens[0] == "QUIT")        //サーバーがDown
                    {
                        CloseAll();
                        alive = false;
                        label_show.Text = "Disconnected";
                        btn_stopconnect.Enabled = false;
                        btn_connect.Enabled = true;
                        btn_send.Enabled = false;
                        textBox_ip.Enabled = true;
                        textBox_port.Enabled = true;
                        textBox_username.Enabled = true;
                        MessageBox.Show("Disconnected!");
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// 退室
        /// </summary>
        private void QuitChat()
        {
            if (connected)
            {
                try
                {
                    string command = "LEAVE|" + clientname;
                    sw.WriteLine(command);
                    CloseAll();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            if (recThread != null && recThread.IsAlive)
            {
                recThread.Abort();
            }
            Text = "ChatRoom";
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            if (textBox_username.Text == "" || textBox_ip.Text == "" || textBox_port.Text == "")
            {
                MessageBox.Show("正確に入力してください！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                clientname = textBox_username.Text;
                serveraddress = textBox_ip.Text;
                serverport = int.Parse(textBox_port.Text);
                textBox_ip.Enabled = false;
                textBox_port.Enabled = false;
                textBox_username.Enabled = false;
            }

            //新しThreadを作り、サーバーとやり取りをする
            connectThread = new Thread(() =>
            {
                CreateConnection();
                if (connected)
                {
                    if(!StoreforServer())
                    {
                        return;
                    }
                    recThread = new Thread(new ThreadStart(ReceiveChat));
                    recThread.Start();

                    btn_send.Enabled = true;
                    //btn_shutdown.Enabled = true;
                    btn_connect.Enabled = false;

                    richTextBox1.Text = "";
                }
            });

            connectThread.Start();
        }

        /// <summary>
        /// 発送Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_send_Click(object sender, EventArgs e)
        {
            try
            {
                if (radioBtn_public.Checked)
                {
                    string pubcommand = "CHAT|" + clientname + "| " + richTextBox1.Text;
                    sw.WriteLine(pubcommand);
                    
                    allmessage.AppendText(DateTime.Now.ToLongTimeString() + "  ");
                    allmessage.AppendText("自分\r\n");
                    allmessage.AppendText(richTextBox1.Text + "\r\n");

                    richTextBox1.Text = "";
                }
                else
                {
                    if (onlineUser.SelectedIndex == -1)
                    {
                        MessageBox.Show("内緒話したい相手を選んでください！", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    if(onlineUser.SelectedItem.ToString() == textBox_username.Text)
                    {
                        MessageBox.Show("相手が選べません！！", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    string destclient = onlineUser.SelectedItem.ToString();
                    string command = "PRIV|" + clientname + "| " + richTextBox1.Text + "|" + destclient;
                    sw.WriteLine(command);
                    
                    allmessage.AppendText(DateTime.Now.ToLongTimeString() + "  ");
                    allmessage.AppendText(destclient + "へのメッセージ\r\n");
                    allmessage.AppendText(richTextBox1.Text + "\r\n");

                    richTextBox1.Text = "";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Connect Lost!", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                CloseAll();
                if (recThread != null && recThread.IsAlive)
                    recThread.Abort();
                connected = false;
                label_show.Text = "Disconnect";
            }
        }

        private void message_KeyPress(object sender, KeyPressEventArgs e)
        {
            //
        }

        /// <summary>
        /// LogOut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_stopconnect_Click(object sender, EventArgs e)
        {
            QuitChat();
            btn_stopconnect.Enabled = false;
            btn_connect.Enabled = true;
            btn_send.Enabled = false;
            textBox_ip.Enabled = true;
            textBox_port.Enabled = true;
            textBox_username.Enabled = true;

            recThread.Abort();
            onlineUser.Items.Clear();
            label_show.Text = "Disconnect";
        }

        private void btn_shutdown_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CloseAll()
        {
            sr.Close();
            sw.Close();
            ns.Close();
            clientsocket.Close();
            connected = false;
        }
    }
}
