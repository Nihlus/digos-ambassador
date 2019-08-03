//
//  FileSignatures.cs
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

// ReSharper disable InconsistentNaming
#pragma warning disable SA1310

namespace DIGOS.Ambassador.Core.Services.Content
{
    /// <summary>
    /// Holds binary file signatures.
    /// </summary>
    public static class FileSignatures
    {
        /// <summary>
        /// The header signature of a PDF file.
        /// </summary>
        public static readonly byte[] PDF = { 0x25, 0x50, 0x44, 0x46 };

        /// <summary>
        /// Animated GIF, version 87.
        /// </summary>
        public static readonly byte[] GIF87 = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };

        /// <summary>
        /// Animated GIF, version 89.
        /// </summary>
        public static readonly byte[] GIF89 = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        /// <summary>
        /// TIFF Image, Little Endian.
        /// </summary>
        public static readonly byte[] TIFF_LE = { 0x49, 0x49, 0x2A, 0x00 };

        /// <summary>
        /// TIFF Image, Big Endian.
        /// </summary>
        public static readonly byte[] TIFF_BE = { 0x4D, 0x4D, 0x00, 0x2A };

        /// <summary>
        /// RAW JPEG.
        /// </summary>
        public static readonly byte[] JPEG_RAW = { 0xFF, 0xD8, 0xFF, 0xDB };

        /// <summary>
        /// JFIF JPEG.
        /// </summary>
        public static readonly byte[] JPEG_FIF = { 0xFF, 0xD8, 0xFF, 0xE0 };

        /// <summary>
        /// EXIF JPEG.
        /// </summary>
        public static readonly byte[] JPEG_XIF = { 0xFF, 0xD8, 0xFF, 0xE1 };

        /// <summary>
        /// Portable Network Graphics.
        /// </summary>
        public static readonly byte[] PNG = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Photoshop binary file.
        /// </summary>
        public static readonly byte[] PSD = { 0x38, 0x42, 0x50, 0x53 };

        /// <summary>
        /// Windows bitmap.
        /// </summary>
        public static readonly byte[] BMP = { 0x42, 0x4D };

        /// <summary>
        /// Gets all image signatures.
        /// </summary>
        public static readonly IEnumerable<byte[]> ImageSignatures = new[]
        {
            GIF87, GIF89,
            TIFF_LE, TIFF_BE,
            JPEG_RAW, JPEG_FIF, JPEG_XIF,
            PNG,
            PSD,
            BMP
        };
    }
}
