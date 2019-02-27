﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MediatR;
using Modix;
using Modix.Behaviors;
using Modix.Data.Messages;
using Modix.Data.Models.Core;
using Modix.Data.Repositories;
using Modix.Services;
using Modix.Services.Adapters;
using Modix.Services.AutoCodePaste;
using Modix.Services.AutoRemoveMessage;
using Modix.Services.BehaviourConfiguration;
using Modix.Services.CodePaste;
using Modix.Services.CommandHelp;
using Modix.Services.Core;
using Modix.Services.Csharp;
using Modix.Services.DocsMaster;
using Modix.Services.GuildStats;
using Modix.Services.Mentions;
using Modix.Services.Moderation;
using Modix.Services.NotificationDispatch;
using Modix.Services.PopularityContest;
using Modix.Services.Promotions;
using Modix.Services.Quote;
using Modix.Services.Starboard;
using Modix.Services.StackExchange;
using Modix.Services.Tags;
using Modix.Services.Wikipedia;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddModix(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddHttpClient("ReplClient")
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var config = serviceProvider.GetRequiredService<ModixConfig>();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", config.ReplToken);
                });

            services.AddHttpClient("CodePasteClient")
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                });

            services.AddHttpClient("StackExchangeClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip,
                });

            services.AddSingleton(
                provider => new DiscordSocketClient(config: new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = provider.GetRequiredService<ModixConfig>().MessageCacheSize //needed to log deletions
                }));

            services.AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>());
            services.AddScoped<ISelfUser>(p => p.GetRequiredService<DiscordSocketClient>().CurrentUser);

            services.AddSingleton(
                provider => new DiscordRestClient(config: new DiscordRestConfig
                {
                    LogLevel = LogSeverity.Debug,
                }));

            services.AddSingleton(_ =>
                {
                    var service = new CommandService(
                        new CommandServiceConfig
                        {
                            LogLevel = LogSeverity.Debug,
                            DefaultRunMode = RunMode.Sync,
                            CaseSensitiveCommands = false,
                            SeparatorChar = ' '
                        });

                    service.AddTypeReader<IEmote>(new EmoteTypeReader());
                    service.AddTypeReader<DiscordUserEntity>(new UserEntityTypeReader());

                    return service;
                });

            services.AddSingleton<DiscordSerilogAdapter>();
            services.AddMediator();

            services.AddModixCore()
                .AddModixModeration()
                .AddModixPromotions()
                .AddAutoCodePaste()
                .AddCommandHelp()
                .AddGuildStats()
                .AddMentions()
                .AddModixTags()
                .AddNotificationDispatch()
                .AddStarboard()
                .AddAutoRemoveMessage();

            services.AddSingleton<IBehavior, DiscordAdapter>();
            services.AddScoped<IQuoteService, QuoteService>();
            services.AddSingleton<IBehavior, MessageLinkBehavior>();
            services.AddSingleton<CodePasteHandler>();
            services.AddSingleton<IBehavior, AttachmentBlacklistBehavior>();
            services.AddSingleton<CodePasteService>();
            services.AddScoped<DocsMasterRetrievalService>();
            services.AddMemoryCache();

            services.AddSingleton<ICodePasteRepository, MemoryCodePasteRepository>();
            services.AddScoped<IPopularityContestService, PopularityContestService>();
            services.AddScoped<WikipediaService>();
            services.AddScoped<StackExchangeService>();
            services.AddScoped<DocumentationService>();

            services.AddScoped<IBehaviourConfigurationRepository, BehaviourConfigurationRepository>();
            services.AddScoped<IBehaviourConfigurationService, BehaviourConfigurationService>();
            services.AddSingleton<IBehaviourConfiguration, BehaviourConfiguration>();

            services.AddScoped<IModerationActionEventHandler, ModerationLoggingBehavior>();
            services.AddScoped<INotificationHandler<PromotionActionCreated>, PromotionLoggingHandler>();

            services.AddHostedService<ModixBot>();

            return services;
        }
    }
}
