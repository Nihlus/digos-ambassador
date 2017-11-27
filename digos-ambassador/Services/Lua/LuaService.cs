//
//  LuaService.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using NLua;

namespace DIGOS.Ambassador.Services.Lua
{
	/// <summary>
	/// Handles execution of lua code.
	/// </summary>
	public class LuaService
	{
		private readonly IReadOnlyList<string> FunctionWhitelist = new[]
		{
			"assert",
			"error",
			"ipairs",
			"next",
			"pairs",
			"pcall",
			"select",
			"tonumber",
			"tostring",
			"type",
			"unpack",
			"_VERSION",
			"xpcall",
			"string.byte",
			"string.char",
			"string.find",
			"string.format",
			"string.gmatch",
			"string.gsub",
			"string.len",
			"string.lower",
			"string.match",
			"string.rep",
			"string.reverse",
			"string.sub",
			"string.upper",
			"table.insert",
			"table.maxn",
			"table.remove",
			"table.sort",
			"math.abs",
			"math.acos",
			"math.asin",
			"math.atan",
			"math.atan2",
			"math.ceil",
			"math.cos",
			"math.cosh",
			"math.deg",
			"math.exp",
			"math.floor",
			"math.fmod",
			"math.frexp",
			"math.huge",
			"math.ldexp",
			"math.log",
			"math.log10",
			"math.max",
			"math.min",
			"math.modf",
			"math.pi",
			"math.pow",
			"math.rad",
			"math.random",
			"math.randomseed",
			"math.sin",
			"math.sinh",
			"math.sqrt",
			"math.tan",
			"math.tanh",
			"os.clock",
			"os.time",

		};

		private Regex GetErroringFunctionRegex =
			new Regex("(?<=\\((?>global)|(?>field )(?> \')).+(?=\'\\))", RegexOptions.Compiled);

		/// <summary>
		/// Gets a sandboxed lua state.
		/// </summary>
		/// <returns>A sandboxed lua state.</returns>
		[MustUseReturnValue("The state must be disposed after use.")]
		private NLua.Lua GetState()
		{
			var state = new NLua.Lua();

			// Sandbox the state by restricting the available functions to an API whitelist
			state.DoString($"local env = {{{string.Join(",\n", this.FunctionWhitelist.Select(f => $"\"{f}\""))}}}");
			state.DoString
			(
				@"local function run(untrusted_code)
					if untrusted_code:byte(1) == 27 then return nil, ""binary bytecode prohibited"" end
					local untrusted_function, message = loadstring(untrusted_code)
					if not untrusted_function then return nil, message end
					setfenv(untrusted_function, env)
					return pcall(untrusted_function)
				end"
			);

			// Add a script timeout after 1e8 VM instructions
			state.DoString
			(
				@"local function f()
					error(""timeout!"")
				end
				debug.sethook(f,""count"", 1e8)"
			);

			return state;
		}

		/// <summary>
		/// Executes the given lua snippet and retrieves its first result.
		/// </summary>
		/// <param name="snippet">The snippet to execute.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public Task<RetrieveEntityResult<string>> ExecuteSnippetAsync(string snippet)
		{
			return Task.Run
			(
				() =>
				{
					using (var lua = GetState())
					{
						lua.DoString($"local status, result = run [[{snippet}]]");

						string result = lua.DoString("return result").First() as string;
						bool ranSuccessfully = lua["status"] is bool b && b;
						if (ranSuccessfully && !(result is null))
						{
							return RetrieveEntityResult<string>.FromSuccess(result);
						}

						string erroringFunction = this.GetErroringFunctionRegex.Match(result ?? string.Empty).Value;
						if (!this.FunctionWhitelist.Contains(erroringFunction))
						{
							return RetrieveEntityResult<string>.FromError(CommandError.UnmetPrecondition, "Usage of that API is prohibited.");
						}

						return RetrieveEntityResult<string>.FromError(CommandError.ParseFailed, $"Lua error: {result}");
					}
				}
			);
		}

		/// <summary>
		/// Executes the given lua script file and retrieves its first result.
		/// </summary>
		/// <param name="scriptPath">The path to the file which should be executed.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<string>> ExecuteScriptAsync([PathReference] string scriptPath)
		{
			throw new NotImplementedException();
		}
	}
}
