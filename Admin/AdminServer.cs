using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Discord.WebSocket;
using System.Linq;
using System.Collections.Generic;

namespace Sevenisko.IceBot.Admin
{
    public class AdminServer
    {
        public Socket adminSocket;

        public SocketGuild discordServer;

        public Thread serverThread;

        public static bool isRunning = false;

        public AdminServer(int Port)
        {
            try
            {
                Program.LogText(Discord.LogSeverity.Info, "AdminListener", "Remote control started on port " + Port);
                adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                adminSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
                adminSocket.Listen(0);
                isRunning = true;
                serverThread = new Thread(new ThreadStart(UpdateServer));
                serverThread.Start();
            }
            catch (SocketException e)
            {
                Program.LogText(Discord.LogSeverity.Info, "AdminListener", "Cannot start remote control server: " + e.Message);
            }
        }

        public void UpdateServer()
        {
            while(isRunning)
            {
                Socket acceptedData = adminSocket.Accept();
                byte[] data = new byte[acceptedData.SendBufferSize];
                int j = acceptedData.Receive(data);
                byte[] adata = new byte[j];
                for (int i = 0; i < j; i++)
                    adata[i] = data[i];
                string plainmsg = Encoding.UTF8.GetString(adata);
                string[] msg = plainmsg.Split(' ');
                IPEndPoint remoteIpEndPoint = acceptedData.RemoteEndPoint as IPEndPoint;
                switch (msg[0])
                {
                    case "hello":
                        {
                            acceptedData.Send(Encoding.UTF8.GetBytes("Hello there."));
                        }
                        break;
                    case "login":
                        {
                            foreach(AdminUser user in Shared.Users)
                            {
                                bool usernameRight = false;
                                bool passwordRight = false;

                                if(msg[1] == user.Username)
                                {
                                    usernameRight = true;
                                }

                                if(msg[2] == user.Password)
                                {
                                    passwordRight = true;
                                }

                                if(usernameRight && passwordRight)
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("success"));
                                    AdminSession newSession = new AdminSession()
                                    {
                                        IP = remoteIpEndPoint.Address.MapToIPv4().ToString(),
                                        User = user
                                    };
                                    Shared.Sessions.Add(newSession);
                                }
                                else
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("badlogin"));
                                }
                            }
                        }
                        break;
                    case "logout":
                        {
                            foreach(AdminSession session in Shared.Sessions)
                            {
                                if(session.IP == remoteIpEndPoint.Address.MapToIPv4().ToString())
                                {
                                    Shared.Sessions.Remove(session);
                                    acceptedData.Send(Encoding.UTF8.GetBytes("success"));
                                }
                                else
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("nosession"));
                                }
                            }
                        }
                        break;
                    case "cmdsenabled":
                        {
                            foreach (AdminSession session in Shared.Sessions)
                            {
                                if (session.IP == remoteIpEndPoint.Address.MapToIPv4().ToString())
                                {
                                    bool canEnableCommands = bool.Parse(msg[1]);

                                    if (canEnableCommands)
                                    {
                                        Shared.commandsEnabled = true;
                                        acceptedData.Send(Encoding.UTF8.GetBytes("cmdsareon"));
                                    }
                                    else
                                    {
                                        Shared.commandsEnabled = false;
                                        acceptedData.Send(Encoding.UTF8.GetBytes("cmdsareoff"));
                                    }
                                }
                                else
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("nopermission"));
                                }
                            }
                        }
                        break;
                    case "usercount":
                        {
                            foreach (AdminSession session in Shared.Sessions)
                            {
                                if (session.IP == remoteIpEndPoint.Address.MapToIPv4().ToString())
                                {
                                    int usersCount = Shared.discordServer.Users.Count;
                                    acceptedData.Send(Encoding.UTF8.GetBytes("count-" + usersCount));
                                }
                                else
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("nopermission"));
                                }
                            }
                        }
                        break;
                    case "userlist":
                        {
                            foreach (AdminSession session in Shared.Sessions)
                            {
                                if (session.IP == remoteIpEndPoint.Address.MapToIPv4().ToString())
                                {
                                    string userList = "";
                                    foreach (SocketGuildUser user in Shared.discordServer.Users)
                                    {
                                        if(user == Shared.discordServer.Users.Last())
                                        {
                                            userList += user.Username + "#" + user.Discriminator + "¤";
                                        }
                                        else
                                        {
                                            userList += user.Username + "#" + user.Discriminator;
                                        }
                                        acceptedData.Send(Encoding.UTF8.GetBytes(userList));
                                    }
                                }
                                else
                                {
                                    acceptedData.Send(Encoding.UTF8.GetBytes("nopermission"));
                                }
                            }
                        }
                        break;
                }
            }
        }
    }

    public class AdminSession
    {
        public string IP { get; set; }
        public AdminUser User { get; set; }
    }

    public class AdminUser
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
