//
//  UncachedMessageTypeReader.cs
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
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DIGOS.Ambassador.Discord.TypeReaders
{
    /// <summary>
    /// Reads an IMessage, downloading it if necessary.
    /// </summary>
    /// <typeparam name="T">A type implementing <see cref="IMessage"/>.</typeparam>
    public class UncachedMessageTypeReader<T> : MessageTypeReader<T> where T : class, IMessage
    {
        /// <inheritdoc />
        public override async Task<TypeReaderResult> ReadAsync
        (
            ICommandContext context,
            string input,
            IServiceProvider services
        )
        {
            var baseResult = await base.ReadAsync(context, input, services);
            if (baseResult.IsSuccess)
            {
                return baseResult;
            }

            if (!ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                // Maybe it's a message link?
                if (!Uri.IsWellFormedUriString(input, UriKind.RelativeOrAbsolute))
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
                }

                var uri = new Uri(input, UriKind.RelativeOrAbsolute);
                if (uri.Segments.Length < 3)
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
                }

                var messageIDSegment = uri.Segments[^1];
                var channelIDSegment = uri.Segments[^2];

                if (!ulong.TryParse(messageIDSegment.Trim('/'), out var messageID))
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
                }

                if (!ulong.TryParse(channelIDSegment.Trim('/'), out var channelID))
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
                }

                var channel = await context.Guild.GetChannelAsync(channelID);
                if (!(channel is ITextChannel textChannel))
                {
                    return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
                }

                if (await textChannel.GetMessageAsync(messageID) is T jumpUrlMessage)
                {
                    return TypeReaderResult.FromSuccess(jumpUrlMessage);
                }

                return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
            }

            if (await context.Channel.GetMessageAsync(id) is T directIDMessage)
            {
                return TypeReaderResult.FromSuccess(directIDMessage);
            }

            return TypeReaderResult.FromError(CommandError.Unsuccessful, "Message not found.");
        }
    }
}
