//
//  DedicatedChannelService.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Discord;
using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Business logic for managing dedicated roleplay channels.
    /// </summary>
    [PublicAPI]
    public class DedicatedChannelService
    {
        private readonly RoleplayingDatabaseContext _database;
        private readonly IDiscordClient _client;
        private readonly ServerService _servers;
        private readonly RoleplayServerSettingsService _serverSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DedicatedChannelService"/> class.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="serverSettings">The server settings service.</param>
        /// <param name="database">The database context.</param>
        public DedicatedChannelService
        (
            IDiscordClient client,
            ServerService servers,
            RoleplayServerSettingsService serverSettings,
            RoleplayingDatabaseContext database
        )
        {
            _client = client;
            _servers = servers;
            _serverSettings = serverSettings;
            _database = database;
        }

        /// <summary>
        /// Creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="guild">The guild in which the request was made.</param>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<ITextChannel>> CreateDedicatedChannelAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServerResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            if (!(await guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return CreateEntityResult<ITextChannel>.FromError
                (
                    "I don't have permission to manage channels, so I can't create dedicated RP channels."
                );
            }

            var getExistingChannelResult = await GetDedicatedChannelAsync(guild, roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError
                (
                    "The roleplay already has a dedicated channel."
                );
            }

            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!(settings.DedicatedRoleplayChannelsCategory is null))
            {
                var categoryChannelCount = (await guild.GetTextChannelsAsync())
                    .Count(c => c.CategoryId == (ulong)settings.DedicatedRoleplayChannelsCategory);

                if (categoryChannelCount >= 50)
                {
                    return CreateEntityResult<ITextChannel>.FromError
                    (
                        "The server's roleplaying category has reached its maximum number of channels. Try " +
                        "contacting the server's owners and either removing some old roleplays or setting up " +
                        "a new category."
                    );
                }
            }

            Optional<ulong?> categoryId;
            if (settings.DedicatedRoleplayChannelsCategory is null)
            {
                categoryId = null;
            }
            else
            {
                categoryId = (ulong?)settings.DedicatedRoleplayChannelsCategory;
            }

            var dedicatedChannel = await guild.CreateTextChannelAsync
            (
                $"{roleplay.Name}-rp",
                properties =>
                {
                    properties.CategoryId = categoryId;
                    properties.IsNsfw = roleplay.IsNSFW;
                    properties.Topic = $"Dedicated roleplay channel for {roleplay.Name}. {roleplay.Summary}";
                }
            );

            roleplay.DedicatedChannelID = (long)dedicatedChannel.Id;

            var resetPermissions = await ResetChannelPermissionsAsync(guild, roleplay);
            if (!resetPermissions.IsSuccess)
            {
                return CreateEntityResult<ITextChannel>.FromError(resetPermissions);
            }

            await _database.SaveChangesAsync();
            return CreateEntityResult<ITextChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetChannelWritabilityForUserAsync
        (
            IGuildChannel dedicatedChannel,
            IUser participant,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(participant);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                sendMessages: isVisible ? PermValue.Allow : PermValue.Deny,
                addReactions: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(participant, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetChannelVisibilityForUserAsync
        (
            IGuildChannel dedicatedChannel,
            IUser participant,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(participant);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(participant, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="guild">The context in which the request was made.</param>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteChannelAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return DeleteEntityResult.FromError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            var getDedicatedChannelResult = await GetDedicatedChannelAsync(guild, roleplay);
            if (getDedicatedChannelResult.IsSuccess)
            {
                await getDedicatedChannelResult.Entity.DeleteAsync();
            }

            roleplay.DedicatedChannelID = null;
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the channel dedicated to the given roleplay.
        /// </summary>
        /// <param name="guild">The guild that contains the channel.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<ITextChannel>> GetDedicatedChannelAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return RetrieveEntityResult<ITextChannel>.FromError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            var guildChannel = await guild.GetTextChannelAsync((ulong)roleplay.DedicatedChannelID.Value);
            if (!(guildChannel is null))
            {
                return RetrieveEntityResult<ITextChannel>.FromSuccess(guildChannel);
            }

            return RetrieveEntityResult<ITextChannel>.FromError
            (
                "Attempted to delete a channel, but it appears to have been deleted."
            );
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="dedicatedChannel">The roleplay's dedicated channel.</param>
        /// <param name="role">The role to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetChannelVisibilityForRoleAsync
        (
            IGuildChannel dedicatedChannel,
            IRole role,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = dedicatedChannel.GetPermissionOverwrite(role);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(role, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Revokes the given roleplay participant access to the given roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="participant">The participant to grant access to.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RevokeUserAccessAsync
        (
            Roleplay roleplay,
            IGuildUser participant
        )
        {
            var guild = participant.Guild;

            if (!(await guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return ModifyEntityResult.FromError
                (
                    "I don't have permission to manage channels, so I can't change permissions on dedicated RP channels."
                );
            }

            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            await channel.RemovePermissionOverwriteAsync(participant);
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Retrieves the channel category that's set for the given server as the roleplay category.
        /// </summary>
        /// <param name="discordServer">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<ICategoryChannel>> GetDedicatedChannelCategoryAsync
        (
            IGuild discordServer
        )
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(discordServer);
            if (!getServerResult.IsSuccess)
            {
                return RetrieveEntityResult<ICategoryChannel>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;
            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return RetrieveEntityResult<ICategoryChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!settings.DedicatedRoleplayChannelsCategory.HasValue)
            {
                return RetrieveEntityResult<ICategoryChannel>.FromError("Failed to retrieve a valid category.");
            }

            var categories = await discordServer.GetCategoriesAsync();
            var category = categories.FirstOrDefault
            (
                c => c.Id == (ulong)settings.DedicatedRoleplayChannelsCategory.Value
            );

            if (category is null)
            {
                return RetrieveEntityResult<ICategoryChannel>.FromError("Failed to retrieve a valid category.");
            }

            return RetrieveEntityResult<ICategoryChannel>.FromSuccess(category);
        }

        /// <summary>
        /// Resets the channel permissions for the given roleplay to their default values.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ResetChannelPermissionsAsync(IGuild guild, Roleplay roleplay)
        {
            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            // First, clear all overwrites from the channel
            var clear = await ClearChannelPermissionOverwrites(channel);
            if (!clear.IsSuccess)
            {
                return clear;
            }

            // Next, apply default role settings
            var configureDefault = await ConfigureDefaultUserRolePermissions(guild, channel);
            if (!configureDefault.IsSuccess)
            {
                return configureDefault;
            }

            // Finally, ensure the bot has full access to the channel
            var botDiscordUser = await guild.GetUserAsync(_client.CurrentUser.Id);
            await SetChannelWritabilityForUserAsync(channel, botDiscordUser, true);
            await SetChannelVisibilityForUserAsync(channel, botDiscordUser, true);

            // Then, set up permission overrides for participants
            var updateParticipants = await UpdateParticipantPermissionsAsync(guild, roleplay);
            if (!updateParticipants.IsSuccess)
            {
                return updateParticipants;
            }

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the permissions for the participants of the roleplay.
        /// </summary>
        /// <param name="guild">The guild the roleplay is in.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateParticipantPermissionsAsync
        (
            IGuild guild,
            Roleplay roleplay
        )
        {
            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var discordUser = await guild.GetUserAsync((ulong)participant.User.DiscordID);
                if (discordUser is null)
                {
                    continue;
                }

                await SetChannelWritabilityForUserAsync(channel, discordUser, roleplay.IsActive);
                await SetChannelVisibilityForUserAsync(channel, discordUser, roleplay.IsActive);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var server = getServer.Entity;

            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            // Finally, configure visibility for everyone
            IRole everyoneRole;
            if (settings.DefaultUserRole.HasValue)
            {
                var defaultRole = guild.GetRole((ulong)settings.DefaultUserRole!.Value);
                everyoneRole = defaultRole ?? guild.EveryoneRole;
            }
            else
            {
                everyoneRole = guild.EveryoneRole;
            }

            await SetChannelVisibilityForRoleAsync(channel, everyoneRole, roleplay.IsActive);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the name of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateChannelNameAsync(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync(m => m.Name = $"{roleplay.Name}-rp");

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the summary of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateChannelSummaryAsync(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync
            (
                m => m.Topic = $"Dedicated roleplay channel for {roleplay.Name}. {roleplay.Summary}"
            );

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the NSFW status of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateChannelNSFWStatus(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return ModifyEntityResult.FromError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(guild, roleplay);
            if (!getChannel.IsSuccess)
            {
                return ModifyEntityResult.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync(m => m.IsNsfw = roleplay.IsNSFW);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears all channel permission overwrites from the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        private static async Task<ModifyEntityResult> ClearChannelPermissionOverwrites(IGuildChannel channel)
        {
            var guild = channel.Guild;

            foreach (var overwrite in channel.PermissionOverwrites)
            {
                switch (overwrite.TargetType)
                {
                    case PermissionTarget.Role:
                    {
                        var role = guild.GetRole(overwrite.TargetId);
                        if (role is null)
                        {
                            continue;
                        }

                        if (role.Id == guild.EveryoneRole.Id)
                        {
                            await channel.AddPermissionOverwriteAsync(role, OverwritePermissions.InheritAll);
                        }
                        else
                        {
                            await channel.RemovePermissionOverwriteAsync(role);
                        }

                        break;
                    }
                    case PermissionTarget.User:
                    {
                        var user = await guild.GetUserAsync(overwrite.TargetId);
                        if (user is null)
                        {
                            continue;
                        }

                        await channel.RemovePermissionOverwriteAsync(user);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return ModifyEntityResult.FromSuccess();
        }

        private async Task<ModifyEntityResult> ConfigureDefaultUserRolePermissions
        (
            IGuild guild,
            IGuildChannel dedicatedChannel
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var server = getServer.Entity;
            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            // Configure visibility for everyone
            // viewChannel starts off as deny, since starting or stopping the RP will set the correct permissions.
            IRole everyoneRole;
            if (settings.DefaultUserRole.HasValue)
            {
                var defaultRole = guild.GetRole((ulong)settings.DefaultUserRole.Value);
                everyoneRole = defaultRole ?? guild.EveryoneRole;
            }
            else
            {
                everyoneRole = guild.EveryoneRole;
            }

            var everyonePermissions = OverwritePermissions.InheritAll.Modify
            (
                viewChannel: PermValue.Deny,
                sendMessages: PermValue.Deny,
                addReactions: PermValue.Deny
            );

            await dedicatedChannel.AddPermissionOverwriteAsync(everyoneRole, everyonePermissions);

            return ModifyEntityResult.FromSuccess();
        }
    }
}
