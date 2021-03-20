//
//  DroneService.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Drone.Extensions;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Drone.Services
{
    /// <summary>
    /// Contains business logic for creating and generating drone characters.
    /// </summary>
    public class DroneService
    {
        private readonly Random _random;
        private readonly CharacterDiscordService _characters;
        private readonly CharacterRoleService _characterRoles;
        private readonly ContentService _content;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="DroneService"/> class.
        /// </summary>
        /// <param name="characters">The character service.</param>
        /// <param name="random">An entropy source.</param>
        /// <param name="content">The content service.</param>
        /// <param name="characterRoles">The role service.</param>
        /// <param name="guildAPI">The user API.</param>
        public DroneService
        (
            CharacterDiscordService characters,
            Random random,
            ContentService content,
            CharacterRoleService characterRoles,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _characters = characters;
            _random = random;
            _content = content;
            _characterRoles = characterRoles;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Drones the given user, creating a randomized sharkdrone character for them and forcing them into that form.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<Result<Character>> DroneUserAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var createGeneratedIdentity = await GenerateDroneIdentityAsync(guildID, userID, ct);
            if (!createGeneratedIdentity.IsSuccess)
            {
                return Result<Character>.FromError(createGeneratedIdentity);
            }

            var (name, nickname) = createGeneratedIdentity.Entity;
            var generatedCharacterResult = await _characters.CreateCharacterAsync
            (
                guildID,
                userID,
                name,
                _content.GetRandomDroneAvatarUri().ToString(),
                nickname,
                _content.GetRandomDroneSummary(),
                _content.GetRandomDroneDescription(),
                new FemininePronounProvider().Family,
                ct
            );

            if (!generatedCharacterResult.IsSuccess)
            {
                return generatedCharacterResult;
            }

            var character = generatedCharacterResult.Entity;

            var getGuildRoles = await _guildAPI.GetGuildRolesAsync(guildID, ct);
            if (!getGuildRoles.IsSuccess)
            {
                return Result<Character>.FromError(getGuildRoles);
            }

            var guildRoles = getGuildRoles.Entity;
            var droneRole = guildRoles.FirstOrDefault
            (
                r => r.Name.Contains("Drone") || r.Name.Contains("Dronies")
            );

            if (!(droneRole is null))
            {
                var getCharacterRole = await _characterRoles.GetCharacterRoleAsync(guildID, droneRole.ID, ct);
                if (getCharacterRole.IsSuccess)
                {
                    var characterRole = getCharacterRole.Entity;
                    var setCharacterRole = await _characterRoles.SetCharacterRoleAsync
                    (
                        guildID,
                        userID,
                        character,
                        characterRole,
                        ct
                    );

                    if (!setCharacterRole.IsSuccess)
                    {
                        return Result<Character>.FromError(setCharacterRole);
                    }
                }
            }

            var becomeCharacterResult = await _characters.MakeCharacterCurrentAsync(guildID, userID, character, ct);

            return !becomeCharacterResult.IsSuccess
                ? Result<Character>.FromError(becomeCharacterResult)
                : character;
        }

        /// <summary>
        /// Generates a drone name.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The generated identity.</returns>
        private async Task<Result<(string Name, string Nickname)>> GenerateDroneIdentityAsync
        (
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        )
        {
            var getCharacters = await _characters.GetUserCharactersAsync(guildID, userID, ct);
            if (!getCharacters.IsSuccess)
            {
                return Result<(string Name, string Nickname)>.FromError(getCharacters);
            }

            var characters = getCharacters.Entity;
            var characterNames = characters.Select(c => c.Name).ToList();

            var getMember = await _guildAPI.GetGuildMemberAsync(guildID, userID, ct);
            if (!getMember.IsSuccess)
            {
                return Result<(string Name, string Nickname)>.FromError(getMember);
            }

            var member = getMember.Entity;
            if (!member.User.HasValue)
            {
                throw new InvalidOperationException();
            }

            string? characterName;
            string? displayName;
            while (true)
            {
                var serialNumber = _random.Next(0, 9999);
                var firstLetter = member.User.Value.Username.First();

                var generatedName = $"sharkdrone-{char.ToLowerInvariant(firstLetter)}{serialNumber}";
                if (characterNames.Contains(generatedName))
                {
                    continue;
                }

                characterName = generatedName;
                displayName = $"D-V: {char.ToUpperInvariant(firstLetter)}{serialNumber:D4}";

                break;
            }

            return (characterName, displayName);
        }
    }
}
