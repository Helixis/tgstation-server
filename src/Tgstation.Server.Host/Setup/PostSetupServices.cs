﻿using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Setup
{
	/// <inheritdoc />
	sealed class PostSetupServices : IPostSetupServices
	{
		/// <inheritdoc />
		public IPlatformIdentifier PlatformIdentifier { get; }

		/// <inheritdoc />
		public GeneralConfiguration GeneralConfiguration => generalConfigurationOptions.Value;

		/// <inheritdoc />
		public DatabaseConfiguration DatabaseConfiguration => databaseConfigurationOptions.Value;

		/// <inheritdoc />
		public FileLoggingConfiguration FileLoggingConfiguration => fileLoggingConfigurationOptions.Value;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="GeneralConfiguration"/>.
		/// </summary>
		readonly IOptions<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="DatabaseConfiguration"/>.
		/// </summary>
		readonly IOptions<DatabaseConfiguration> databaseConfigurationOptions;

		/// <summary>
		/// Backing <see cref="IOptions{TOptions}"/> for <see cref="FileLoggingConfiguration"/>.
		/// </summary>
		readonly IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions;

		/// <summary>
		/// Initializes a new instance of the <see cref="PostSetupServices"/> <see langword="class"/>.
		/// </summary>
		/// <param name="platformIdentifier">The value of <see cref="PlatformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="GeneralConfiguration"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="DatabaseConfiguration"/>.</param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="FileLoggingConfiguration"/>.</param>
		public PostSetupServices(
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<DatabaseConfiguration> databaseConfigurationOptions,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions)
		{
			PlatformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.databaseConfigurationOptions = databaseConfigurationOptions ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.fileLoggingConfigurationOptions = fileLoggingConfigurationOptions ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
		}
	}
}
