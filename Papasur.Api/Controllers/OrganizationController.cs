using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Settings.Commands.UpdateOrganization;
using Papasur.Application.Settings.Queries.GetOrganization;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>
/// Datos del exportador que van en todos los documentos. El front los trata como un mapa
/// clave/valor, así que agregar un campo no toca el backend.
/// </summary>
[Route("api/v1/organization")]
public class OrganizationController : ApiControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyDictionary<string, string>>> Get(
        [FromServices] IQueryHandler<GetOrganizationQuery, IReadOnlyDictionary<string, string>> handler,
        CancellationToken cancellationToken)
        => Ok(await handler.Handle(new GetOrganizationQuery(), cancellationToken));

    [HttpPatch]
    [AuthorizeRoles(RoleNames.Admin)]
    public async Task<ActionResult<IReadOnlyDictionary<string, string>>> Update(
        [FromBody] Dictionary<string, string> values,
        [FromServices] ICommandHandler<UpdateOrganizationCommand, Result<IReadOnlyDictionary<string, string>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateOrganizationCommand(values ?? []) { Actor = CurrentActor },
            cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : Ok(result.Value);
    }
}
