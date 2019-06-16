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
        //Socket配置
        static string[] SocketSettingStrings = null;
        //地址
        static IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        //母服务器
        static Socket ParentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //套接字服务器组
        static Socket[] SubSocket = new Socket[0];
        //服务器链接数
        static int SocketLink = 0;
        //白名单
        static bool WhiteList = false;

        //黑名单
        static string[] DarkLiskSetting = new string[0];
        static string[] WhiteLiskSetting = new string[0];
        #endregion
        static void Main(string[] args)
        {
            #region 设置空配置
            WriteColor("初始化配置..");
            SocketSettingStrings = null;
            string Socket_ip = "0.0.0.0";
            int Socket_port = 0;
            int Socket_MaxLink = 0;
            WhiteList = false;
            #endregion
            #region 加载Socket配置文件
            //查看文件是否存在
            if (File.Exists("./Setting.config"))
            {
                try
                {
                    //如果存在读取
                    WriteColor("加载配置..");
                    #region 读取配置区
                    SocketSettingStrings = System.IO.File.ReadAllLines(@".\Setting.config");
                    Socket_ip = SocketSettingStrings[0];
                    Socket_port = int.Parse(SocketSettingStrings[1]);
                    Socket_MaxLink = int.Parse(SocketSettingStrings[2]);
                    WhiteList = bool.Parse(SocketSettingStrings[3]);
                    #endregion

                    #region 输出配置区
                    Console.WriteLine("IP:" + Socket_ip);
                    Console.WriteLine("端口:" + Socket_port);
                    Console.WriteLine("最大链接数:" + Socket_MaxLink);
                    Console.WriteLine("白名单:" + WhiteList);
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

                    WriteColor("是否使用白名单[true/false]", null, true, ConsoleColor.Yellow);
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
            //加载名单
            ListRead();
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
            //启动监听线程
            Task<int> SocketListenTask = new Task<int>(() => SocketListen());
            SocketListenTask.Start();
            #endregion
            #region 命令
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
                else if (CMD == "ban")
                {
                    Console.WriteLine("输入id");
                    SocketOut(int.Parse(Console.ReadLine()));
                }
                else if (CMD == "List")
                {
                    ListRead();
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
                //获取空套接字
                int NullSocketAccept = GetNullSocketAccept();
                //黑名单白名单
                if (WhiteList)
                {
                    if (!ListSearch(WhiteLiskSetting, serverSocket.RemoteEndPoint.ToString())) { NullSocketAccept = -1; WriteColor("客户被拒绝", serverSocket.RemoteEndPoint); }
                }
                else
                {
                    if (ListSearch(DarkLiskSetting, serverSocket.RemoteEndPoint.ToString())) { NullSocketAccept = -1; WriteColor("客户被拒绝", serverSocket.RemoteEndPoint); }
                }

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
                   //serverSocket.Send(Encoding.ASCII.GetBytes("Server is full"));
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
                int bytes = 0;
                try
                {
                    bytes = SubSocket[SocketAccept].Receive(recByte, recByte.Length, 0);
                }
                catch
                {
                    return 0;
                }
                Inf = Encoding.ASCII.GetString(recByte, 0, bytes);
                if (Inf == "")
                {
                    SocketOut(SocketAccept);
                    return 0;
                }
                else
                {
                    WriteColor(SubSocket[SocketAccept].RemoteEndPoint + ":" + SocketAccept, Inf);
                    SocketWrite(Inf, SocketAccept);
                }
            }
            // return 0;
        }
        #endregion

        #region Socket out
        static void SocketOut(int SocketAccept)
        {
            if (SubSocket[SocketAccept] != null)
            {
                WriteColor("客户退出", SubSocket[SocketAccept].RemoteEndPoint + ":" + SocketAccept);
                SubSocket[SocketAccept].Close();
                SubSocket[SocketAccept] = null;
                SocketLink--;
            }
            else
            {
                Console.WriteLine("用户不存在");
            }
        }
        #endregion

        #region 群发
        static int SocketWrite(string inf, int id = -1) {
            int i = 0;
            while (i != SubSocket.Length)
            {
                if (SubSocket[i] != null && i != id)
                {
                    SubSocket[i].Send(Encoding.ASCII.GetBytes(inf));
                }
                i++;
            }
            return 0;
        }
        #endregion

        #region 加载名单
        static void ListRead()
        {
            WriteColor("加载名单");
            if (File.Exists("./DarkLiskSetting.config"))
            {
                DarkLiskSetting = File.ReadAllLines("./DarkLiskSetting.config");
            }
            else
            {
                WriteColor("找不到黑名单,已自动生成空文件", null, true, ConsoleColor.Yellow);
                File.WriteAllLines("./DarkLiskSetting.config", DarkLiskSetting);
            }
            if (File.Exists("./WhiteLiskSetting.config"))
            {
                WhiteLiskSetting = File.ReadAllLines("./WhiteLiskSetting.config");
            }
            else
            {
                WriteColor("找不到白名单,已自动生成空文件", null, true, ConsoleColor.Yellow);
                File.WriteAllLines("./WhiteLiskSetting.config", WhiteLiskSetting);
            }

            WriteColor("===黑名单配置===");
            WhileWriteArray(DarkLiskSetting);
            WriteColor("================");
            WriteColor("===白名单配置===");
            WhileWriteArray(WhiteLiskSetting);
            WriteColor("================");
        }

#endregion


#region 简易函数
#region 彩色字体
/// <summary>
/// 彩色字体
/// </summary>
/// <param name="inf">带色的内容</param>
/// <param name="inf2">追加内容</param>
/// <param name="com">带色内容前后是否加入符号</param>
/// <param name="color">颜色</param>
static void WriteColor(object inf, object inf2 = null, bool com = true, ConsoleColor color = ConsoleColor.Green)
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

        #region 循环输出
        static void WhileWriteArray(string[] inf)
        {
            int i = 0;
            while (i != inf.Length)
            {
                Console.WriteLine(inf[i]);
                i = i + 1;
            }
        }
        #endregion

        #region 列表搜索
        static bool ListSearch(string[] List,string SearchInf)
        {
            int i = 0;
            while (i != List.Length)
            {
                if (SearchInf.IndexOf(List[i])!=-1)
                {
                    return true;
                }
                i = i + 1;
            }
            return false;
        }
        #endregion
        #endregion
    }
}
