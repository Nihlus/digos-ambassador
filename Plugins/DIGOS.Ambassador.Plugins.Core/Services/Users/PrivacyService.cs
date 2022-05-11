//
//  PrivacyService.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using Zio;

namespace DIGOS.Ambassador.Plugins.Core.Services.Users;

/// <summary>
/// Handles privacy-related logic.
/// </summary>
public sealed class PrivacyService
{
    private readonly CoreDatabaseContext _database;
    private readonly FeedbackService _feedback;
    private readonly ContentService _content;

    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly IDiscordRestUserAPI _userAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacyService"/> class.
    /// </summary>
    /// <param name="database">The core database.</param>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="content">The content service.</param>
    /// <param name="channelAPI">The channel API.</param>
    /// <param name="userAPI">The user API.</param>
    public PrivacyService
    (
        CoreDatabaseContext database,
        FeedbackService feedback,
        ContentService content,
        IDiscordRestChannelAPI channelAPI,
        IDiscordRestUserAPI userAPI
    )
    {
        _database = database;
        _feedback = feedback;
        _content = content;
        _channelAPI = channelAPI;
        _userAPI = userAPI;
    }

    /// <summary>
    /// Sends a consent request from the given user.
    /// </summary>
    /// <param name="discordUser">The user to request consent from.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>An execution result.</returns>
    public async Task<Result> RequestConsentAsync(Snowflake discordUser, CancellationToken ct = default)
    {
        var embed = new Embed
        {
            Colour = _feedback.Theme.Warning,
            Description =
                "Hello there! This appears to be the first time you're using the bot (or you've not granted your " +
                "consent for it to store potentially sensitive or identifiable data about you).\n" +
                "\n" +
                "In order to use Amby and her commands, you need to give her your consent to store various data " +
                "about you. We need this consent in order to be compliant with data regulations in the European " +
                "Union (and it'd be rude not to ask!).\n" +
                "\n" +
                "In short, if you use the bot, we're going to be storing " +
                "stuff like your Discord ID, some messages, server IDs, etc. You can - and should! - read the " +
                "full privacy policy before you agree to anything. It's not very long (3 pages) and shouldn't take " +
                "more than five minutes to read through.\n" +
                "\n" +
                "Once you've read it, you can grant consent by running the `!privacy grant-consent` command over DM. " +
                "If you don't want to consent to anything, just don't use the bot :smiley:"
        };

        var openDM = await _userAPI.CreateDMAsync(discordUser, ct);
        if (openDM.IsSuccess)
        {
            var channel = openDM.Entity;
            var sendMessage = await _channelAPI.CreateMessageAsync(channel.ID, embeds: new[] { embed }, ct: ct);
            if (sendMessage.IsSuccess)
            {
                return await SendPrivacyPolicyAsync(channel.ID, ct);
            }
        }

        var warningMessage = "I was unable to send you some initial privacy policy information for " +
                             "first-time users of the bot. In order to use the bot, please read the " +
                             "privacy policy (which can be requested by running the `!privacy policy` command) " +
                             "and agree to it by sending `!privacy grant-consent` to the bot over DM.";

        var sendWarning = await _feedback.SendContextualWarningAsync(warningMessage, discordUser, ct: ct);
        return !sendWarning.IsSuccess
            ? Result.FromError(sendWarning)
            : Result.FromSuccess();
    }

    /// <summary>
    /// Sends the privacy policy to the given channel.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A task that must be awaited.</returns>
    public async Task<Result> SendPrivacyPolicyAsync(Snowflake channel, CancellationToken ct = default)
    {
        var result = _content.OpenLocalStream(UPath.Combine(UPath.Root, "Privacy", "PrivacyPolicy.pdf"));
        if (!result.IsSuccess)
        {
            var embed = new Embed
            {
                Colour = _feedback.Theme.FaultOrDanger,
                Description = "Oops. Something went wrong, and I couldn't grab the privacy policy. Please report " +
                              "this to the developer, don't agree to anything, and read it online instead.",
                Fields = new[] { new EmbedField("Privacy Policy", _content.PrivacyPolicyUri.ToString()) }
            };

            var sendError = await _channelAPI.CreateMessageAsync(channel, embeds: new[] { embed }, ct: ct);
            return sendError.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendError);
        }

        await using var privacyPolicy = result.Entity;
        var sendPolicy = await _channelAPI.CreateMessageAsync
        (
            channel,
            attachments: new List<OneOf<FileData, IPartialAttachment>>
            {
                new FileData("PrivacyPolicy.pdf", privacyPolicy)
            },
            ct: ct
        );

        return sendPolicy.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendPolicy);
    }

    /// <summary>
    /// Determines whether or not the given user has granted consent to store user data.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>true if the user has granted consent; Otherwise, false.</returns>
    [Pure]
    public async Task<bool> HasUserConsentedAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var consent = await _database.UserConsents.ServersideQueryAsync
        (
            q => q
                .Where(uc => uc.DiscordID == discordUser && uc.HasConsented)
                .SingleOrDefaultAsync(ct)
        );

        return consent is not null && consent.HasConsented;
    }

    /// <summary>
    /// Gets a consent entity for the given user.
    /// </summary>
    /// <param name="discordUser">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<UserConsent>> GetUserConsentAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var consent = await _database.UserConsents.ServersideQueryAsync
        (
            q => q
                .Where(uc => uc.DiscordID == discordUser)
                .SingleOrDefaultAsync(ct)
        );

        if (consent is not null)
        {
            return consent;
        }

        return new UserError("The given user doesn't have a consent entity.");
    }

    /// <summary>
    /// Grants consent to store user data for a given user.
    /// </summary>
    /// <param name="discordUser">The user that has granted consent.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    public async Task<Result<UserConsent>> GrantUserConsentAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var getConsent = await GetUserConsentAsync(discordUser, ct);

        UserConsent userConsent;
        if (!getConsent.IsSuccess)
        {
            userConsent = _database.CreateProxy<UserConsent>(discordUser);
            _database.UserConsents.Update(userConsent);

            userConsent.HasConsented = true;
        }
        else
        {
            userConsent = getConsent.Entity;
            userConsent.HasConsented = true;
        }

        await _database.SaveChangesAsync(ct);

        return userConsent;
    }

    /// <summary>
    /// Revokes consent to store user data for a given user.
    /// </summary>
    /// <param name="discordUser">The user that has revoked consent.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    public async Task<Result> RevokeUserConsentAsync
    (
        Snowflake discordUser,
        CancellationToken ct = default
    )
    {
        var getConsent = await GetUserConsentAsync(discordUser, ct);
        if (!getConsent.IsSuccess)
        {
            return new UserError("The user has not consented.");
        }

        var userConsent = getConsent.Entity;
        userConsent.HasConsented = false;

        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
