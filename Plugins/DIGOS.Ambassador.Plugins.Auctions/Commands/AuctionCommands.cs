//
//  AuctionCommands.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using DIGOS.Ambassador.Plugins.Auctions.Services;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Commands;

/// <summary>
/// Defines auction-related commands.
/// </summary>
[Group("auction")]
[RequireContext(ChannelContext.Guild)]
public sealed partial class AuctionCommands : CommandGroup
{
    private readonly ICommandContext _context;
    private readonly UserService _userService;
    private readonly ServerService _serverService;
    private readonly AuctionsDatabaseContext _database;
    private readonly IDiscordRestInteractionAPI _interactionAPI;
    private readonly FeedbackService _feedbackService;
    private readonly AuctionDisplayService _auctionDisplay;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuctionCommands"/> class.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="serverService">The server service.</param>
    /// <param name="database">The database.</param>
    /// <param name="interactionAPI">The interaction API.</param>
    /// <param name="feedbackService">The feedback service.</param>
    /// <param name="auctionDisplay">The auction display service.</param>
    public AuctionCommands
    (
        ICommandContext context,
        UserService userService,
        ServerService serverService,
        AuctionsDatabaseContext database,
        IDiscordRestInteractionAPI interactionAPI,
        FeedbackService feedbackService,
        AuctionDisplayService auctionDisplay
    )
    {
        _context = context;
        _userService = userService;
        _serverService = serverService;
        _database = database;
        _interactionAPI = interactionAPI;
        _feedbackService = feedbackService;
        _auctionDisplay = auctionDisplay;
    }

    /// <summary>
    /// Creates a new auction.
    /// </summary>
    /// <param name="name">The name of the auction.</param>
    /// <param name="startBid">The starting bid.</param>
    /// <param name="endTime">The time at which the auction ends.</param>
    /// <param name="currency">The currency used when bidding.</param>
    /// <param name="timeExtension">The time with which the auction is extended when a bid is made.</param>
    /// <param name="minimumBid">The minimum bidding amount.</param>
    /// <param name="maximumBid">The maximum bidding amount.</param>
    /// <param name="bidCap">The bidding cap at which the auction automatically closes.</param>
    /// <param name="buyout">The buyout amount.</param>
    /// <param name="areBidsBinding">Indicates whether bids are binding.</param>
    /// <param name="privacy">The privacy options.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("create")]
    [Description("Creates a new auction.")]
    public async Task<Result<FeedbackMessage>> CreateAuctionAsync
    (
        [Description("The name of the auction.")] string name,
        [Description("The starting bid.")] decimal startBid,
        [Description("The time at which the auction ends.")] DateTimeOffset endTime,
        [Description("The currency used when bidding.")] string currency,
        [Description("Time time with which the auction is extended when a bid is made.")] TimeSpan? timeExtension = null,
        [Description("The minimum bidding amount.")] decimal? minimumBid = null,
        [Description("The maximum bidding amount.")] decimal? maximumBid = null,
        [Description("The bidding cap at which the auction automatically closes.")] decimal? bidCap = null,
        [Description("The buyout amount.")] decimal? buyout = null,
        [Description("Indicates whether bids are binding.")] bool areBidsBinding = true,
        [Description("The privacy options.")] AuctionPrivacy privacy = AuctionPrivacy.AnonymizeBidders
    )
    {
        if (startBid < 0)
        {
            return new UserError("The starting bid must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;

        if (endTime < now)
        {
            return new UserError("The end time cannot be in the past.");
        }

        if (minimumBid < 0)
        {
            return new UserError("The minimum bid must be greater than zero.");
        }

        if (maximumBid < minimumBid)
        {
            return new UserError("The maximum bid must be greater than or equal to the minimum bid.");
        }

        if (bidCap < 0)
        {
            return new UserError("The bidding cap must be greater than zero.");
        }

        if (bidCap <= startBid)
        {
            return new UserError("The bidding cap must be greater than the starting bid.");
        }

        if (buyout < 0)
        {
            return new UserError("The buyout must be greater than zero.");
        }

        if (buyout < startBid)
        {
            return new UserError("The buyout must be greater than or equal to the starting bid.");
        }

        _ = _context.TryGetGuildID(out var guildID);

        var getServer = await _serverService.GetOrRegisterServerAsync(guildID!.Value, this.CancellationToken);
        if (!getServer.IsDefined(out var server))
        {
            return Result<FeedbackMessage>.FromError(getServer);
        }

        server = _database.NormalizeReference(server);

        var nameExists = await _database.Auctions
            .AsNoTracking()
            .Where(a => a.Server.DiscordID == guildID)
            .AnyAsync(a => a.Name == name, this.CancellationToken);

        if (nameExists)
        {
            return new UserError("An auction with that name already exists.");
        }

        _ = _context.TryGetUserID(out var userID);

        var getUser = await _userService.GetOrRegisterUserAsync(userID!.Value, this.CancellationToken);
        if (!getUser.IsDefined(out var user))
        {
            return Result<FeedbackMessage>.FromError(getUser);
        }

        user = _database.NormalizeReference(user);

        var auction = new Auction
        (
            server,
            user,
            name,
            startBid,
            endTime,
            currency,
            timeExtension,
            minimumBid,
            maximumBid,
            bidCap,
            buyout,
            areBidsBinding,
            privacy
        );

        _database.Auctions.Add(auction);
        await _database.SaveChangesAsync(this.CancellationToken);

        return new FeedbackMessage("Auction created.", Color.MediumPurple);
    }

    /// <summary>
    /// Opens an auction for bids.
    /// </summary>
    /// <param name="auction">The auction to open.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("open")]
    [Description("Opens an auction for bids.")]
    public async Task<Result<FeedbackMessage>> OpenAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        if (auction.State is AuctionState.Open)
        {
            return new UserError("The auction is already open.");
        }

        auction.State = AuctionState.Open;
        await _database.SaveChangesAsync(this.CancellationToken);

        var updateDisplays = await _auctionDisplay.UpdateDisplaysAsync(auction, this.CancellationToken);
        if (!updateDisplays.IsSuccess)
        {
            var warnUser = await _feedbackService.SendContextualWarningAsync
            (
                "Failed to update auction displays. Some information may be out of date.",
                ct: this.CancellationToken
            );

            if (!warnUser.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(warnUser);
            }
        }

        return new FeedbackMessage("Auction opened.", Color.MediumPurple);
    }

    /// <summary>
    /// Displays a live-updating view of the specified auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("display")]
    [Description("Displays a live-updating view of the specified auction.")]
    public async Task<Result<FeedbackMessage>> DisplayAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        _ = _context.TryGetChannelID(out var channelID);
        var displayAuction = await _auctionDisplay.DisplayAuctionAsync
        (
            auction,
            channelID!.Value,
            this.CancellationToken
        );

        return displayAuction.IsSuccess
            ? new FeedbackMessage("Auction displayed.", Color.MediumPurple)
            : Result<FeedbackMessage>.FromError(displayAuction);
    }

    /// <summary>
    /// Hides the live-updating view of the specified auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("hide")]
    [Description("Hides the live-updating view of the specified auction.")]
    public async Task<Result<FeedbackMessage>> HideAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        _ = _context.TryGetChannelID(out var channelID);
        var hideAuction = await _auctionDisplay.HideAuctionAsync(auction, channelID!.Value, this.CancellationToken);

        return hideAuction.IsSuccess
            ? new FeedbackMessage("Auction hidden.", Color.MediumPurple)
            : Result<FeedbackMessage>.FromError(hideAuction);
    }

    /// <summary>
    /// Bids on an auction.
    /// </summary>
    /// <param name="auction">The auction to bid on.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("bid")]
    [Description("Bids on the given auction.")]
    [SuppressInteractionResponse(true)]
    [Ephemeral]
    public async Task<Result> SubmitAuctionBidAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        if (_context is not InteractionCommandContext interactionContext)
        {
            return (Result)await _feedbackService.SendContextualWarningAsync
            (
                "Interactive bids may only be placed via slash command.",
                ct: this.CancellationToken
            );
        }

        if (auction.State is not AuctionState.Open)
        {
            return (Result)await _feedbackService.SendContextualErrorAsync
            (
                "The auction is not open for bids.",
                ct: this.CancellationToken
            );
        }

        var now = DateTimeOffset.UtcNow;
        if (auction.EndTime <= now )
        {
            return (Result)await _feedbackService.SendContextualErrorAsync
            (
                "The auction has concluded.",
                ct: this.CancellationToken
            );
        }

        _ = _context.TryGetUserID(out var userID);

        if (auction.Bids.MaxBy(b => b.Amount)?.User.DiscordID == userID)
        {
            return (Result)await _feedbackService.SendContextualErrorAsync
            (
                "You're already the highest bidder.",
                ct: this.CancellationToken
            );
        }

        var confirmationMessage = "I UNDERSTAND";

        var components = new List<IMessageComponent>
        {
            new ActionRowComponent
            (
                new[]
                {
                    new TextInputComponent
                    (
                        "bid-amount",
                        TextInputStyle.Short,
                        "Bid Amount",
                        MinLength: 1,
                        default,
                        IsRequired: true,
                        Value: auction.GetNextBidSuggestion().ToString(CultureInfo.InvariantCulture),
                        default
                    )
                }
            )
        };

        if (auction.AreBidsBinding)
        {
            components.Add
            (
                new ActionRowComponent
                (
                    new[]
                    {
                        new TextInputComponent
                        (
                            "bid-confirmation",
                            TextInputStyle.Short,
                            "Confirmation",
                            MinLength: confirmationMessage.Length,
                            MaxLength: confirmationMessage.Length,
                            IsRequired: true,
                            default,
                            Placeholder: "Bids are binding. Please type \"I UNDERSTAND\" in this box to confirm."
                        )
                    }
                )
            );
        }

        var modal = new InteractionModalCallbackData
        (
            CustomIDHelpers.CreateModalIDWithState("auction-bid", auction.Name),
            "Place Bid",
            components
        );

        return await _interactionAPI.CreateInteractionResponseAsync
        (
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, new(modal)),
            ct: this.CancellationToken
        );
    }

    /// <summary>
    /// Manually extends the auction time by the given amount of time.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <param name="time">The time to extend the auction by.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("extend")]
    [Description("Manually extends the auction by the given amount of time.")]
    public async Task<Result<FeedbackMessage>> ExtendAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction,
        [Description("The time to extend the auction by.")] TimeSpan time
    )
    {
        if (auction.State is not AuctionState.Open)
        {
            return new UserError("Closed or concluded auctions cannot be extended.");
        }

        if (auction.EndTime + time < auction.EndTime)
        {
            return new UserError("Time extensions must be positive.");
        }

        auction.EndTime += time;
        await _database.SaveChangesAsync(this.CancellationToken);

        var updateDisplays = await _auctionDisplay.UpdateDisplaysAsync(auction, this.CancellationToken);
        if (!updateDisplays.IsSuccess)
        {
            var warnUser = await _feedbackService.SendContextualWarningAsync
            (
                "Failed to update auction displays. Some information may be out of date.",
                ct: this.CancellationToken
            );

            if (!warnUser.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(warnUser);
            }
        }

        return new FeedbackMessage("Time extended.", Color.MediumPurple);
    }

    /// <summary>
    /// Closes an auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("close")]
    [Description("Closes an auction.")]
    public async Task<Result<FeedbackMessage>> CloseAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        if (auction.State is not AuctionState.Open)
        {
            return new UserError("The auction is not open.");
        }

        auction.State = AuctionState.Closed;
        await _database.SaveChangesAsync(this.CancellationToken);

        var updateDisplays = await _auctionDisplay.UpdateDisplaysAsync(auction, this.CancellationToken);
        if (!updateDisplays.IsSuccess)
        {
            var warnUser = await _feedbackService.SendContextualWarningAsync
            (
                "Failed to update auction displays. Some information may be out of date.",
                ct: this.CancellationToken
            );

            if (!warnUser.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(warnUser);
            }
        }

        return new FeedbackMessage("Auction closed.", Color.MediumPurple);
    }

    /// <summary>
    /// Deletes the given auction.
    /// </summary>
    /// <param name="auction">The auction.</param>
    /// <returns>An asynchronous result representing the command execution.</returns>
    [Command("delete")]
    [Description("Deletes the given auction.")]
    public async Task<Result<FeedbackMessage>> DeleteAuctionAsync
    (
        [Autocomplete, Description("The name or ID of the auction.")] Auction auction
    )
    {
        _database.Auctions.Remove(auction);

        var hideDisplays = await _auctionDisplay.HideAuctionAsync(auction, ct: this.CancellationToken);
        if (!hideDisplays.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(hideDisplays);
        }

        await _database.SaveChangesAsync(this.CancellationToken);

        return new FeedbackMessage("Auction deleted.", Color.MediumPurple);
    }
}
