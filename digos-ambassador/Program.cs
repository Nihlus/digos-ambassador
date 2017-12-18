//
//  Program.cs
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Services;
using log4net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DIGOS.Ambassador
{
	/// <summary>
	/// The main entry point class of the program.
	/// </summary>
	internal static class Program
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		/// <summary>
		/// The main entry point of the program.
		/// </summary>
		/// <returns>A task.</returns>
		public static async Task Main()
		{
			// Connect to uncaught exceptions for logging
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			// Configure logging
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("app.config"));

			// Log.Debug($"Starting up. Running on {RuntimeInformation.OSDescription}");

			// Initialize
			var contentService = new ContentService();
			try
			{
				await contentService.InitializeAsync();
			}
			catch (FileNotFoundException fex)
			{
				Log.Error("Could not initialize content service.", fex);
				return;
			}

			using (var db = new GlobalInfoContext())
			{
				if (!((RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>()).Exists())
				{
					Log.Error("The database doesn't exist.");
					return;
				}
			}

			var ambassadorClient = new AmbassadorClient(contentService);
			await ambassadorClient.LoginAsync();
			await ambassadorClient.StartAsync();

			// Wait for shutdown
			await Task.Delay(-1);
		}

		/// <summary>
		/// Event handler for all unhandled exceptions that may be encountered during runtime. While there should never
		/// be any unhandled exceptions in an ideal program, unexpected issues can and will arise. This handler logs
		/// the exception and all relevant information to a logfile and prints it to the console for debugging purposes.
		/// </summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="unhandledExceptionEventArgs">The event object containing the information about the exception.</param>
		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
		{
			// Force english exception output
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			Log.Fatal("----------------");
			Log.Fatal("FATAL UNHANDLED EXCEPTION!");
			Log.Fatal("Something has gone terribly, terribly wrong during runtime.");
			Log.Fatal("The following is what information could be gathered by the program before crashing.");
			Log.Fatal("Please report this to <jarl.gullberg@gmail.com> or via GitHub. Include the full log and a " +
					  "description of what you were doing when it happened.");

			if (!(unhandledExceptionEventArgs.ExceptionObject is Exception unhandledException))
			{
				Log.Fatal("The unhandled exception was null. Call a priest.");
				return;
			}

			Log.Fatal($"Exception type: {unhandledException.GetType().FullName}");
			Log.Fatal($"Exception Message: {unhandledException.Message}");
			Log.Fatal($"Exception Stacktrace: {unhandledException.StackTrace}");

			if (unhandledException.InnerException is null)
			{
				return;
			}

			Log.Fatal($"Inner exception type: {unhandledException.InnerException.GetType().FullName}");
			Log.Fatal($"Inner exception Message: {unhandledException.InnerException.Message}");
			Log.Fatal($"Inner exception Stacktrace: {unhandledException.InnerException.StackTrace}");
		}
	}
}
