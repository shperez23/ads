using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AdsManager.API.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value);
        if (roles.Any(role => RolePermissionMap.RoleHasPermission(role, requirement.Permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
