//
//  FileSystemFactory.cs
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

using System.IO;
using System.Reflection;
using Zio;
using Zio.FileSystems;

namespace DIGOS.Ambassador.Core.Services
{
    /// <summary>
    /// Serves as a factory class for abstract file systems.
    /// </summary>
    public static class FileSystemFactory
    {
        /// <summary>
        /// Creates a rooted sub-filesystem for the local content folder.
        /// </summary>
        /// <returns>The rooted filesystem.</returns>
        public static IFileSystem CreateContentFileSystem()
        {
            var realFileSystem = new PhysicalFileSystem();

            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var executingAssemblyDirectory = Directory.GetParent(executingAssemblyLocation).FullName;

            var realContentPath = Path.GetFullPath(Path.Combine(executingAssemblyDirectory, "Content"));
            var zioContentPath = realFileSystem.ConvertPathFromInternal(realContentPath);

            if (!realFileSystem.DirectoryExists(zioContentPath))
            {
                realFileSystem.CreateDirectory(zioContentPath);
            }

            return new SubFileSystem(realFileSystem, zioContentPath);
        }
    }
}
