//
//  Program.cs
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

using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.TypeReaders;

using Discord;

namespace DIGOS.Ambassador.Doc
{
	/// <summary>
	/// The main program class.
	/// </summary>
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			var options = new Options();
			Parser.Default.ParseArgumentsStrict(args, options);

			var assembly = Assembly.LoadFrom(options.AssemblyPath);

			var generator = new ModuleDocumentationGenerator(assembly, options.OutputPath)
			.WithTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>())
			.WithTypeReader<Character>(new CharacterTypeReader())
			.WithTypeReader<Roleplay>(new RoleplayTypeReader())
			.WithTypeReader<Colour>(new ColourTypeReader())
			.WithTypeReader<UserClass>(new HumanizerEnumTypeReader<UserClass>())
			.WithTypeReader<KinkPreference>(new HumanizerEnumTypeReader<KinkPreference>())
			.WithTypeReader<Bodypart>(new HumanizerEnumTypeReader<Bodypart>())
			.WithTypeReader<Pattern>(new HumanizerEnumTypeReader<Pattern>());

			await generator.GenerateDocumentationAsync();
		}
	}
}
