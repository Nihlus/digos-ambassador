//
//  MetaTableBuilderTests.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591

using System.Linq;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public class MetaTableBuilderTests
{
    [Fact]
    public void CanBuildMetaTable()
    {
        const string expected = "env = { test = test,string = { format = string.format,subtable = { format = string.subtable.format } } }";
        var entries = new[] { "test", "string.format", "string.subtable.format" };
        var builder = new MetaTableBuilder();

        builder = entries.Aggregate(builder, (current, entry) => current.WithEntry(entry));

        var result = builder.Build();

        Assert.Equal(expected, result);
    }
}
