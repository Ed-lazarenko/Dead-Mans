using backend.Api.Realtime;
using backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Tests.Architecture;

public sealed class EndpointAuthorizationRulesTests
{
    private static readonly HashSet<Type> AnonymousControllerAllowlist =
    [
        typeof(AuthController),
        typeof(AuthSessionController)
    ];

    [Fact]
    public void ApplicationControllers_ShouldRequireAuthorizationByDefault()
    {
        var controllers = typeof(GameController)
            .Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes(typeof(ApiControllerAttribute), true).Length > 0)
            .ToArray();

        Assert.NotEmpty(controllers);
        foreach (var controller in controllers)
        {
            if (AnonymousControllerAllowlist.Contains(controller))
            {
                continue;
            }

            Assert.True(
                controller.IsDefined(typeof(AuthorizeAttribute), true),
                $"{controller.FullName} must require authorization at controller level."
            );
            Assert.DoesNotContain(
                controller.GetMethods(),
                method => method.IsDefined(typeof(AllowAnonymousAttribute), true)
            );
        }
    }

    [Fact]
    public void SignalRHubs_ShouldRequireAuthorization()
    {
        var hubs = typeof(GameBoardHub)
            .Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Hub).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(hubs);
        Assert.All(
            hubs,
            hub =>
                Assert.True(
                    hub.IsDefined(typeof(AuthorizeAttribute), true),
                    $"{hub.FullName} must require authorization."
                )
        );
    }
}
