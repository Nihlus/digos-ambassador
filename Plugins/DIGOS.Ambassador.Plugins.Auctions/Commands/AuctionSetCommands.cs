//
//  AuctionSetCommands.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Model.Data;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Auctions.Commands;

public partial class AuctionCommands
{
    /// <summary>
    /// Defines property-setting commands for auctions.
    /// </summary>
    [Group("set")]
    [RequireContext(ChannelContext.Guild)]
    public sealed class AuctionSetCommands : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly AuctionsDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuctionSetCommands"/> class.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="database">The database.</param>
        public AuctionSetCommands(ICommandContext context, AuctionsDatabaseContext database)
        {
            _context = context;
            _database = database;
        }

        /// <summary>
        /// Sets the name of the auction.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="name">The new name.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("name")]
        [Description("Sets the name of the auction.")]
        public async Task<Result<FeedbackMessage>> SetNameAsync([Autocomplete] Auction auction, string name)
        {
            if (auction.State is AuctionState.Concluded)
            {
                return new UserError("The name of concluded auctions cannot be modified.");
            }

            if (name.IsNullOrWhitespace())
            {
                return new UserError("The new name must not be composed of only whitespace.");
            }

            _ = _context.TryGetGuildID(out var guildID);

            var nameExists = await _database.Auctions
                .AsNoTracking()
                .Where(a => a.Server.DiscordID == guildID)
                .AnyAsync(a => a.Name == name, this.CancellationToken);

            if (nameExists)
            {
                return new UserError("An auction with that name already exists.");
            }

            auction.Name = name;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Name set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the starting bid of the auction.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="startBid">The new starting bid.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("start-bid")]
        [Description("Sets the starting bid of the auction.")]
        public async Task<Result<FeedbackMessage>> SetStartBidAsync([Autocomplete] Auction auction, decimal startBid)
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The starting bid of open or concluded auctions cannot be modified.");
            }

            if (startBid < 0)
            {
                return new UserError("The starting bid must be greater than zero.");
            }

            auction.StartBid = startBid;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Starting bid set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the time at which the auction ends.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="endTime">The new end time.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("end-time")]
        [Description("Sets the time at which the auction ends.")]
        public async Task<Result<FeedbackMessage>> SetEndTimeAsync
        (
            [Autocomplete] Auction auction,
            DateTimeOffset endTime
        )
        {
            endTime = endTime.ToUniversalTime();

            if (auction.State is not AuctionState.Closed)
            {
                return new UserError
                (
                    "The end time of open or concluded auctions cannot be modified. If you want to add time to an "
                    + "open auction, use the \"extend\" command instead."
                );
            }

            var now = DateTimeOffset.UtcNow;

            if (endTime < now)
            {
                return new UserError("The new end time cannot be in the past.");
            }

            auction.EndTime = endTime;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("End time set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the currency used when bidding on the auction.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="currency">The new currency.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("currency")]
        [Description("Sets the currency used when bidding on the auction.")]
        public async Task<Result<FeedbackMessage>> SetCurrencyAsync([Autocomplete] Auction auction, string currency)
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The currency of open or concluded auctions cannot be modified.");
            }

            auction.Currency = currency;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Currency set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the time with which the auction is extended when a bid is placed.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="timeExtension">The time to extend the auction by.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("time-extension")]
        [Description("Sets the time with which the auction is extended when a bid is placed.")]
        public async Task<Result<FeedbackMessage>> SetTimeExtensionAsync
        (
            [Autocomplete] Auction auction,
            TimeSpan? timeExtension
        )
        {
            if (auction.State is AuctionState.Concluded)
            {
                return new UserError("The extension time of concluded auctions cannot be modified.");
            }

            auction.TimeExtension = timeExtension;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Currency set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the minimum bid.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="minimumBid">The new minimum bid.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("minimum-bid")]
        [Description("Sets the minimum bid.")]
        public async Task<Result<FeedbackMessage>> SetMinimumBidAsync
        (
            [Autocomplete] Auction auction,
            decimal? minimumBid
        )
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The minimum bid of open or concluded auctions cannot be modified.");
            }

            if (minimumBid < 0)
            {
                return new UserError("The minimum bid must be greater than zero.");
            }

            auction.MinimumBid = minimumBid;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Minimum bid set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the maximum bid.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="maximumBid">The new maximum bid.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("maximum-bid")]
        [Description("Sets the maximum bid.")]
        public async Task<Result<FeedbackMessage>> SetMaximumBidAsync
        (
            [Autocomplete] Auction auction,
            decimal? maximumBid
        )
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The maximum bid of open or concluded auctions cannot be modified.");
            }

            if (maximumBid < 0)
            {
                return new UserError("The maximum bid must be greater than zero.");
            }

            if (maximumBid < auction.MinimumBid)
            {
                return new UserError("The maximum bid must be greater than or equal to the minimum bid.");
            }

            auction.MaximumBid = maximumBid;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Maximum bid set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the bidding cap at which the auction automatically closes.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="bidCap">The new bidding cap.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("bid-cap")]
        [Description("Sets the bidding cap at which the auction automatically closes.")]
        public async Task<Result<FeedbackMessage>> SetBidCapAsync([Autocomplete] Auction auction, decimal? bidCap)
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The bidding cap of open or concluded auctions cannot be modified.");
            }

            if (bidCap < 0)
            {
                return new UserError("The bidding cap must be greater than zero.");
            }

            if (bidCap <= auction.StartBid)
            {
                return new UserError("The bidding cap must be greater than the starting bid.");
            }

            auction.BidCap = bidCap;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Bidding cap set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets the buyout price of the auction.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="buyout">The buyout price.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("buyout")]
        [Description("Sets the buyout price of the auction.")]
        public async Task<Result<FeedbackMessage>> SetBuyoutAsync([Autocomplete] Auction auction, decimal? buyout)
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("The buyout price of open or concluded auctions cannot be modified.");
            }

            if (buyout < 0)
            {
                return new UserError("The buyout must be greater than zero.");
            }

            if (buyout < auction.StartBid)
            {
                return new UserError("The buyout must be greater than or equal to the starting bid.");
            }

            auction.Buyout = buyout;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage("Buyout price set.", Color.MediumPurple);
        }

        /// <summary>
        /// Sets whether bids on the auction are binding.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="areBidsBinding">Whether bids are binding.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("binding-bids")]
        [Description("Sets whether bids on the auction are binding.")]
        public async Task<Result<FeedbackMessage>> SetAreBidsBindingAsync
        (
            [Autocomplete] Auction auction,
            bool areBidsBinding
        )
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("Whether bids are binding can't be changed on open or concluded auctions.");
            }

            if (auction.AreBidsBinding == areBidsBinding)
            {
                return new UserError($"Bids are already {(areBidsBinding ? "binding" : "not binding")}.");
            }

            auction.AreBidsBinding = areBidsBinding;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage
            (
                $"Bidding set to {(areBidsBinding ? "binding" : "not binding")}.",
                Color.MediumPurple
            );
        }

        /// <summary>
        /// Sets the privacy options for the auction.
        /// </summary>
        /// <param name="auction">The auction.</param>
        /// <param name="privacy">The privacy options.</param>
        /// <returns>An asynchronous result representing the command execution.</returns>
        [Command("privacy")]
        [Description("Sets the privacy options for the auction.")]
        public async Task<Result<FeedbackMessage>> SetPrivacyAsync
        (
            [Autocomplete] Auction auction,
            AuctionPrivacy privacy
        )
        {
            if (auction.State is not AuctionState.Closed)
            {
                return new UserError("Privacy settings can't be changed on open or concluded auctions.");
            }

            if (auction.Privacy == privacy)
            {
                return new UserError($"Auction privacy is already set to \"{privacy.Humanize()}\".");
            }

            auction.Privacy = privacy;
            await _database.SaveChangesAsync(this.CancellationToken);

            return new FeedbackMessage($"Auction privacy set to \"{privacy.Humanize()}\".", Color.MediumPurple);
        }
    }
}
