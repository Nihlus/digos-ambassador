//
//  TransformationSetCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Humanizer;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Transformations.CommandModules
{
    public partial class TransformationCommands
    {
        /// <summary>
        /// Contains setter commands for the transformation module.
        /// </summary>
        [Group("set")]
        [Description("Various setter commands for user options.")]
        public class TransformationSetCommands : CommandGroup
        {
            private readonly CharacterDiscordService _characters;
            private readonly TransformationService _transformation;
            private readonly ICommandContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="TransformationSetCommands"/> class.
            /// </summary>
            /// <param name="characters">The character service.</param>
            /// <param name="transformation">The transformation service.</param>
            /// <param name="context">The command context.</param>
            public TransformationSetCommands
            (
                CharacterDiscordService characters,
                TransformationService transformation,
                ICommandContext context
            )
            {
                _characters = characters;
                _transformation = transformation;
                _context = context;
            }

            /// <summary>
            /// Sets your current appearance as your current character's default one.
            /// </summary>
            [UsedImplicitly]
            [Command("default")]
            [Description("Saves your current appearance as your current character's default one.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetCurrentAppearanceAsDefaultAsync()
            {
                var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID
                );

                if (!getCurrentCharacterResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getCurrentCharacterResult);
                }

                var character = getCurrentCharacterResult.Entity;

                var setDefaultAppearanceResult =
                    await _transformation.SetCurrentAppearanceAsDefaultForCharacterAsync(character);
                if (!setDefaultAppearanceResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setDefaultAppearanceResult);
                }

                return new ConfirmationMessage
                (
                    "Current appearance saved as the default one of this character."
                );
            }

            /// <summary>
            /// Sets your default setting for opting in or out of transformations on servers you join.
            /// </summary>
            /// <param name="shouldOptIn">Whether or not to opt in by default.</param>
            [UsedImplicitly]
            [Command("default-opt-in")]
            [Description("Sets your default setting for opting in or out of transformations on servers you join.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetDefaultOptInOrOutOfTransformationsAsync(bool shouldOptIn = true)
            {
                var setDefaultOptInResult = await _transformation.SetDefaultOptInAsync(_context.User.ID, shouldOptIn);
                if (!setDefaultOptInResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setDefaultOptInResult);
                }

                return new ConfirmationMessage
                (
                    $"You're now opted {(shouldOptIn ? "in" : "out")} by default on new servers."
                );
            }

            /// <summary>
            /// Sets your default protection type for transformations on servers you join.
            /// </summary>
            /// <param name="protectionType">The protection type to use.</param>
            [UsedImplicitly]
            [Command("default-protection")]
            [Description("Sets your default protection type for transformations on servers you join.")]
            public async Task<Result<UserMessage>> SetDefaultProtectionTypeAsync(ProtectionType protectionType)
            {
                var setProtectionTypeResult = await _transformation.SetDefaultProtectionTypeAsync
                (
                    _context.User.ID,
                    protectionType
                );

                if (!setProtectionTypeResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setProtectionTypeResult);
                }

                return new ConfirmationMessage
                (
                    $"Default protection type set to \"{protectionType.Humanize()}\""
                );
            }

            /// <summary>
            /// Sets your protection type for transformations. Available types are Whitelist and Blacklist.
            /// </summary>
            /// <param name="protectionType">The protection type to use.</param>
            [UsedImplicitly]
            [Command("protection")]
            [Description("Sets your protection type for transformations.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetProtectionTypeAsync(ProtectionType protectionType)
            {
                var setProtectionTypeResult = await _transformation.SetServerProtectionTypeAsync
                (
                    _context.User.ID,
                    _context.GuildID.Value,
                    protectionType
                );

                if (!setProtectionTypeResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setProtectionTypeResult);
                }

                return new ConfirmationMessage
                (
                    $"Protection type set to \"{protectionType.Humanize()}\""
                );
            }
        }
    }
}
