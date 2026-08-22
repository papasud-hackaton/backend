namespace Papasur.Domain.Users;

/// <summary>
/// Catálogo de roles del sistema (tabla fija, sembrada por migración: admin, supervisor, agente).
/// El Name es el valor que viaja como claim de rol en el JWT.
/// </summary>
public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}

/// <summary>
/// Nombres de rol e IDs fijos. Usar estas constantes en [AuthorizeRoles(...)] y nunca literales sueltos.
/// </summary>
public static class RoleNames
{
    public const string Admin = "admin";

    public const string Supervisor = "supervisor";

    public const string Agente = "agente";

    public static readonly string[] All = [Admin, Supervisor, Agente];

    public static bool Exists(string name) => All.Contains(name);
}

public static class RoleIds
{
    public const int Admin = 1;

    public const int Supervisor = 2;

    public const int Agente = 3;
}
