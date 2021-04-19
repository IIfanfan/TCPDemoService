using HslCommunication.Serial;
using RP.ScoutRobot.Common;
using RP.ScoutRobot.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPDemo
{
    public partial class Form1 : Form
    {
        //创建一个和客户端通信的套接字

        static Socket SocketWatch = null;

        //定义一个集合，存储客户端信息

        static Dictionary<string, Socket> ClientConnectionItems = new Dictionary<string, Socket> { };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
          
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //端口号（用来监听的）
           
            int port = Convert.ToInt32(txtPort.Text);

            IPAddress ip = IPAddress.Parse(txtIP.Text);

            //将IP地址和端口号绑定到网络节点point上  

            IPEndPoint ipe = new IPEndPoint(ip, port);

            //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）  

            SocketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //监听绑定的网络节点  

            SocketWatch.Bind(ipe);

            //将套接字的监听队列长度限制为20  

            SocketWatch.Listen(20);

            //负责监听客户端的线程:创建一个监听线程  

            Thread threadwatch = new Thread(WatchConnecting);

            //将窗体线程设置为与后台同步，随着主线程结束而结束  

            threadwatch.IsBackground = true;

            //启动线程     

            threadwatch.Start();

            writeListBox("开启监听......");

            
        }
        //监听客户端发来的请求  

        private void WatchConnecting()

        {

            Socket connection = null;

            //持续不断监听客户端发来的请求     

            while (true)

            {

                try

                {

                    connection = SocketWatch.Accept();

                }

                catch (Exception ex)

                {

                    //提示套接字监听异常     

                    Console.WriteLine(ex.Message);

                    break;

                }



                //客户端网络结点号  

                string remoteEndPoint = connection.RemoteEndPoint.ToString();

                //添加客户端信息  

                ClientConnectionItems.Add(remoteEndPoint, connection);

                //显示与客户端连接情况

                writeListBox("\r\n[客户端\"" + remoteEndPoint + "\"建立连接成功！ 客户端数量：" + ClientConnectionItems.Count + "]");



                //获取客户端的IP和端口号  

                IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;

                int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;



                //让客户显示"连接成功的"的信息  

                string sendmsg = "[" + "本地IP：" + clientIP + " 本地端口：" + clientPort.ToString() + " 连接服务端成功！]";

                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendmsg);

                connection.Send(arrSendMsg);



                //创建一个通信线程      

                Thread thread = new Thread(recv);

                //设置为后台线程，随着主线程退出而退出 

                thread.IsBackground = true;

                //启动线程     

                thread.Start(connection);

            }

        }



        /// <summary>

        /// 接收客户端发来的信息，客户端套接字对象

        /// </summary>

        /// <param name="socketclientpara"></param>    

        private void recv(object socketclientpara)

        {

            Socket socketServer = socketclientpara as Socket;



            while (true)

            {

                //创建字节数组接收客户端的数据 返回值length表示接收了多少字节的数据,根据结构体计算出来长度大小

                byte[] arrServerRecMsg = new byte[Marshal.SizeOf(typeof(SocketDateStruct)) + 2];

                //将接收到的信息存入到内存缓冲区，并返回其字节数组的长度    

                try

                {

                    int length = socketServer.Receive(arrServerRecMsg);

                    byte[] dataValue = new byte[Marshal.SizeOf(typeof(SocketDateStruct))]; //数据用于crc
                    Array.Copy(arrServerRecMsg, 0, dataValue, 0, length - 2);

                    byte[] myCRC = SoftCRC16.CRC16(dataValue);

                    if (myCRC[myCRC.Length - 1] == arrServerRecMsg[myCRC.Length - 1] && myCRC[myCRC.Length - 2] == arrServerRecMsg[myCRC.Length - 2])   //验证CRC高位和地位是否相等
                    {
                        SocketDateStruct socketDateStruct = new SocketDateStruct();
                        socketDateStruct = MarshalHelper.BytesToStruct<SocketDateStruct>(arrServerRecMsg);

                        string strStruct = socketDateStruct.devId + ";" + socketDateStruct.Temperature.ToString() + ";" + socketDateStruct.Humidity.ToString() +
                             ";" + socketDateStruct.Distance.ToString() + ";" + socketDateStruct.XAxisAcceleration.ToString() +
                              ";" + socketDateStruct.YAxisAcceleration.ToString() + ";" + socketDateStruct.ZAxisAcceleration.ToString() +
                               ";" + socketDateStruct.PitchAngle.ToString() + ";" + socketDateStruct.RollAngle.ToString() + ";" + socketDateStruct.CourseAngle.ToString();

                        writeListBox("接收到了一条消息：结构体数据->" + strStruct);
                    }
                    else
                    {
                        writeListBox("接收到了一条消息：收到的数据CRC错误");
                    }
                    string strData2 = BitConverter.ToString(arrServerRecMsg);  //只把接收到的数据（0 - length）进行转化
                    writeListBox("接收到了一条消息：原始的byte->" + strData2);

                    //将机器接受到的字节数组转换为人可以读懂的字符串     

                    //string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);
                    string strSRecMsg = "12345";



                    //将发送的字符串信息附加到文本框txtMsg上     

                    writeListBox("\r\n[客户端：" + socketServer.RemoteEndPoint + " 时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "]\r\n" + strSRecMsg);



                    //Thread.Sleep(3000);

                    //socketServer.Send(Encoding.UTF8.GetBytes("[" + socketServer.RemoteEndPoint + "]："+strSRecMsg));

                    //发送客户端数据

                    if (ClientConnectionItems.Count > 0)

                    {

                        foreach (var socketTemp in ClientConnectionItems)

                        {

                            socketTemp.Value.Send(Encoding.UTF8.GetBytes("[" + socketServer.RemoteEndPoint + "]：" + strSRecMsg));

                        }

                    }

                }

                catch (Exception)

                {

                    ClientConnectionItems.Remove(socketServer.RemoteEndPoint.ToString());

                    //提示套接字监听异常  

                    writeListBox("\r\n[客户端\"" + socketServer.RemoteEndPoint + "\"已经中断连接！ 客户端数量：" + ClientConnectionItems.Count + "]");

                    //关闭之前accept出来的和客户端进行通信的套接字 

                    socketServer.Close();

                    break;

                }

            }

        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (ClientConnectionItems.Count > 0)

            {

                foreach (var socketTemp in ClientConnectionItems)

                {

                    socketTemp.Value.Send(Encoding.UTF8.GetBytes("[" + socketTemp.Value.RemoteEndPoint + "]：" + txtSendMessage.Text));

                }

            }
        }
        public void writeListBox(string s)
        {
            this.Invoke((MethodInvoker)delegate {
                if (LoglistBox.Items.Count >= 50)
                    LoglistBox.Items.Clear();
                LoglistBox.Items.Add(s);
            });

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SocketWatch.Close();
            System.Environment.Exit(0);
        }
    }
}
