using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;

namespace Papasur.Api.Contracts;

/// <summary>
/// Base de los controllers: resuelve el actor desde el JWT y mapea Result → HTTP con la
/// forma de error del contrato ({ message, code }).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Usuario del token, con nombre y rol, para auditar y autorizar.</summary>
    protected Actor? CurrentActor
    {
        get
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                return null;
            }

            return new Actor(
                id,
                User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }

    protected Guid? CurrentUserId => CurrentActor?.Id;

    protected string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    protected ObjectResult Fail(int statusCode, Error error)
        => ApiErrorResults.FromError(statusCode, error);

    protected ObjectResult Fail(int statusCode, string message, string? code = null)
        => ApiErrorResults.Result(statusCode, message, code);
}
