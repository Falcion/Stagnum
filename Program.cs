﻿using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace Stratum
{
    public class Program
    {
        public string AUTH_TOKEN;
        public string BOT_PREFIX;

        private static void Main(string[] args)
            => new Program().Init().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandService cmd;
        private IServiceProvider service;

        private async Task Init()
        {
            Console.OutputEncoding = Encoding.Unicode;

            Configuration.Init();

            client = new DiscordSocketClient();
            cmd = new CommandService();

            service = new ServiceCollection()
                                .AddSingleton(client)
                                .AddSingleton(cmd)
                                .BuildServiceProvider();

            await PostInit();

            client.Log += Logging;

            AUTH_TOKEN = Configuration.getAuthToken();

            await client.LoginAsync(TokenType.Bot, AUTH_TOKEN);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Logging(LogMessage arg)
        {
            string? message = arg.Message;

            if (message.Contains("Exception")) Logger.Error(message);
            else Logger.Info(message);

            return Task.CompletedTask;
        }

        private async Task PostInit()
        {
            client.MessageReceived += Handler;

            await cmd.AddModulesAsync(Assembly.GetEntryAssembly(), service);
        }

        private async Task Handler(SocketMessage arg)
        {
            BOT_PREFIX = Configuration.getBotPrefix();

            SocketUserMessage msg = arg as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(client, msg);

            if (msg.Author.IsBot) return;

            int argPos = 0;
            if(msg.HasStringPrefix(BOT_PREFIX, ref argPos))
            {
                IResult result = await cmd.ExecuteAsync(context, argPos, service);

                if (!result.IsSuccess)
                {
                    Logger.Error(result.Error + " 001x00 " + result.ErrorReason);

                    EmbedBuilder embed = new EmbedBuilder();

                    int hash = result.Error.GetHashCode();

                    embed.WithTitle($"{hash}")
                         .WithColor(Color.Red)
                         .AddField("Error:", result.Error)
                         .AddField("Reason:", result.ErrorReason);

                    await context.Channel.SendMessageAsync(null, false, embed.Build());
                }

                else Logger.Info($"{result}");
            }
        }
    }
}
