using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sevenisko.IceBot.Modules
{
    [Name("Main")]
    [Summary("Main module gives some basic functions.")]
    public class MainModule : ModuleBase<SocketCommandContext>
    {
        [Command("translate")]
        [Alias(new string[]
        {
            "trans"
        })]
        [Remarks("!translate [originalLang] [translatedLang] [msg]")]
        [Summary("The bot will translate an message for you.")]
        public async Task TranslateIt(string o = null, string t = null, [Remainder] string s = null)
        {
            if (o == null | t == null | s == null)
            {
                await ReplyAsync("Usage: `!translate <originalLanguage> <translatedLanguage> <text>`");
            }
            else if (o == t)
            {
                await ReplyAsync("Cannot translate into same language!");
            }
            else
            {
                WebClient client = new WebClient();
                string URL = "https://translate.yandex.net/api/v1.5/tr.json/translate?key=trnsl.1.1.20190213T195546Z.c022ac82714601d5.7e1b084d228374fa6f4799c85512e666bc6e1806&text=" + s + "&lang=" + o + "-" + t + "";
                await Program.LogText(LogSeverity.Info, "Translator", $"Sending request to API: [({o} -> {t}): {s}]");
                try
                {
                    byte[] returnData = client.DownloadData(URL);
                    string Code = Encoding.UTF8.GetString(returnData);
                    var l = JsonConvert.DeserializeObject<TranslateResponse>(Code);
                    if (l.code == 200)
                    {
                        await ReplyAsync("**" + Context.User.Username + "#" + Context.User.Discriminator + " (" + o + " -> " + t + "):** " + l.text[0]);
                    }
                    else
                    {
                        await Context.User.SendFileAsync("languages.txt", "Incorrect language, check this file for avaiable languages...");
                    }
                }
                catch
                {
                    await Program.LogText(LogSeverity.Info, "Translator", "Exception is thrown, cannot translate the text!");
                    await Context.User.SendFileAsync("languages.txt", "Cannot translate text, got invalid response...");
                }
            }
        }

        [Command("serverInfo")]
        [Alias(new string[]
        {
            "serverinfo"
        })]
        [Remarks("!serverinfo")]
        [Summary("Gets informations about this server.")]
        public async Task GetInfo()
        {
            var emb = new EmbedBuilder
            {
                Title = "Server info",
                Description = "There are informations about server."
            };
            emb.WithAuthor(Context.Client.CurrentUser);
            emb.WithColor(Color.Blue);
            emb.WithFooter("IceBot v" + BotInfo.GetBotVersion());
            emb.WithThumbnailUrl("https://cdn.discordapp.com/icons/516650791885471744/394b8f725ee8bfa2c770bc20bb206254.png");
            emb.AddField("Owner", Context.Guild.Owner.Username + "#" + Context.Guild.Owner.Discriminator);
            emb.AddField("Created at", Context.Guild.CreatedAt.ToString("dd.MM.yyyy - HH:mm:ss"));
            emb.AddField("Location", Context.Guild.VoiceRegionId);
            emb.AddField("All member count", Context.Guild.Users.Count);
            emb.AddField("Bot author", "Sevenisko#3292");
            await ReplyAsync(null, false, emb.Build());
        }

        [Command("botInfo")]
        [Alias(new string[]
        {
            "botinfo",
            "version",
            "ver"
        })]
        [Remarks("!version")]
        [Summary("Gets informations about IceBot.")]
        public async Task GetVersion()
        {
            var emb = new EmbedBuilder
            {
                Title = "IceBot v" + BotInfo.GetBotVersion(),
                Description = "The world is not controled by laws on papers, the people controls the world."
            };
            emb.WithAuthor(Context.Client.CurrentUser);
            emb.WithColor(Color.Red);
            emb.WithFooter("I like that, but i'm so alone.");
            emb.WithThumbnailUrl("https://cdn.discordapp.com/app-icons/516975705066700850/dfe5df212a161435768af049c48c6944.png");
            emb.AddField("Created by", "Sevenisko#3292");
            switch(BotInfo.GetRelease())
            {
                case BotInfo.DotNetRelease.NET48:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.8");
                    }
                    break;
                case BotInfo.DotNetRelease.NET472:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.7.2");
                    }
                    break;
                case BotInfo.DotNetRelease.NET471:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.7.1");
                    }
                    break;
                case BotInfo.DotNetRelease.NET47:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.7");
                    }
                    break;
                case BotInfo.DotNetRelease.NET462:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.6.2");
                    }
                    break;
                case BotInfo.DotNetRelease.NET461:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.6.1");
                    }
                    break;
                case BotInfo.DotNetRelease.NET46:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.6");
                    }
                    break;
                case BotInfo.DotNetRelease.NET452:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.5.2");
                    }
                    break;
                case BotInfo.DotNetRelease.NET451:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.5.1");
                    }
                    break;
                case BotInfo.DotNetRelease.NET45:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.5");
                    }
                    break;
                case BotInfo.DotNetRelease.NET40:
                    {
                        emb.AddField("Runtime", ".NET Framework 4.0");
                    }
                    break;
                case BotInfo.DotNetRelease.NET35:
                    {
                        emb.AddField("Runtime", ".NET Framework 3.5");
                    }
                    break;
                case BotInfo.DotNetRelease.NET20:
                    {
                        emb.AddField("Runtime", ".NET Framework 2.0");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE10:
                    {
                        emb.AddField("Runtime", ".NET Core 1.0");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE11:
                    {
                        emb.AddField("Runtime", ".NET Core 1.1");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE12:
                    {
                        emb.AddField("Runtime", ".NET Core 1.2");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE20:
                    {
                        emb.AddField("Runtime", ".NET Core 2.0");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE21:
                    {
                        emb.AddField("Runtime", ".NET Core 2.1");
                    }
                    break;
                case BotInfo.DotNetRelease.CORE22:
                    {
                        emb.AddField("Runtime", ".NET Core 2.2");
                    }
                    break;
                default:
                    {
                        emb.AddField("Runtime", "Undetected .NET runtime");
                    }
                    break;
            }
            emb.AddField("Build date-time", BotInfo.GetBotBuildDateTime().ToString("dd.MM.yyyy - HH:mm:ss"));
            emb.AddField("Discord.Net Version", DiscordConfig.Version + " (API v" + DiscordConfig.APIVersion + ")");
            emb.AddField("3rd party", "- Yandex Translator API\n- ffmpeg (Used for audio streaming)\n- youtube-dl (Online downloader)");
            await ReplyAsync(null, false, emb.Build());
        }
    }

    class TranslateResponse
    {
        public int code;
        public string lang;
        public string[] text;
    }
}
