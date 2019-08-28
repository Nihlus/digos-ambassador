//
//  ClientEventBehaviour.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.Behaviours
{
    /// <summary>
    /// Represents a behaviour that continuously monitors and responds to interactions.
    /// </summary>
    [PublicAPI]
    public abstract class ClientEventBehaviour : ContinuousBehaviour
    {
        /// <summary>
        /// Gets the events that are currently running.
        /// </summary>
        private ConcurrentQueue<Task> RunningEvents { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEventBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        protected ClientEventBehaviour(DiscordSocketClient client)
            : base(client)
        {
            this.RunningEvents = new ConcurrentQueue<Task>();
        }

        /// <summary>
        /// Raised when a channel is created.
        /// </summary>
        /// <param name="channel">The created channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ChannelCreated([NotNull] SocketChannel channel)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a channel is deleted.
        /// </summary>
        /// <param name="channel">The created channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ChannelDeleted([NotNull] SocketChannel channel)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a channel is updated.
        /// </summary>
        /// <param name="originalChannel">The original channel.</param>
        /// <param name="newChannel">The new channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ChannelUpdated
        (
            [NotNull] SocketChannel originalChannel,
            [NotNull] SocketChannel newChannel
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task MessageReceived([NotNull] SocketMessage message)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a message is deleted.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task MessageDeleted
        (
            Cacheable<IMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when messages are deleted in bulk.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task MessagesBulkDeleted
        (
            [NotNull] IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
            [NotNull] ISocketMessageChannel channel
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a message is updated.
        /// </summary>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="newMessage">The new message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task MessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            [NotNull] SocketMessage newMessage,
            [NotNull] ISocketMessageChannel channel
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a reaction is added to a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel,
            [NotNull] SocketReaction reaction
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a reaction is removed from a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel,
            [NotNull] SocketReaction reaction
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when all reactions are cleared from a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ReactionsCleared
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a role is created.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task RoleCreated([NotNull] SocketRole role)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a role is deleted.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task RoleDeleted([NotNull] SocketRole role)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a role's information is updated.
        /// </summary>
        /// <param name="oldRole">The old role information.</param>
        /// <param name="newRole">The new role information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task RoleUpdated([NotNull] SocketRole oldRole, [NotNull] SocketRole newRole)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the bot joins a guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task JoinedGuild([NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the bot leaves a guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task LeftGuild([NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a guild becomes available.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task GuildAvailable([NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a guild becomes unavailable.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task GuildUnavailable([NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when information about offline guild members has finished downloading.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task GuildMembersDownloaded([NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a guild's information is updated.
        /// </summary>
        /// <param name="oldGuild">The old guild information.</param>
        /// <param name="newGuild">The new guild information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task GuildUpdated([NotNull] SocketGuild oldGuild, [NotNull] SocketGuild newGuild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user joins a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserJoined([NotNull] SocketGuildUser user)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user leaves a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserLeft([NotNull] SocketGuildUser user)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user is banned from a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserBanned([NotNull] SocketUser user, [NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user is unbanned from a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserUnbanned([NotNull] SocketUser user, [NotNull] SocketGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user's information is updated.
        /// </summary>
        /// <param name="oldUser">The old user information.</param>
        /// <param name="newUser">The new user information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserUpdated([NotNull] SocketUser oldUser, [NotNull] SocketUser newUser)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a guild member updates their information, or the rich presence of the user is updated.
        /// </summary>
        /// <param name="oldMember">The old user information.</param>
        /// <param name="newMember">The new user information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task GuildMemberUpdated
        (
            [NotNull] SocketGuildUser oldMember,
            [NotNull] SocketGuildUser newMember
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user joins, leaves, or moves between voice channels.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserVoiceStateUpdated
        (
            [NotNull] SocketUser user,
            SocketVoiceState oldState,
            SocketVoiceState newState
        )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the bot connects to a or changes Discord voice server.
        /// </summary>
        /// <param name="voiceServer">The new server.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task VoiceServerUpdated([NotNull] SocketVoiceServer voiceServer)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the bot account is updated.
        /// </summary>
        /// <param name="oldSelf">The old user settings.</param>
        /// <param name="newSelf">The new user settings.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task SelfUpdated([NotNull] SocketSelfUser oldSelf, [NotNull] SocketSelfUser newSelf)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user starts typing.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="messageChannel">The channel the user is typing in.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task UserIsTyping([NotNull] SocketUser user, [NotNull] ISocketMessageChannel messageChannel)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user is added to a group chat.
        /// </summary>
        /// <param name="groupUser">The added user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ParticipantAdded([NotNull] SocketGroupUser groupUser)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user is removed from a group chat.
        /// </summary>
        /// <param name="groupUser">The removed user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task ParticipantRemoved([NotNull] SocketGroupUser groupUser)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the client is connected to the Discord gateway.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task Connected()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the client is disconnected from the Discord gateway.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnect; if any.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task Disconnected([CanBeNull] Exception exception)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when guild data has finished downloading.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task Ready()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a heartbeat is received from the Discord gateway.
        /// </summary>
        /// <param name="oldLatency">The old latency.</param>
        /// <param name="newLatency">The new latency.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task LatencyUpdated(int oldLatency, int newLatency)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a channel is created.
        /// </summary>
        /// <param name="channel">The created channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnChannelCreated(SocketChannel channel)
        {
            this.RunningEvents.Enqueue(ChannelCreated(channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a channel is deleted.
        /// </summary>
        /// <param name="channel">The created channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnChannelDeleted(SocketChannel channel)
        {
            this.RunningEvents.Enqueue(ChannelDeleted(channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a channel is updated.
        /// </summary>
        /// <param name="originalChannel">The original channel.</param>
        /// <param name="newChannel">The new channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnChannelUpdated(SocketChannel originalChannel, SocketChannel newChannel)
        {
            this.RunningEvents.Enqueue(ChannelUpdated(originalChannel, newChannel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnMessageReceived(SocketMessage message)
        {
            this.RunningEvents.Enqueue(MessageReceived(message));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a message is deleted.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            this.RunningEvents.Enqueue(MessageDeleted(message, channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when messages are deleted in bulk.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnMessagesBulkDeleted
        (
            IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
            ISocketMessageChannel channel
        )
        {
            this.RunningEvents.Enqueue(MessagesBulkDeleted(messages, channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a message is updated.
        /// </summary>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="newMessage">The new message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnMessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            SocketMessage newMessage,
            ISocketMessageChannel channel
        )
        {
            this.RunningEvents.Enqueue(MessageUpdated(oldMessage, newMessage, channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a reaction is added to a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            this.RunningEvents.Enqueue(ReactionAdded(message, channel, reaction));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a reaction is removed from a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="reaction">The reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            this.RunningEvents.Enqueue(ReactionRemoved(message, channel, reaction));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when all reactions are cleared from a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnReactionsCleared
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel
        )
        {
            this.RunningEvents.Enqueue(ReactionsCleared(message, channel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a role is created.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnRoleCreated(SocketRole role)
        {
            this.RunningEvents.Enqueue(RoleCreated(role));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a role is deleted.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnRoleDeleted(SocketRole role)
        {
            this.RunningEvents.Enqueue(RoleDeleted(role));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a role's information is updated.
        /// </summary>
        /// <param name="oldRole">The old role information.</param>
        /// <param name="newRole">The new role information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnRoleUpdated(SocketRole oldRole, SocketRole newRole)
        {
            this.RunningEvents.Enqueue(RoleUpdated(oldRole, newRole));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the bot joins a guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnJoinedGuild(SocketGuild guild)
        {
            this.RunningEvents.Enqueue(JoinedGuild(guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the bot leaves a guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnLeftGuild(SocketGuild guild)
        {
            this.RunningEvents.Enqueue(LeftGuild(guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a guild becomes available.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnGuildAvailable(SocketGuild guild)
        {
            this.RunningEvents.Enqueue(GuildAvailable(guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a guild becomes unavailable.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnGuildUnavailable(SocketGuild guild)
        {
            this.RunningEvents.Enqueue(GuildUnavailable(guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when information about offline guild members has finished downloading.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            this.RunningEvents.Enqueue(GuildMembersDownloaded(guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a guild's information is updated.
        /// </summary>
        /// <param name="oldGuild">The old guild information.</param>
        /// <param name="newGuild">The new guild information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnGuildUpdated(SocketGuild oldGuild, SocketGuild newGuild)
        {
            this.RunningEvents.Enqueue(GuildUpdated(oldGuild, newGuild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user joins a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserJoined(SocketGuildUser user)
        {
            this.RunningEvents.Enqueue(UserJoined(user));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user leaves a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserLeft(SocketGuildUser user)
        {
            this.RunningEvents.Enqueue(UserLeft(user));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user is banned from a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            this.RunningEvents.Enqueue(UserBanned(user, guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user is unbanned from a guild.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="guild">The guild.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserUnbanned(SocketUser user, SocketGuild guild)
        {
            this.RunningEvents.Enqueue(UserUnbanned(user, guild));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user's information is updated.
        /// </summary>
        /// <param name="oldUser">The old user information.</param>
        /// <param name="newUser">The new user information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserUpdated(SocketUser oldUser, SocketUser newUser)
        {
            this.RunningEvents.Enqueue(UserUpdated(oldUser, newUser));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a guild member updates their information, or the rich presence of the user is updated.
        /// </summary>
        /// <param name="oldMember">The old user information.</param>
        /// <param name="newMember">The new user information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnGuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            this.RunningEvents.Enqueue(GuildMemberUpdated(oldMember, newMember));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user joins, leaves, or moves between voice channels.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserVoiceStateUpdated
        (
            SocketUser user,
            SocketVoiceState oldState,
            SocketVoiceState newState
        )
        {
            this.RunningEvents.Enqueue(UserVoiceStateUpdated(user, oldState, newState));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the bot connects to a or changes Discord voice server.
        /// </summary>
        /// <param name="voiceServer">The new server.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnVoiceServerUpdated(SocketVoiceServer voiceServer)
        {
            this.RunningEvents.Enqueue(VoiceServerUpdated(voiceServer));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the bot account is updated.
        /// </summary>
        /// <param name="oldSelf">The old user settings.</param>
        /// <param name="newSelf">The new user settings.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnSelfUpdated(SocketSelfUser oldSelf, SocketSelfUser newSelf)
        {
            this.RunningEvents.Enqueue(SelfUpdated(oldSelf, newSelf));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user starts typing.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="messageChannel">The channel the user is typing in.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnUserIsTyping(SocketUser user, ISocketMessageChannel messageChannel)
        {
            this.RunningEvents.Enqueue(UserIsTyping(user, messageChannel));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user is added to a group chat.
        /// </summary>
        /// <param name="groupUser">The added user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnParticipantAdded(SocketGroupUser groupUser)
        {
            this.RunningEvents.Enqueue(ParticipantAdded(groupUser));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a user is removed from a group chat.
        /// </summary>
        /// <param name="groupUser">The removed user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnParticipantRemoved(SocketGroupUser groupUser)
        {
            this.RunningEvents.Enqueue(ParticipantRemoved(groupUser));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the client is connected to the Discord gateway.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnConnected()
        {
            this.RunningEvents.Enqueue(Connected());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when the client is disconnected from the Discord gateway.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnect; if any.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnDisconnected([CanBeNull] Exception exception)
        {
            this.RunningEvents.Enqueue(Disconnected(exception));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when guild data has finished downloading.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnReady()
        {
            this.RunningEvents.Enqueue(Ready());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Detaches and stores the handler for when a heartbeat is received from the Discord gateway.
        /// </summary>
        /// <param name="oldLatency">The old latency.</param>
        /// <param name="newLatency">The new latency.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task OnLatencyUpdated(int oldLatency, int newLatency)
        {
            this.RunningEvents.Enqueue(LatencyUpdated(oldLatency, newLatency));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnStartingAsync()
        {
            this.Client.ChannelCreated += OnChannelCreated;
            this.Client.ChannelDestroyed += OnChannelDeleted;
            this.Client.ChannelUpdated += OnChannelUpdated;
            this.Client.MessageReceived += OnMessageReceived;
            this.Client.MessageDeleted += OnMessageDeleted;
            this.Client.MessagesBulkDeleted += OnMessagesBulkDeleted;
            this.Client.MessageUpdated += OnMessageUpdated;
            this.Client.ReactionAdded += OnReactionAdded;
            this.Client.ReactionRemoved += OnReactionRemoved;
            this.Client.ReactionsCleared += OnReactionsCleared;
            this.Client.RoleCreated += OnRoleCreated;
            this.Client.RoleDeleted += OnRoleDeleted;
            this.Client.RoleUpdated += OnRoleUpdated;
            this.Client.JoinedGuild += OnJoinedGuild;
            this.Client.LeftGuild += OnLeftGuild;
            this.Client.GuildAvailable += OnGuildAvailable;
            this.Client.GuildUnavailable += OnGuildUnavailable;
            this.Client.GuildMembersDownloaded += OnGuildMembersDownloaded;
            this.Client.GuildUpdated += OnGuildUpdated;
            this.Client.UserJoined += OnUserJoined;
            this.Client.UserLeft += OnUserLeft;
            this.Client.UserBanned += OnUserBanned;
            this.Client.UserUnbanned += OnUserUnbanned;
            this.Client.UserUpdated += OnUserUpdated;
            this.Client.GuildMemberUpdated += OnGuildMemberUpdated;
            this.Client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
            this.Client.VoiceServerUpdated += OnVoiceServerUpdated;
            this.Client.CurrentUserUpdated += OnSelfUpdated;
            this.Client.UserIsTyping += OnUserIsTyping;
            this.Client.RecipientAdded += OnParticipantAdded;
            this.Client.RecipientRemoved += OnParticipantRemoved;
            this.Client.Connected += OnConnected;
            this.Client.Disconnected += OnDisconnected;
            this.Client.Ready += OnReady;
            this.Client.LatencyUpdated += OnLatencyUpdated;

            return base.OnStartingAsync();
        }

        /// <inheritdoc />
        protected override Task OnStoppingAsync()
        {
            this.Client.ChannelCreated -= OnChannelCreated;
            this.Client.ChannelDestroyed -= OnChannelDeleted;
            this.Client.ChannelUpdated -= OnChannelUpdated;
            this.Client.MessageReceived -= OnMessageReceived;
            this.Client.MessageDeleted -= OnMessageDeleted;
            this.Client.MessagesBulkDeleted -= OnMessagesBulkDeleted;
            this.Client.MessageUpdated -= OnMessageUpdated;
            this.Client.ReactionAdded -= OnReactionAdded;
            this.Client.ReactionRemoved -= OnReactionRemoved;
            this.Client.ReactionsCleared -= OnReactionsCleared;
            this.Client.RoleCreated -= OnRoleCreated;
            this.Client.RoleDeleted -= OnRoleDeleted;
            this.Client.RoleUpdated -= OnRoleUpdated;
            this.Client.JoinedGuild -= OnJoinedGuild;
            this.Client.LeftGuild -= OnLeftGuild;
            this.Client.GuildAvailable -= OnGuildAvailable;
            this.Client.GuildUnavailable -= OnGuildUnavailable;
            this.Client.GuildMembersDownloaded -= OnGuildMembersDownloaded;
            this.Client.GuildUpdated -= OnGuildUpdated;
            this.Client.UserJoined -= OnUserJoined;
            this.Client.UserLeft -= OnUserLeft;
            this.Client.UserBanned -= OnUserBanned;
            this.Client.UserUnbanned -= OnUserUnbanned;
            this.Client.UserUpdated -= OnUserUpdated;
            this.Client.GuildMemberUpdated -= OnGuildMemberUpdated;
            this.Client.UserVoiceStateUpdated -= OnUserVoiceStateUpdated;
            this.Client.VoiceServerUpdated -= OnVoiceServerUpdated;
            this.Client.CurrentUserUpdated -= OnSelfUpdated;
            this.Client.UserIsTyping -= OnUserIsTyping;
            this.Client.RecipientAdded -= OnParticipantAdded;
            this.Client.RecipientRemoved -= OnParticipantRemoved;
            this.Client.Connected -= OnConnected;
            this.Client.Disconnected -= OnDisconnected;
            this.Client.Ready -= OnReady;
            this.Client.LatencyUpdated -= OnLatencyUpdated;

            return base.OnStoppingAsync();
        }

        /// <inheritdoc />
        protected override async Task OnTickAsync(CancellationToken ct)
        {
            if (this.RunningEvents.TryDequeue(out var clientEvent))
            {
                if (clientEvent.IsCompleted)
                {
                    try
                    {
                        await clientEvent;
                    }
                    catch (Exception e)
                    {
                        // Nom nom nom
                        this.Log.Error("Error in client event handler.", e);
                    }
                }
                else
                {
                    this.RunningEvents.Enqueue(clientEvent);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }
    }
}
