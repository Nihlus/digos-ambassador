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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using NLua;

namespace DIGOS.Ambassador.Services
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

		private readonly ContentService ContentService;

		private readonly Regex GetErroringFunctionRegex =
			new Regex("(?<=\\((?>global)|(?>field )(?> \')).+(?=\'\\))", RegexOptions.Compiled);

		/// <summary>
		/// Initializes a new instance of the <see cref="LuaService"/> class.
		/// </summary>
		/// <param name="contentService">The application's content service.</param>
		public LuaService(ContentService contentService)
		{
			this.ContentService = contentService;
		}

		/// <summary>
		/// Gets a sandboxed lua state.
		/// </summary>
		/// <returns>A sandboxed lua state.</returns>
		[MustUseReturnValue("The state must be disposed after use.")]
		private Lua GetState()
		{
			var state = new Lua();

			// Sandbox the state by restricting the available functions to an API whitelist
			var functionList = string.Join(",\n", this.FunctionWhitelist.Select(f => $"\"{f}\""));
			state.DoString($"env = {{{functionList}}}");

			state.DoString
			(
				@"function run(untrusted_code)
					if untrusted_code:byte(1) == 27 then return nil, ""binary bytecode prohibited"" end
					local untrusted_function, message = loadstring(untrusted_code)
					if not untrusted_function then return nil, message end
					setfenv(untrusted_function, env)
					return pcall(untrusted_function)
				end
				"
			);

			// Add a script timeout after 1e8 VM instructions
			state.DoString
			(
				@"function f()
					error(""timeout!"")
				end
				debug.sethook(f,"""", 1e8)
				"
			);

			return state;
		}

		/// <summary>
		/// Executes the given lua snippet and retrieves its first result.
		/// </summary>
		/// <param name="snippet">The snippet to execute.</param>
		/// <param name="variables">Any variables to pass to the snippet as globals.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public Task<RetrieveEntityResult<string>> ExecuteSnippetAsync(string snippet, params (string name, object value)[] variables)
		{
			return Task.Run
			(
				() =>
				{
					using (var lua = GetState())
					{
						foreach (var variable in variables)
						{
							lua[variable.name] = variable.value;
						}

						lua.DoString($"status, result = run [[{snippet}]]");

						string result = lua["result"] as string;
						bool ranSuccessfully = lua["status"] is bool b && b;
						if (!(result is null) && ranSuccessfully)
						{
							return RetrieveEntityResult<string>.FromSuccess(result);
						}

						if (!(result is null) && result.EndsWith("timeout!"))
						{
							return RetrieveEntityResult<string>.FromError(CommandError.Unsuccessful, "Timed out while waiting for the script to complete.");
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
		/// <param name="variables">Any variables to pass to the script as globals.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<string>> ExecuteScriptAsync([PathReference] string scriptPath, params (string name, object value)[] variables)
		{
			var getScriptResult = this.ContentService.GetLocalStream(scriptPath);
			if (!getScriptResult.IsSuccess)
			{
				return RetrieveEntityResult<string>.FromError(getScriptResult);
			}

			string script;
			using (var sr = new StreamReader(getScriptResult.Entity))
			{
				script = await sr.ReadToEndAsync();
			}

			return await ExecuteSnippetAsync(script, variables);
		}
	}
}
