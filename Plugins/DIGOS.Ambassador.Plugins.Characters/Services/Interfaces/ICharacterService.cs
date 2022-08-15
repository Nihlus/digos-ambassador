//
//  ICharacterService.cs
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;

/// <summary>
/// Represents the public interface of a character service.
/// </summary>
public interface ICharacterService
{
    /// <summary>
    /// Creates a character with the given parameters.
    /// </summary>
    /// <param name="user">The owner of the character..</param>
    /// <param name="server">The server the owner is on.</param>
    /// <param name="name">The name of the character.</param>
    /// <param name="avatarUrl">The character's avatar url.</param>
    /// <param name="nickname">The nickname that should be applied to the user when the character is active.</param>
    /// <param name="summary">The summary of the character.</param>
    /// <param name="description">The full description of the character.</param>
    /// <param name="pronounFamily">The pronoun family of the character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have been successful.</returns>
    Task<Result<Character>> CreateCharacterAsync
    (
        User user,
        Server server,
        string name,
        string? avatarUrl = null,
        string? nickname = null,
        string? summary = null,
        string? description = null,
        string? pronounFamily = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets the characters belonging to the given user on the given server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="user">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The characters.</returns>
    Task<IReadOnlyList<Character>> GetUserCharactersAsync
    (
        Server server,
        User user,
        CancellationToken ct = default
    );

    /// <summary>
    /// Deletes the given character.
    /// </summary>
    /// <param name="character">The character to delete.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A deletion result which may or may not have succeeded.</returns>
    Task<Result> DeleteCharacterAsync(Character character, CancellationToken ct = default);

    /// <summary>
    /// This method searches for the best matching character given an owner and a name. If no owner is provided, then
    /// the global list is searched for a unique name. If no match can be found, a failed result is returned.
    /// </summary>
    /// <param name="server">The server the user is on.</param>
    /// <param name="user">The owner of the character, if any.</param>
    /// <param name="name">The name of the character, if any.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    Task<Result<Character>> GetBestMatchingCharacterAsync
    (
        Server server,
        User? user,
        string? name,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets the current character a user has assumed the form of.
    /// </summary>
    /// <param name="user">The user to get the current character of.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    Task<Result<Character>> GetCurrentCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets a character by its given name.
    /// </summary>
    /// <param name="server">The server that the character is on.</param>
    /// <param name="name">The name of the character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    Task<Result<Character>> GetCharacterByNameAsync
    (
        Server server,
        string name,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets a character belonging to a given user by a given name.
    /// </summary>
    /// <param name="user">The user to get the character from.</param>
    /// <param name="server">The server that the user is on.</param>
    /// <param name="name">The name of the character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    Task<Result<Character>> GetUserCharacterByNameAsync
    (
        User user,
        Server server,
        string name,
        CancellationToken ct = default
    );

    /// <summary>
    /// Makes the given character current on the given server.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="character">The character to make current.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    Task<Result> MakeCharacterCurrentAsync
    (
        User user,
        Server server,
        Character character,
        CancellationToken ct = default
    );

    /// <summary>
    /// Clears any current characters in the server from the given user, returning them to their default form.
    /// </summary>
    /// <param name="user">The user to clear the characters from.</param>
    /// <param name="server">The server to clear the characters on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    Task<Result> ClearCurrentCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    );

    /// <summary>
    /// Determines whether or not the given user has an active character on the given server.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>true if the user has an active character on the server; otherwise, false.</returns>
    Task<bool> HasCurrentCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves the given user's default character.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    Task<Result<Character>> GetDefaultCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the default character of a user.
    /// </summary>
    /// <param name="user">The user to set the default character of.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="character">The new default character.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetDefaultCharacterAsync
    (
        User user,
        Server server,
        Character character,
        CancellationToken ct = default
    );

    /// <summary>
    /// Clears the default character from the given user.
    /// </summary>
    /// <param name="user">The user to clear the default character of.</param>
    /// <param name="server">The server the user is on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> ClearDefaultCharacterAsync
    (
        User user,
        Server server,
        CancellationToken ct = default
    );

    /// <summary>
    /// Transfers ownership of the named character to the specified user.
    /// </summary>
    /// <param name="newOwner">The new owner.</param>
    /// <param name="server">The server to scope the character search to.</param>
    /// <param name="character">The character to transfer.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An execution result which may or may not have succeeded.</returns>
    Task<Result> TransferCharacterOwnershipAsync
    (
        User newOwner,
        Server server,
        Character character,
        CancellationToken ct = default
    );

    /// <summary>
    /// Determines whether or not the given character name is unique for a given user.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="server">The server to scope the character search to.</param>
    /// <param name="characterName">The character name to check.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>true if the name is unique; otherwise, false.</returns>
    Task<bool> IsNameUniqueForUserAsync
    (
        User user,
        Server server,
        string characterName,
        CancellationToken ct = default
    );
}
