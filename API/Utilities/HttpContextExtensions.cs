using System.IdentityModel.Tokens.Jwt;

namespace API.Utilities;

// TODO: Remove this class and refactor in favor of ClaimsPrincipleExtensions.AuthorizedIdentity()
// All methods here only use the ClaimsPrinciple of the HttpContext
// We also have no current use for the distinction of client and user identities
public static class HttpContextExtensions
{
    /// <summary>
    /// If the user is properly logged in, returns their id.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>An optional user id</returns>
    public static int? AuthorizedUserIdentity(this HttpContext context)
    {
        return context.User.IsUser() ? ParseIdFromIssuer(context) : null;
    }

    public static int? AuthorizedClientIdentity(this HttpContext context)
    {
        return context.User.IsClient() ? ParseIdFromIssuer(context) : null;
    }

    private static int? ParseIdFromIssuer(HttpContext context)
    {
        var id = context.User.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Iss)?.Value;
        if (id == null)
        {
            return null;
        }

        if (!int.TryParse(id, out var idInt))
        {
            return null;
        }

        return idInt;
    }
}
