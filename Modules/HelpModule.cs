using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sevenisko.IceBot.Modules
{
    public class HelpModule : ModuleBase
    {
        private readonly CommandService Commands;

        private readonly IServiceProvider Provider;

        private bool UseRemarks = true;

        public HelpModule(CommandService commands, IServiceProvider provider)
        {
            Commands = commands;
            Provider = provider;
        }

        [Command("help")]
        [Alias(new string[]
        {
            "help",
            "h"
        })]
        [Summary("Finds all the modules and prints out it's summary tag.")]
        public async Task HelpAsync()
        {
            IEnumerable<ModuleInfo> enumerable = from x in Commands.Modules
                                                 where !string.IsNullOrWhiteSpace(x.Summary)
                                                 select x;
            EmbedBuilder emb = new EmbedBuilder();
            emb.WithColor(Color.Green);
            emb.WithTitle("IceBot commands");
            foreach (ModuleInfo module in enumerable)
            {
                bool success = false;
                foreach (CommandInfo command in module.Commands)
                {
                    if ((await command.CheckPreconditionsAsync(base.Context, Provider)).IsSuccess)
                    {
                        success = true;
                        break;
                    }
                }
                if (success)
                {
                    emb.AddField(module.Name, module.Summary);
                }
            }
            if (emb.Fields.Count <= 0)
            {
                await ReplyAsync("Module information cannot be found, please try again later.");
            }
            else
            {
                await ReplyAsync("", isTTS: false, emb.Build());
            }
        }

        [Command("help")]
        [Alias(new string[]
        {
            "help",
            "h"
        })]
        [Summary("Finds all the commands from a specific module and prints out it's summary tag.")]
        public async Task HelpAsync(string moduleName)
        {
            ModuleInfo moduleInfo = Commands.Modules.FirstOrDefault((ModuleInfo x) => x.Name.ToLower() == moduleName.ToLower());
            if (moduleInfo == null)
            {
                await ReplyAsync("The module `" + moduleName + "` does not exist. Are you sure you typed the right module?");
                await HelpAsync();
                return;
            }
            IEnumerable<CommandInfo> enumerable = from x in moduleInfo.Commands
                                                  where !string.IsNullOrWhiteSpace(x.Summary)
                                                  group x by x.Name into x
                                                  select x.First();
            if (!enumerable.Any())
            {
                await ReplyAsync("The module `" + moduleInfo.Name + "` has no available commands.");
                return;
            }
            EmbedBuilder emb = new EmbedBuilder();
            emb.WithColor(Color.Green);
            emb.WithTitle("IceBot commands (Module: " + moduleName + ")");
            foreach (CommandInfo command in enumerable)
            {
                if ((await command.CheckPreconditionsAsync(base.Context, Provider)).IsSuccess)
                {
                    emb.AddField(UseRemarks ? command.Remarks : command.Aliases.First(), command.Summary);
                }
            }
            if (emb.Fields.Count <= 0)
            {
                await ReplyAsync("Command information cannot be found, please try again later.");
            }
            else
            {
                await ReplyAsync("", isTTS: false, emb.Build());
            }
        }
    }
}
