using API.Utilities;

namespace API.Authorization;

/// <summary>
/// String constants that represent authorization policies
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy that allows access from <see cref="OtrClaims.Admin"/> and <see cref="OtrClaims.System"/> requests,
    /// as well as redirected requests from unprivileged users to allow accessing their own resources.
    /// </summary>
    public const string AccessUserResources = "AccessUserResources";
}
