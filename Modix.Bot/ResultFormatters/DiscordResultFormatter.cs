﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MediatR;
using Modix.Common.ErrorHandling;
using Modix.Services.Core;

namespace Modix.Bot.ResultFormatters
{
    /// <summary>
    /// Formats instances of <typeparamref name="TInput"/> into <see cref="EmbedBuilder"/>s for sending to Discord
    /// </summary>
    public abstract class DiscordResultFormatter<TInput> : IResultFormatter<TInput, EmbedBuilder> where TInput : ServiceResult
    {
        protected ICommandContext Context { get; private set; }

        public DiscordResultFormatter(ICommandContextAccessor contextAccessor)
        {
            Context = contextAccessor.Context;
        }

        /// <inheritdoc />
        public abstract EmbedBuilder Format(TInput result);
    }

    //We need to seal the generic otherwise DI is not happy with us
    /// <inheritdoc />
    public class DefaultDiscordResultFormatter : DiscordResultFormatter<ServiceResult>
    {
        public DefaultDiscordResultFormatter(ICommandContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

        public override EmbedBuilder Format(ServiceResult result)
        {
            if (result.IsSuccess) { return null; }

            return new EmbedBuilder()
                .WithTitle("Uh oh, an error occurred.")
                .WithDescription(result.Error)
                .WithColor(new Color(255, 0, 0));
        }
    }
}
