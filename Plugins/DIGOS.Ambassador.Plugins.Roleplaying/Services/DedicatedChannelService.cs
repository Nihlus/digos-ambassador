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
using System.Net;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Business logic for managing dedicated roleplay channels.
    /// </summary>
    public class DedicatedChannelService
    {
        private readonly RoleplayingDatabaseContext _database;
        private readonly ServerService _servers;
        private readonly RoleplayServerSettingsService _serverSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DedicatedChannelService"/> class.
        /// </summary>
        /// <param name="servers">The server service.</param>
        /// <param name="serverSettings">The server settings service.</param>
        /// <param name="database">The database context.</param>
        public DedicatedChannelService
        (
            ServerService servers,
            RoleplayServerSettingsService serverSettings,
            RoleplayingDatabaseContext database
        )
        {
            _servers = servers;
            _serverSettings = serverSettings;
            _database = database;
        }

        /// <summary>
        /// Creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result<IChannel>> CreateDedicatedChannelAsync(Roleplay roleplay)
        {
            if (!(await guildID.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return new UserError
                (
                    "I don't have permission to manage channels, so I can't create dedicated RP channels."
                );
            }

            var getExistingChannelResult = await GetDedicatedChannelAsync(roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return new UserError
                (
                    "The roleplay already has a dedicated channel."
                );
            }

            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return Result<IChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (settings.DedicatedRoleplayChannelsCategory is null)
            {
                return new UserError
                (
                    "No dedicated channel category has been configured."
                );
            }

            var categoryChannelCount = (await guildID.GetTextChannelsAsync())
                .Count(c => c.CategoryId == (ulong)settings.DedicatedRoleplayChannelsCategory);

            if (categoryChannelCount >= 50)
            {
                return new UserError
                (
                    "The server's roleplaying category has reached its maximum number of channels. Try " +
                    "contacting the server's owners and either removing some old roleplays or setting up " +
                    "a new category."
                );
            }

            Optional<ulong?> categoryId = (ulong?)settings.DedicatedRoleplayChannelsCategory;

            var dedicatedChannel = await guildID.CreateTextChannelAsync
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

            // This can fail in all manner of ways because of Discord.NET. Try, catch, etc...
            try
            {
                var resetPermissions = await ResetChannelPermissionsAsync(dedicatedChannel, roleplay);
                if (!resetPermissions.IsSuccess)
                {
                    return Result<IChannel>.FromError(resetPermissions);
                }
            }
            catch (HttpException hex) when (hex.HttpCode == HttpStatusCode.Forbidden)
            {
                return new UserError
                (
                    "Failed to update channel permissions. Does the bot have permissions to manage permissions on " +
                    "new channels?"
                );
            }
            catch (Exception ex)
            {
                await dedicatedChannel.DeleteAsync();
                return Result<IChannel>.FromError(ex);
            }

            await _database.SaveChangesAsync();

            return Result<IChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelWritabilityForUserAsync
        (
            Snowflake channelID,
            Snowflake userID,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = channelID.GetPermissionOverwrite(userID);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                sendMessages: isVisible ? PermValue.Allow : PermValue.Deny,
                addReactions: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await channelID.AddPermissionOverwriteAsync(userID, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelVisibilityForUserAsync
        (
            Snowflake channelID,
            Snowflake userID,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = channelID.GetPermissionOverwrite(userID);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await channelID.AddPermissionOverwriteAsync(userID, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> DeleteChannelAsync(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            // TODO: delete in discord

            roleplay.DedicatedChannelID = null;
            await _database.SaveChangesAsync();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Gets the channel dedicated to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Snowflake>> GetDedicatedChannelAsync(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            return roleplay.DedicatedChannelID.Value;
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="roleID">The role to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelVisibilityForRoleAsync
        (
            Snowflake channelID,
            Snowflake roleID,
            bool isVisible
        )
        {
            var permissions = OverwritePermissions.InheritAll;
            var existingOverwrite = channelID.GetPermissionOverwrite(roleID);
            if (!(existingOverwrite is null))
            {
                permissions = existingOverwrite.Value;
            }

            permissions = permissions.Modify
            (
                readMessageHistory: isVisible ? PermValue.Allow : PermValue.Deny,
                viewChannel: isVisible ? PermValue.Allow : PermValue.Deny
            );

            await channelID.AddPermissionOverwriteAsync(roleID, permissions);

            // Ugly hack - there seems to be some kind of race condition on Discord's end.
            await Task.Delay(20);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Revokes the given roleplay participant access to the given roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> RevokeUserAccessAsync(Roleplay roleplay, Snowflake userID)
        {
            var guild = participant.Guild;

            if (!(await guild.GetUserAsync(_client.CurrentUser.Id)).GuildPermissions.ManageChannels)
            {
                return new UserError
                (
                    "I don't have permission to manage channels, so I can't change permissions on dedicated RP channels."
                );
            }

            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            await channel.RemovePermissionOverwriteAsync(participant);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Retrieves the channel category that's set for the given server as the roleplay category.
        /// </summary>
        /// <param name="guildID">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Snowflake>> GetDedicatedChannelCategoryAsync(Snowflake guildID)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(guildID);
            if (!getServerResult.IsSuccess)
            {
                return Result<IChannel>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;
            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettingsResult.IsSuccess)
            {
                return Result<IChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!settings.DedicatedRoleplayChannelsCategory.HasValue)
            {
                return new UserError("Failed to retrieve a valid category.");
            }

            var categories = await guildID.GetCategoriesAsync();
            var category = categories.FirstOrDefault
            (
                c => c.Id == (ulong)settings.DedicatedRoleplayChannelsCategory.Value
            );

            if (category is null)
            {
                return new UserError("Failed to retrieve a valid category.");
            }

            return Result<IChannel>.FromSuccess(category);
        }

        /// <summary>
        /// Resets the channel permissions for the given roleplay to their default values.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ResetChannelPermissionsAsync(Roleplay roleplay)
        {
            var guild = channelID.Guild;

            // First, clear all overwrites from the channel
            var clear = await ClearChannelPermissionOverwrites(channelID);
            if (!clear.IsSuccess)
            {
                return clear;
            }

            // Then, ensure the bot has full access to the channel
            var botDiscordUser = await guild.GetUserAsync(_client.CurrentUser.Id);
            var allowAll = OverwritePermissions.AllowAll(channelID);

            await channelID.AddPermissionOverwriteAsync(botDiscordUser, allowAll);

            // Next, apply default role settings
            var configureDefault = await ConfigureDefaultUserRolePermissions(guild, channelID);
            if (!configureDefault.IsSuccess)
            {
                return configureDefault;
            }

            // Finally, set up permission overrides for participants
            var updateParticipants = await UpdateParticipantPermissionsAsync(guild, roleplay);
            if (!updateParticipants.IsSuccess)
            {
                return updateParticipants;
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Resets the channel permissions for the given roleplay to their default values.
        /// </summary>
        /// <param name="guildID">The guild.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ResetChannelPermissionsAsync(Roleplay roleplay)
        {
            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            return await ResetChannelPermissionsAsync(channel, roleplay);
        }

        /// <summary>
        /// Updates the permissions for the participants of the roleplay.
        /// </summary>
        /// <param name="guildID">The guild the roleplay is in.</param>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateParticipantPermissionsAsync(Roleplay roleplay)
        {
            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var discordUser = await guildID.GetUserAsync((ulong)participant.User.DiscordID);
                if (discordUser is null)
                {
                    continue;
                }

                await SetChannelWritabilityForUserAsync(channel, discordUser, roleplay.IsActive);
                await SetChannelVisibilityForUserAsync(channel, discordUser, roleplay.IsActive);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var server = getServer.Entity;

            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            // Finally, configure visibility for everyone
            IRole everyoneRole;
            if (settings.DefaultUserRole.HasValue)
            {
                var defaultRole = guildID.GetRole((ulong)settings.DefaultUserRole!.Value);
                everyoneRole = defaultRole ?? guildID.EveryoneRole;
            }
            else
            {
                everyoneRole = guildID.EveryoneRole;
            }

            return await SetChannelVisibilityForRoleAsync
            (
                channel,
                everyoneRole,
                roleplay.IsActive && roleplay.IsPublic
            );
        }

        /// <summary>
        /// Updates the name of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelNameAsync(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return new UserError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync(m => m.Name = $"{roleplay.Name}-rp");

            return Result.FromSuccess();
        }

        /// <summary>
        /// Updates the summary of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelSummaryAsync(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return new UserError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync
            (
                m => m.Topic = $"Dedicated roleplay channel for {roleplay.Name}. {roleplay.Summary}"
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Updates the NSFW status of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelNSFWStatus(Roleplay roleplay)
        {
            var guild = await _client.GetGuildAsync((ulong)roleplay.Server.DiscordID);
            if (guild is null)
            {
                return new UserError("Could not retrieve a valid guild.");
            }

            var getChannel = await GetDedicatedChannelAsync(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;
            await channel.ModifyAsync(m => m.IsNsfw = roleplay.IsNSFW);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Clears all channel permission overwrites from the given channel.
        /// </summary>
        /// <param name="channelID">The channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        private async Task<Result> ClearChannelPermissionOverwrites(Snowflake channelID)
        {
            var guild = channelID.Guild;

            foreach (var overwrite in channelID.PermissionOverwrites)
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
                            await channelID.AddPermissionOverwriteAsync(role, OverwritePermissions.InheritAll);
                        }
                        else
                        {
                            await channelID.RemovePermissionOverwriteAsync(role);
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

                        if (user.IsMe(_client))
                        {
                            continue;
                        }

                        await channelID.RemovePermissionOverwriteAsync(user);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return Result.FromSuccess();
        }

        private async Task<Result> ConfigureDefaultUserRolePermissions
        (
            Snowflake guildID,
            Snowflake channelID
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildID);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var server = getServer.Entity;
            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(server);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            var denyView = OverwritePermissions.InheritAll.Modify
            (
                viewChannel: PermValue.Deny,
                sendMessages: PermValue.Deny,
                addReactions: PermValue.Deny
            );

            // Configure visibility for everyone
            // viewChannel starts off as deny, since starting or stopping the RP will set the correct permissions.
            IRole defaultUserRole;
            if (settings.DefaultUserRole.HasValue)
            {
                var defaultRole = guildID.GetRole((ulong)settings.DefaultUserRole.Value);
                defaultUserRole = defaultRole ?? guildID.EveryoneRole;
            }
            else
            {
                defaultUserRole = guildID.EveryoneRole;
            }

            await channelID.AddPermissionOverwriteAsync(defaultUserRole, denyView);

            if (defaultUserRole != guildID.EveryoneRole)
            {
                // Also override @everyone so it can't see anything
                await channelID.AddPermissionOverwriteAsync(guildID.EveryoneRole, denyView);
            }

            return Result.FromSuccess();
        }
    }
}
