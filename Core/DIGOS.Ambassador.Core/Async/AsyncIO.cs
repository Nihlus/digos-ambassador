//
//  AsyncIO.cs
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

namespace DIGOS.Ambassador.Core.Async;

/// <summary>
/// Asynchronous file operations.
/// </summary>
public static class AsyncIO
{
    /// <summary>
    /// This is the same default buffer size as
    /// <see cref="StreamReader"/> and <see cref="FileStream"/>.
    /// </summary>
    private const int _defaultBufferSize = 4096;

    /// <summary>
    /// Indicates that
    /// 1. The file is to be used for asynchronous reading.
    /// 2. The file is to be accessed sequentially from beginning to end.
    /// </summary>
    private const FileOptions _defaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

    /// <summary>
    /// Asynchronously reads all lines from the given file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <returns>The contents of the file.</returns>
    public static async Task<string[]> ReadAllLinesAsync
    (
        string path,
        Encoding? encoding = null
    )
    {
        encoding ??= Encoding.UTF8;

        // Open the FileStream with the same FileMode, FileAccess
        // and FileShare as a call to File.OpenText would've done.
        await using var stream = new FileStream
        (
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _defaultBufferSize,
            _defaultOptions
        );

        return await ReadAllLinesAsync(stream, encoding);
    }

    /// <summary>
    /// Asynchronously reads all lines from the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after the call.</param>
    /// <returns>The contents of the file.</returns>
    public static async Task<string[]> ReadAllLinesAsync
    (
        Stream stream,
        Encoding? encoding = null,
        bool leaveOpen = true
    )
    {
        encoding ??= Encoding.UTF8;

        var lines = new List<string>();
        using var reader = new StreamReader(stream, encoding, false, _defaultBufferSize, leaveOpen);

        while (await reader.ReadLineAsync() is { } line)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Asynchronously reads all text from the given file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="encoding">The encoding of the file.</param>
    /// <returns>The contents of the file.</returns>
    public static async Task<string> ReadAllTextAsync(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        // Open the FileStream with the same FileMode, FileAccess
        // and FileShare as a call to File.OpenText would've done.
        await using var stream = new FileStream
        (
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _defaultBufferSize,
            _defaultOptions
        );

        return await ReadAllTextAsync(stream, encoding);
    }

    /// <summary>
    /// Asynchronously reads all text from the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after the read.</param>
    /// <returns>The contents of the stream.</returns>
    public static async Task<string> ReadAllTextAsync
    (
        Stream stream,
        Encoding? encoding = null,
        bool leaveOpen = true
    )
    {
        encoding ??= Encoding.UTF8;

        using var reader = new StreamReader(stream, encoding, false, _defaultBufferSize, leaveOpen);
        return await reader.ReadToEndAsync();
    }
}
