//
//  StreamExtensions.cs
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
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Extension methods for streams.
	/// </summary>
	public static class StreamExtensions
	{
		/// <summary>
		/// Determines whether or not a given stream has a given signature at a given offset. Used for verifying file
		/// formats.
		/// </summary>
		/// <param name="this">The stream.</param>
		/// <param name="signature">The binary signature.</param>
		/// <param name="offset">The offset at which to search.</param>
		/// <returns>true if the stream has the signature; otherwise, false.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the stream does not support seeking or reading, or if no signature data is provided.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if reading the given signature at the given offset would fall outside of the stream.
		/// </exception>
		public static async Task<bool> HasSignatureAsync([NotNull] this Stream @this, [NotNull] byte[] signature, long offset = 0)
		{
			if (!@this.CanSeek)
			{
				throw new ArgumentException("The stream does not support seeking.", nameof(@this));
			}

			if (!@this.CanRead)
			{
				throw new ArgumentException("The stream does not support reading.", nameof(@this));
			}

			if (signature.Length <= 0)
			{
				throw new ArgumentException("No signature data provided.", nameof(signature));
			}

			if (offset + signature.Length > @this.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "Reading the signature at the given offset would fall outside of the stream.");
			}

			long originalPosition = @this.Position;
			var signatureBuffer = new byte[signature.Length];

			@this.Seek(offset, SeekOrigin.Begin);
			await @this.ReadAsync(signatureBuffer, 0, signatureBuffer.Length);

			@this.Seek(originalPosition, SeekOrigin.Begin);

			return signatureBuffer.SequenceEqual(signature);
		}
	}
}
