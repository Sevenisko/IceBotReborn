using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sevenisko.IceBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);

            await Program.LogText(LogSeverity.Info, "CommandHandler", "Loaded " + Program.config.Messages.Count + " answers and questions.");

            await Program.LogText(LogSeverity.Info, "CommandHandler", "Loaded " + _commands.Modules.Count() + " modules, " + _commands.Commands.Count() + " commands.");
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            foreach (Message msg in Program.config.Messages)
            {
                if (message.Content.ToLower().Contains(msg.Question.ToLower()) && message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    await message.Channel.SendMessageAsync(msg.Answer.Replace("%g", "<").Replace("%s", ">"));
                }
            }

            if ((message.HasStringPrefix(Program.config.CommandPrefix, ref argPos) && !message.Author.IsBot))
            {
                var context = new SocketCommandContext(_client, message);

                await Program.Log(new LogMessage(LogSeverity.Info, "CommandHandler", "User " + messageParam.Author.Username + " in #" + messageParam.Channel.Name + " sent command: " + message));

                // Execute the command with the command context we just
                // created, along with the service provider for precondition checks.
                if(Shared.commandsEnabled)
                {
                    IResult result = await _commands.ExecuteAsync(context: context, argPos: argPos, services: null);
                    if (!result.IsSuccess)
                    {
                        if (result.ErrorReason == "Unknown command.")
                        {
                            Random r = new Random();
                            int index = r.Next(0, 5);
                            int index2 = r.Next(0, 5);
                            if (index == index2)
                            {
                                await context.Channel.SendMessageAsync("I'm sorry, but i'm now watching to Pornhub and i don't know that command.");
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync("I'm sorry, but i don't know that command.");
                            }
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync(result.ToString());
                        }
                    }
                }
                else
                {
                    await context.Channel.SendMessageAsync("I'm sorry, but commands are disabled by Administrator.");
                }
            }
            else
            {
                if (Program.config.AntiBWEnabled)
                {
                    foreach (string badword in Program.config.BadWords)
                    {
                        if (message.Content.ToLower().Contains(badword.ToLower()))
                        {
                            await message.DeleteAsync();
                            await message.Channel.SendMessageAsync("That bad behaviour? Just be calm! <:fuckyou:545662379560796214>");
                            return;
                        }
                    }
                }
            }
        }
    }
}
