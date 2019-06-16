using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace DPC_for_csharp
{
    class Program
    {
        #region 初始化变量
        //地址
        static IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        //母服务器
        static Socket ParentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //套接字服务器组
        static Socket[] SubSocket = new Socket[0];
        //服务器链接数
        static int SocketLink = 0;
        #endregion
        static void Main(string[] args)
        {
            #region 设置空配置
            WriteColor("初始化配置..");
            string Socket_ip = "0.0.0.0";
            int Socket_port = 0;
            int Socket_MaxLink = 0;
            #endregion
            #region 加载配置文件
            //查看文件是否存在
            if (File.Exists("./Setting.config"))
            {
                try
                {
                    //如果存在读取
                    WriteColor("加载配置..");
                    #region 读取配置区
                    Socket_ip = System.IO.File.ReadAllLines(@".\Setting.config")[0];
                    Socket_port = int.Parse(System.IO.File.ReadAllLines(@".\Setting.config")[1]);
                    Socket_MaxLink = int.Parse(System.IO.File.ReadAllLines(@".\Setting.config")[2]);
                    #endregion

                    #region 输出配置区
                    Console.WriteLine("IP:" + Socket_ip);
                    Console.WriteLine("端口:" + Socket_port);
                    Console.WriteLine("最大链接数:" + Socket_MaxLink);
                    #endregion

                    // WriteColor("Done");
                }
                catch (Exception ex)
                {
                    WriteColor("Error", null, true, ConsoleColor.Red);
                    Console.WriteLine(ex);
                    Console.WriteLine("按下任意键退出..");
                    Console.ReadKey();
                    System.Environment.Exit(0);
                }
            }
            else
            {

                WriteColor("Warning", null, true, ConsoleColor.Yellow);
                //如果不存在
                Console.Write("查找不到配置,是否通过向导创建配置?[y/n]");
                //询问是否创建配置
                if (Console.ReadLine() != "y")
                {
                    System.Environment.Exit(0);
                }
                else
                {
                    #region 设置配置文件
                    WriteColor("开始创建配置");
                    List<string> Setting = new List<string>();

                    WriteColor("输入本地IP地址", null, true, ConsoleColor.Yellow);
                    Setting.Add(Console.ReadLine());

                    WriteColor("输入本地端口", null, true, ConsoleColor.Yellow);
                    Setting.Add(Console.ReadLine());

                    WriteColor("输入服务器最大人数", null, true, ConsoleColor.Yellow);
                    Setting.Add(Console.ReadLine());

                    string[] SettingStrings = Setting.ToArray();
                    WriteColor("你的配置为");
                    int i = 0;
                    while (i != SettingStrings.Length)
                    {
                        Console.WriteLine(SettingStrings[i]);
                        i++;
                    }
                    #endregion
                    Console.Write("确定保存[y/n]");
                    if (Console.ReadLine() != "y")
                    {
                        System.Environment.Exit(0);
                    }
                    else
                    {
                        File.WriteAllLines(@".\Setting.config", SettingStrings);
                    }
                    WriteColor("保存完毕,按任意键退出");
                    Console.ReadKey();
                    System.Environment.Exit(0);
                }
            }
            #endregion
            #region 运作服务器
            try
            {
                //配置服务器
                WriteColor("配置服务器..");
                ipe = new IPEndPoint(IPAddress.Parse(Socket_ip), Socket_port);
                SubSocket = new Socket[Socket_MaxLink];

                //启动服务器
                WriteColor("开启服务器..");
                ParentSocket.Bind(ipe);
                // WriteColor("Done");
                WriteColor("服务器开始侦听");
                //多县城(雾)
            }
            catch (Exception ex)
            {
                WriteColor("Error", null, true, ConsoleColor.Red);
                Console.WriteLine(ex);
                Console.WriteLine("====尝试删除配置文件===");
                Console.WriteLine("按下任意键退出..");
                Console.ReadKey();
                System.Environment.Exit(0);
            }
            #endregion
            //启动监听线程
            Task<int> SocketListenTask = new Task<int>(() => SocketListen());
            SocketListenTask.Start();
            #region 命令区
            while (true)
            {
                string CMD = Console.ReadLine();
                if (CMD == "clear")
                {
                    Console.Clear();
                }

                else if (CMD == "rsl")
                {
                    Console.WriteLine("链接个数:" + SocketLink);
                }
                else if (CMD == "readipe")
                {
                    Console.WriteLine("输入ID");
                    int read = int.Parse(Console.ReadLine());
                    if (SubSocket[read] != null)
                    {
                        Console.WriteLine("该客户IP:" + SubSocket[read].RemoteEndPoint);
                    }
                    else
                    {
                        Console.WriteLine("ID不存在");
                    }
                }
                else if (CMD == "sendid")
                {
                    Console.WriteLine("输入ID");
                    int read = int.Parse(Console.ReadLine());
                    Console.WriteLine("输入内容");
                    byte[] inf = Encoding.ASCII.GetBytes(Console.ReadLine());
                    if (SubSocket[read] != null)
                    {
                        SubSocket[read].Send(inf);
                    }
                    else
                    {
                        Console.WriteLine("ID不存在");
                    }
                }
                else if (CMD == "send")
                {
                    Console.WriteLine("输入内容");
                    SocketWrite(Console.ReadLine());

                }

            }
            #endregion

        }

        #region 侦听线程
        static int SocketListen()
        {
            while (true)
            {
                ParentSocket.Listen(0);
                Socket serverSocket = ParentSocket.Accept();
                // serverSocket.Listen(0);
                //Console.WriteLine("[客户进入]" + serverSocket.RemoteEndPoint);
                WriteColor("客户进入", serverSocket.RemoteEndPoint);
                int NullSocketAccept = GetNullSocketAccept();
                if (NullSocketAccept != -1)
                {
                    SubSocket[NullSocketAccept] = serverSocket;
                    WriteColor("分配的客户ID为:" + NullSocketAccept);
                    Task<int> SubSocketReadInfTask = new Task<int>(() => SocketReadInf(NullSocketAccept));
                    SubSocketReadInfTask.Start();
                    SocketLink = SocketLink + 1;
                }
                else
                {
                    serverSocket.Send(Encoding.ASCII.GetBytes("Server is full"));
                    serverSocket.Close();
                }
                //Console.WriteLine(SubSocket[0].Poll(10, SelectMode.SelectRead));

            }
        }
        #endregion

        #region 获取空套接字
        static int GetNullSocketAccept()
        {
            int i = 0;
            bool WhileEnd = false;
            while (!WhileEnd)
            {

                if (SubSocket[i] == null)
                {
                    WhileEnd = true;
                }
                else
                {
                    if (i != SubSocket.Length - 1)
                    {
                        i++;
                    }
                    else
                    {
                        WhileEnd = true; i = -1;
                    }
                }
            }
            return i;
        }
        #endregion

        #region 套接字接受客户发送信息线程
        static int SocketReadInf(int SocketAccept)
        {
            while (true)
            {
                string Inf = "";
                byte[] recByte = new byte[4096];
                int bytes = SubSocket[SocketAccept].Receive(recByte, recByte.Length, 0);
                Inf = Encoding.ASCII.GetString(recByte, 0, bytes);
                if (Inf == "")
                {
                    WriteColor("客户退出", SubSocket[SocketAccept].RemoteEndPoint + ":" + SocketAccept);
                    SubSocket[SocketAccept].Close();
                    SubSocket[SocketAccept] = null;
                    SocketLink--;
                    return 0;
                }
                else
                {

                    WriteColor(SubSocket[SocketAccept].RemoteEndPoint + ":" + SocketAccept, Inf);
                    SocketWrite(Inf,SocketAccept);

                }
            }
            // return 0;
        }
        #endregion

        #region 群发
        static int SocketWrite(string inf,int id=-1) {
            int i = 0;
            while (i != SubSocket.Length)
            {
                if (SubSocket[i]!=null && i!=id)
                {
                    SubSocket[i].Send(Encoding.ASCII.GetBytes(inf));
                }
                i++;
            }
            return 0;
        }
        #endregion

        #region 彩色字体
        /// <summary>
        /// 彩色字体
        /// </summary>
        /// <param name="inf">带色的内容</param>
        /// <param name="inf2">追加内容</param>
        /// <param name="com">带色内容前后是否加入符号</param>
        /// <param name="color">颜色</param>
        static void WriteColor(object inf, object inf2=null,bool com = true,ConsoleColor color=ConsoleColor.Green)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (com) Console.Write("[");
            Console.ForegroundColor = color;
            Console.Write(inf);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (com) Console.Write("]");
            Console.ForegroundColor = ConsoleColor.White;
            if (inf2 == null) Console.Write("\r\n"); else Console.WriteLine(inf2);

        }
        #endregion
    }
}
