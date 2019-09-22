using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Discord.Audio;
using System.Threading;

namespace Sevenisko.IceBot
{
    class Program
    {
        static DiscordSocketClient client;
        public static ConfigFile config;
        Admin.WebServer webServer;

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(EventHandler Handler, bool Add);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        public delegate bool EventHandler(CtrlType sig);

        private EventHandler _handler;

        private static bool ConsoleCtrlCheck(CtrlType ctrlType)
        {
            LogText(LogSeverity.Info, "Main", "Exiting IceBot...");
            client.LogoutAsync();
            client.StopAsync();
            
            LogText(LogSeverity.Info, "Main", "IceBot successfully exited, have i nice day and goodbye.");
            Environment.Exit(0);
            return true;
        }

        public static void ThrowException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("*** IceBot has stopped working properly! ***");
            Console.WriteLine("Exception class: " + (e.ExceptionObject as Exception).GetType().Name);
            Console.WriteLine("Source: " + (e.ExceptionObject as Exception).Source);
            Console.WriteLine("Message: " + (e.ExceptionObject as Exception).Message);
            Console.WriteLine("Stack trace: \n" + (e.ExceptionObject as Exception).StackTrace);
            Environment.Exit(1);
        }

        public static void ThrowFatal(string message)
        {
            LogText(LogSeverity.Critical, "Main", message);
            Environment.Exit(1);
        }

        public static void Main(string[] args)
            => new Program().StartBot().GetAwaiter().GetResult();

        public async Task StartBot()
        {
            Console.Title = "IceBot v" + BotInfo.GetBotVersion();
            Console.WriteLine("============================");
            Console.WriteLine("  ...IceBot is starting...  ");
            Console.WriteLine("============================");
            _handler = new EventHandler(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(_handler, true);
            AppDomain.CurrentDomain.UnhandledException += ThrowException;
            config = Configuration.LoadConfig("Config.xml");
            client = new DiscordSocketClient();

            webServer = new Admin.WebServer();

            if (config.BotToken == "InsertHere")
            {
                ThrowFatal("Set your bot token first!");
            }

            CommandServiceConfig servConf = new CommandServiceConfig();
            servConf.CaseSensitiveCommands = true;
            servConf.DefaultRunMode = RunMode.Async;
            servConf.IgnoreExtraArgs = false;
            CommandService service = new CommandService(servConf);

            Services.CommandHandler handler = new Services.CommandHandler(client, service);

            await handler.InstallCommandsAsync();

            client.Log += Log;
            client.Ready += Client_Ready;

            try
            {
                await client.LoginAsync(TokenType.Bot, config.BotToken);
            }
            catch
            {
                Environment.Exit(1);
            }
            await client.StartAsync();

            if (config.WebSettings.Enabled)
            {
                webServer.Start($"http://{config.WebSettings.Address}:{config.WebSettings.Port}/");
            }

            await Task.Delay(-1);
        }

        private async Task Client_Ready()
        {
            await LogText(LogSeverity.Info, "Main", "Connected as " + client.CurrentUser.Username + "#" + client.CurrentUser.Discriminator);
            Shared.discordServer = client.Guilds.First();
            await client.SetGameAsync(config.StatusOnJoin, null, ActivityType.Playing);
        }

        public static Task LogText(LogSeverity severity, string source, string message)
        {
            Log(new LogMessage(severity, source, message));
            return Task.CompletedTask;
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + " - " + msg.Source + "]: " + msg.Message);
            return Task.CompletedTask;
        }
    }

    public class AdminUser
    {
        public string Username;
        public string Password;
    }

    public class Shared
    {
        public static List<string> BadWords { get; set; }
        public static bool commandsEnabled = true;
        public static SocketGuild discordServer { get; set; }

        public static ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        public static ConcurrentQueue<AudioFile> Playlist = new ConcurrentQueue<AudioFile>();

        public static AudioDownloader AudioDownloader = new AudioDownloader();

        public static AudioPlayer AudioPlayer = new AudioPlayer();

        public static bool AutoPlay;

        public static bool AutoPlayRunning;
    }
}
