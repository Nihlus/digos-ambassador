//
//  UserStatistics.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Statistics;

/// <summary>
/// Represents simple statistics about a user.
/// </summary>
[Table("UserStatistics", Schema = "AutoroleModule")]
public class UserStatistics : EFEntity
{
    /// <summary>
    /// Gets the user that the statistics are for.
    /// </summary>
    public virtual User User { get; private set; } = null!;

    /// <summary>
    /// Gets the per-server statistics of the user.
    /// </summary>
    public virtual List<UserServerStatistics> ServerStatistics { get; private set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStatistics"/> class.
    /// </summary>
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
    protected UserStatistics()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStatistics"/> class.
    /// </summary>
    /// <param name="user">The user that the statistics are for.</param>
    public UserStatistics(User user)
    {
        this.User = user;
        this.ServerStatistics = new List<UserServerStatistics>();
    }
}
