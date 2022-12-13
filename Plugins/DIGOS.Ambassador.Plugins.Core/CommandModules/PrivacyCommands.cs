//
//  PrivacyCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Attributes;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks
#pragma warning disable SA1118 // Parameter spans multiple lines, big strings

namespace DIGOS.Ambassador.Plugins.Core.CommandModules;

/// <summary>
/// Privacy-related commands (data storage, deleting requests, data protection, privacy contacts, etc).
/// </summary>
[UsedImplicitly]
[Group("privacy")]
[Description("Privacy-related commands (data storage, deleting requests, data protection, privacy contacts, etc).")]
public class PrivacyCommands : CommandGroup
{
    private readonly PrivacyService _privacy;
    private readonly FeedbackService _feedback;
    private readonly ICommandContext _context;
    private readonly IDiscordRestChannelAPI _channelAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacyCommands"/> class.
    /// </summary>
    /// <param name="feedback">The user feedback service.</param>
    /// <param name="privacy">The privacy service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="channelAPI">The channel API.</param>
    public PrivacyCommands
    (
        FeedbackService feedback,
        PrivacyService privacy,
        ICommandContext context,
        IDiscordRestChannelAPI channelAPI
    )
    {
        _feedback = feedback;
        _privacy = privacy;
        _context = context;
        _channelAPI = channelAPI;
    }

    /// <summary>
    /// Requests a copy of the privacy policy.
    /// </summary>
    [UsedImplicitly]
    [Command("policy")]
    [Description("Requests a copy of the privacy policy.")]
    [RequireContext(ChannelContext.DM)]
    [PrivacyExempt]
    public async Task<IResult> RequestPolicyAsync()
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }

        await _privacy.SendPrivacyPolicyAsync(channelID.Value);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Grants consent to store user data.
    /// </summary>
    [UsedImplicitly]
    [Command("grant-consent")]
    [Description("Grants consent to store user data.")]
    [RequireContext(ChannelContext.DM)]
    [PrivacyExempt]
    public async Task<Result<FeedbackMessage>> GrantConsentAsync()
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var grantResult = await _privacy.GrantUserConsentAsync(userID.Value);
        return grantResult.IsSuccess
            ? new FeedbackMessage("Thank you! Enjoy using the bot :smiley:", _feedback.Theme.Secondary)
            : Result<FeedbackMessage>.FromError(grantResult);
    }

    /// <summary>
    /// Revokes consent to store user data.
    /// </summary>
    [UsedImplicitly]
    [Command("revoke-consent")]
    [Description("Revokes consent to store user data.")]
    [RequireContext(ChannelContext.DM)]
    [PrivacyExempt]
    public async Task<Result<FeedbackMessage>> RevokeConsentAsync()
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var revokeResult = await _privacy.RevokeUserConsentAsync(userID.Value);
        if (!revokeResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(revokeResult);
        }

        return new FeedbackMessage
        (
            "Consent revoked - no more information will be stored about you from now on. If you would like to "
            + "delete your existing data, or get a copy of it, please contact the privacy contact individual (use "
            + "!privacy contact to get their contact information).",
            _feedback.Theme.Secondary
        );
    }

    /// <summary>
    /// Displays contact information for the privacy contact person.
    /// </summary>
    [UsedImplicitly]
    [Command("contact")]
    [Description("Displays contact information for the privacy contact person.")]
    [RequireContext(ChannelContext.DM)]
    [PrivacyExempt]
    public async Task<IResult> DisplayContactAsync()
    {
        if (!_context.TryGetChannelID(out var channelID))
        {
            throw new InvalidOperationException();
        }
        const string avatarURL = "https://i.imgur.com/2E334jS.jpg";
        var embed = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = "Privacy Contact",
            Author = new EmbedAuthor("Jarl Gullberg", IconUrl: avatarURL, Url: "https://github.com/Nihlus/"),
            Thumbnail = new EmbedThumbnail(avatarURL),
            Fields = new[]
            {
                new EmbedField("Email", "jarl.gullberg@gmail.com", true),
                new EmbedField("Discord", "Jax#7487", true)
            },
            Footer = new EmbedFooter
            (
                "Not your contact person? Edit the source of your instance with the correct information."
            )
        };

        var sendEmbed = await _channelAPI.CreateMessageAsync
        (
            channelID.Value,
            embeds: new[] { embed },
            ct: this.CancellationToken
        );

        return sendEmbed.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendEmbed);
    }
}
