using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Modix.Services.Core;
using Modix.Data.Models.Core;
using System.Linq;

namespace Modix.Services
{
    public class GuildEventBehavior : BehaviorBase
    {
        private readonly DiscordSocketClient _discordClient;

        public GuildEventBehavior(
            DiscordSocketClient discordClient,
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _discordClient = discordClient;
        }

        internal protected override Task OnStartingAsync()
        {
            _discordClient.UserJoined += OnUserJoined;
            _discordClient.UserLeft += OnUserLeave;
            _discordClient.ChannelCreated += OnChannelCreated;
            _discordClient.ChannelDestroyed += OnChannelDeleted;
            _discordClient.RoleCreated += OnRoleCreated;
            _discordClient.RoleDeleted += OnRoleDeleted;
            //_discordClient.RoleUpdated += OnRoleUpdated;
            _discordClient.GuildMemberUpdated += OnUserUpdated;
            _discordClient.UserVoiceStateUpdated += OnUserVoiceStateUpdated;

            return Task.CompletedTask;
        }

        internal protected override Task OnStoppedAsync()
        {
            _discordClient.UserJoined -= OnUserJoined;
            _discordClient.UserLeft -= OnUserLeave;
            _discordClient.ChannelCreated -= OnChannelCreated;
            _discordClient.ChannelDestroyed -= OnChannelDeleted;
            _discordClient.RoleCreated -= OnRoleCreated;
            _discordClient.RoleDeleted -= OnRoleDeleted;
            //_discordClient.RoleUpdated -= OnRoleUpdated;
            _discordClient.UserUpdated -= OnUserUpdated;
            _discordClient.UserVoiceStateUpdated -= OnUserVoiceStateUpdated;

            return Task.CompletedTask;
        }

        private async Task OnUserUpdated(SocketUser oldUser, SocketUser newUser)
        {
            if (!(oldUser is SocketGuildUser oldGuildUser && newUser is SocketGuildUser newGuildUser))
            {
                return;
            }

            if (oldGuildUser.Nickname != (newUser as SocketGuildUser).Nickname)
            {
                var message = $"{oldGuildUser.ToString()} changed nickname to {Format.Bold(newGuildUser.Nickname ?? newGuildUser.Username)}";

                await LogEvent(message, oldGuildUser.Guild);
            }
        }

        private async Task OnRoleCreated(SocketRole role)
        {
            var message = $"Role created: {role.Name}";
            await LogEvent(message, role.Guild);
        }

        private Task OnRoleUpdated(SocketRole oldRole, SocketRole newRole)
        {
            throw new NotImplementedException();
        }

        private async Task OnRoleDeleted(SocketRole role)
        {
            var message = $"Role deleted: {role.Name} Affected users: {string.Join("\n", role.Members.Select(x => x.ToString()))}";
            await LogEvent(message, role.Guild);
        }

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            if (!(user is SocketGuildUser guildUser))
            {
                return;
            }

            var message = Format.Bold(guildUser.ToString());
            if (oldVoiceState.VoiceChannel == null)
            {
                message += $" joined voice-channel: {newVoiceState.VoiceChannel.Name}";
            }
            else
            {
                message += $" left voice-channel: {oldVoiceState.VoiceChannel.Name}";
            }

            await LogEvent(message, guildUser.Guild);
        }

        private async Task OnChannelDeleted(SocketChannel channel)
        {
            if(!(channel is ITextChannel guildChannel))
            {
                return;
            }

            var message = $"Channel deleted: {guildChannel.Name}";
            await LogEvent(message, guildChannel.Guild);
        }

        private async Task OnChannelCreated(SocketChannel channel)
        {
            if (!(channel is ITextChannel guildChannel))
            {
                return;
            }

            var message = $"Channel created: {guildChannel.Name}";
            await LogEvent(message, guildChannel.Guild);
        }

        private async Task OnUserLeave(SocketGuildUser guildUser)
        {
            var message = $"{guildUser.ToString()} left the guild.";
            await LogEvent(message, guildUser.Guild);
        }

        private async Task OnUserJoined(SocketGuildUser guildUser)
        {
            var message = $"{guildUser.ToString()} joined the guild.";
            await LogEvent(message, guildUser.Guild);
        }

        private async Task LogEvent(string message, IGuild guild)
            => await SelfExecuteRequest<IDesignatedChannelService>(
                async (designatedChannelService)
                    => await designatedChannelService
                        .SendToDesignatedChannelsAsync(guild,
                                                       DesignatedChannelType.GuildEventLog,
                                                       message));
    }
}
