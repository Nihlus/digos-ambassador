//
//  FileAsync.cs
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
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Extensions
{
    /// <summary>
    /// Asynchronous file operations.
    /// </summary>
    public static class FileAsync
    {
        /// <summary>
        /// This is the same default buffer size as
        /// <see cref="StreamReader"/> and <see cref="FileStream"/>.
        /// </summary>
        private const int DefaultBufferSize = 4096;

        /// <summary>
        /// Indicates that
        /// 1. The file is to be used for asynchronous reading.
        /// 2. The file is to be accessed sequentially from beginning to end.
        /// </summary>
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        /// <summary>
        /// Asynchronously reads all lines from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The contents of the file.</returns>
        [NotNull]
        public static Task<string[]> ReadAllLinesAsync(string path)
        {
            return ReadAllLinesAsync(path, Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously reads all lines from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <returns>The contents of the file.</returns>
        [ItemNotNull]
        public static async Task<string[]> ReadAllLinesAsync(string path, [NotNull] Encoding encoding)
        {
            var lines = new List<string>();

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Asynchronously reads all text from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The contents of the file.</returns>
        [NotNull]
        public static Task<string> ReadAllTextAsync(string path)
        {
            return ReadAllTextAsync(path, Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously reads all text from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <returns>The contents of the file.</returns>
        public static async Task<string> ReadAllTextAsync(string path, [NotNull] Encoding encoding)
        {
            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
