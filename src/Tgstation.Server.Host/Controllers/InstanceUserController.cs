﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="InstanceUser"/>s.
	/// </summary>
	[Route(Routes.InstanceUser)]
	public sealed class InstanceUserController : InstanceRequiredController
	{
		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public InstanceUserController(
			IInstanceManager instanceManager,
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<InstanceUserController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{ }

		/// <summary>
		/// Create an <see cref="Api.Models.InstanceUser"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.InstanceUser"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="201"><see cref="Api.Models.InstanceUser"/> created successfully.</response>
		[HttpPut]
		[TgsAuthorize(InstanceUserRights.CreateUsers)]
		[ProducesResponseType(typeof(Api.Models.InstanceUser), 201)]
		public async Task<IActionResult> Create([FromBody] Api.Models.InstanceUser model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var userCanonicalName = await DatabaseContext
				.Users
				.AsQueryable()
				.Where(x => x.Id == model.UserId)
				.Select(x => x.CanonicalName)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);

			if (userCanonicalName == default)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			if (userCanonicalName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
				return Forbid();

			var dbUser = new Models.InstanceUser
			{
				ByondRights = RightsHelper.Clamp(model.ByondRights ?? ByondRights.None),
				ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? ChatBotRights.None),
				ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? ConfigurationRights.None),
				DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? DreamDaemonRights.None),
				DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? DreamMakerRights.None),
				RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? RepositoryRights.None),
				InstanceUserRights = RightsHelper.Clamp(model.InstanceUserRights ?? InstanceUserRights.None),
				UserId = model.UserId,
				InstanceId = Instance.Id
			};

			DatabaseContext.InstanceUsers.Add(dbUser);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			return Created(dbUser.ToApi());
		}

		/// <summary>
		/// Update the permissions for an <see cref="Api.Models.InstanceUser"/>.
		/// </summary>
		/// <param name="model">The updated <see cref="Api.Models.InstanceUser"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="Api.Models.InstanceUser"/> updated successfully.</response>
		/// <response code="410">The requested <see cref="Api.Models.InstanceUser"/> does not currently exist.</response>
		[HttpPost]
		[TgsAuthorize(InstanceUserRights.WriteUsers)]
		[ProducesResponseType(typeof(Api.Models.InstanceUser), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] Api.Models.InstanceUser model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var originalUser = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstanceUsers)
				.Where(x => x.UserId == model.UserId)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (originalUser == null)
				return Gone();

			originalUser.ByondRights = RightsHelper.Clamp(model.ByondRights ?? originalUser.ByondRights.Value);
			originalUser.RepositoryRights = RightsHelper.Clamp(model.RepositoryRights ?? originalUser.RepositoryRights.Value);
			originalUser.InstanceUserRights = RightsHelper.Clamp(model.InstanceUserRights ?? originalUser.InstanceUserRights.Value);
			originalUser.ChatBotRights = RightsHelper.Clamp(model.ChatBotRights ?? originalUser.ChatBotRights.Value);
			originalUser.ConfigurationRights = RightsHelper.Clamp(model.ConfigurationRights ?? originalUser.ConfigurationRights.Value);
			originalUser.DreamDaemonRights = RightsHelper.Clamp(model.DreamDaemonRights ?? originalUser.DreamDaemonRights.Value);
			originalUser.DreamMakerRights = RightsHelper.Clamp(model.DreamMakerRights ?? originalUser.DreamMakerRights.Value);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			return Json(originalUser.UserId == AuthenticationContext.User.Id || (AuthenticationContext.GetRight(RightsType.InstanceUser) & (ulong)InstanceUserRights.ReadUsers) != 0 ? originalUser.ToApi() : new Api.Models.InstanceUser
			{
				UserId = originalUser.UserId
			});
		}
#pragma warning restore CA1506
		/// <summary>
		/// Read the active <see cref="Api.Models.InstanceUser"/>.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="Api.Models.InstanceUser"/> retrieved successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.InstanceUser), 200)]
		public IActionResult Read() => Json(AuthenticationContext.InstanceUser.ToApi());

		/// <summary>
		/// Lists <see cref="Api.Models.InstanceUser"/>s for the instance.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.InstanceUser"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(InstanceUserRights.ReadUsers)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.InstanceUser>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var users = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstanceUsers)
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);
			return Json(users.Select(x => x.ToApi()));
		}

		/// <summary>
		/// Gets a specific <see cref="Api.Models.InstanceUser"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.InstanceUser.UserId"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieve <see cref="Api.Models.InstanceUser"/> successfully.</response>
		/// <response code="410">The requested <see cref="Api.Models.InstanceUser"/> does not currently exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(InstanceUserRights.ReadUsers)]
		[ProducesResponseType(typeof(Api.Models.InstanceUser), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			// this functions as userId
			var user = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstanceUsers)
				.Where(x => x.UserId == id)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (user == default)
				return Gone();
			return Json(user.ToApi());
		}

		/// <summary>
		/// Delete an <see cref="Api.Models.InstanceUser"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.InstanceUser.UserId"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204"><see cref="Api.Models.InstanceUser"/> deleted or no longer exists.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(InstanceUserRights.WriteUsers)]
		[ProducesResponseType(204)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.SelectMany(x => x.InstanceUsers)
				.Where(x => x.UserId == id)
				.DeleteAsync(cancellationToken)
				.ConfigureAwait(false);
			return NoContent();
		}
	}
}
