using AdsManager.API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AdsManager.API.Extensions;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAdsManagerAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var policy in AuthorizationPolicies.All)
            {
                options.AddPolicy(policy, builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.Requirements.Add(new PermissionRequirement(policy));
                });
            }
        });

        return services;
    }
}
