//
//  PlaceholderData.cs
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

using DIGOS.Ambassador.Doc.Abstractions;
using Discord;

[assembly: PlaceholderData(typeof(IUser), "@Ada", "@Bea", "@Cyrus", "@Don")]
[assembly: PlaceholderData(typeof(IGuildUser), "@Ada", "@Bea", "@Cyrus", "@Don")]
[assembly: PlaceholderData(typeof(IMessage), "660204034829058070", "660203865316130867")]
[assembly: PlaceholderData(typeof(ITextChannel), "#general", "#streaming", "#hangout")]
[assembly: PlaceholderData(typeof(ICategoryChannel), "572105666269937705")]
[assembly: PlaceholderData(typeof(IRole), "@Moderators", "@Users", "@Admins")]
[assembly: PlaceholderData(typeof(IEmote), "🦈")]
[assembly: PlaceholderData(typeof(IChannel), "#general")]
