﻿//
//  LuaService.cs
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using JetBrains.Annotations;
using Remora.Results;
using Zio;

namespace DIGOS.Ambassador.Plugins.Transformations.Services.Lua;

/// <summary>
/// Handles execution of lua code.
/// </summary>
[PublicAPI]
public sealed class LuaService
{
    private readonly IReadOnlyList<string> _functionWhitelist = new[]
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
        "os.time"
    };

    private readonly ContentService _contentService;

    private readonly Regex _getErroringFunctionRegex =
        new("(?<=\\((?>global)|(?>field )(?> \')).+(?=\'\\))", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="LuaService"/> class.
    /// </summary>
    /// <param name="contentService">The application's content service.</param>
    public LuaService(ContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Gets a sandboxed lua state.
    /// </summary>
    /// <returns>A sandboxed lua state.</returns>
    [MustUseReturnValue("The state must be disposed after use.")]
    private NLua.Lua GetState(params (string Name, object? Value)[] variables)
    {
        var state = new NLua.Lua();

        var envBuilder = new MetaTableBuilder();

        foreach (var (name, value) in variables)
        {
            state[name] = value;
            envBuilder.WithEntry(name);
        }

        envBuilder = _functionWhitelist.Aggregate(envBuilder, (current, function) => current.WithEntry(function));

        state.DoString($"{envBuilder.Build()}");

        state.DoString
        (
            @"
                local debug = require ""debug""

                if not setfenv then -- Lua 5.2+
                    -- based on http://lua-users.org/lists/lua-l/2010-06/msg00314.html
                    -- this assumes f is a function
                    local function findenv(f)
                        local level = 1
                        repeat
                            local name, value = debug.getupvalue(f, level)
                            if name == '_ENV' then return level, value end
                            level = level + 1
                        until name == nil
                        return nil end
                    getfenv = function (f) return(select(2, findenv(f)) or _G) end
                    setfenv = function (f, t)
                        local level = findenv(f)
                        if level then debug.setupvalue(f, level, t) end
                        return f end
                end

                function run(untrusted_code)
                    if untrusted_code:byte(1) == 27 then
                        return nil, ""binary bytecode prohibited""
                    end

                    local untrusted_function, message = load(untrusted_code)
                    if not untrusted_function then
                        return nil, message
                    end

                    setfenv(untrusted_function, env)
                    return pcall(untrusted_function)
                end
                "
        );

        // Add a script timeout after 1e8 VM instructions
        state.DoString
        (
            @"
                function f()
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
    public Task<Result<string>> ExecuteSnippetAsync
    (
        string snippet,
        params (string Name, object? Value)[] variables
    )
    {
        return Task.Run
        (
            () =>
            {
                using var lua = GetState(variables);

                lua.DoString($"status, result = run [[{snippet}]]");
                lua.DoString("output = tostring(result)");

                var result = lua["output"] as string;
                var ranSuccessfully = lua["status"] is true;
                if (result is not null && ranSuccessfully)
                {
                    return Result<string>.FromSuccess(result);
                }

                if (result is not null && result.EndsWith("timeout!"))
                {
                    return new LuaTimeoutError();
                }

                var erroringFunction = _getErroringFunctionRegex.Match(result ?? string.Empty).Value;
                if (!_functionWhitelist.Contains(erroringFunction))
                {
                    return new LuaSandboxError(erroringFunction);
                }

                return new LuaError(result ?? "No information available.");
            }
        );
    }

    /// <summary>
    /// Executes the given lua script file and retrieves its first result.
    /// </summary>
    /// <param name="scriptPath">The path to the file which should be executed.</param>
    /// <param name="variables">Any variables to pass to the script as globals.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<string>> ExecuteScriptAsync
    (
        [PathReference] UPath scriptPath,
        params (string Name, object? Value)[] variables
    )
    {
        var getScriptResult = _contentService.OpenLocalStream(scriptPath);
        if (!getScriptResult.IsSuccess)
        {
            return Result<string>.FromError(getScriptResult);
        }

        string script;
        using (var sr = new StreamReader(getScriptResult.Entity))
        {
            script = await sr.ReadToEndAsync();
        }

        return await ExecuteSnippetAsync(script, variables);
    }
}
