//
//  KinkService.cs
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
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Pagination;

using Discord;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Service class for user kinks.
	/// </summary>
	public class KinkService
	{
		public async Task<RetrieveEntityResult<Kink>> GetKinkByNameAsync(GlobalInfoContext db, string name)
		{
			throw new System.NotImplementedException();
		}

		public Embed BuildKinkInfoEmbed(Kink kink)
		{
			throw new System.NotImplementedException();
		}

		public async Task<RetrieveEntityResult<UserKink>> GetUserKinkByNameAsync(GlobalInfoContext db, IUser user, string name)
		{
			throw new System.NotImplementedException();
		}

		public Embed BuildKinkPreferenceEmbed(UserKink userKink)
		{
			throw new System.NotImplementedException();
		}

		public async Task<IQueryable<UserKink>> GetUserKinksAsync(GlobalInfoContext db, IUser contextUser)
		{
			throw new System.NotImplementedException();
		}

		public Embed BuildKinkOverlapEmbed(IUser firstUser, IUser secondUser, IEnumerable<UserKink> overlap)
		{
			throw new System.NotImplementedException();
		}

		public PaginatedEmbed BuildPaginatedUserKinkEmbed(IQueryable<UserKink> withPreference)
		{
			throw new System.NotImplementedException();
		}
	}
}
