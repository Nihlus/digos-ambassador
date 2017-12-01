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

using System;
using System.IO;

using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;

using Discord.Commands;
using YamlDotNet.Core;

namespace DIGOS.Ambassador.Tools
{
	/// <summary>
	/// The main class.
	/// </summary>
	internal static class Program
	{
		private static int Main(string[] args)
		{
			var options = new CommandLineOptions();
			var isValid = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);

			var verifier = new TransformationFileVerifier();

			var path = Path.GetFullPath(options.VerifyPath);
			DetermineConditionResult result;
			if (File.Exists(path))
			{
				// run file verification
				result = verifier.VerifyFile<Transformation>(path);
				if (!result.IsSuccess)
				{
					result = verifier.VerifyFile<Species>(path);
				}
			}
			else if (Directory.Exists(path))
			{
				// run directory verification
				result = verifier.VerifyFilesInDirectory(path);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"Input not found.");
				return -2;
			}

			if (!result.IsSuccess)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				if (result.Error == CommandError.Exception)
				{
					Console.WriteLine($"File \"{result.ErrorReason}\" failed verification.");

					var yamlException = (YamlException)result.Exception ?? throw new ArgumentNullException();
					Console.WriteLine($"Error at {yamlException.Start}: {yamlException.InnerException.Message}");

					return -1;
				}

				Console.WriteLine(result.ErrorReason);
				return 3;
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"Target file(s) look fine.");
			return 0;
		}
	}
}
