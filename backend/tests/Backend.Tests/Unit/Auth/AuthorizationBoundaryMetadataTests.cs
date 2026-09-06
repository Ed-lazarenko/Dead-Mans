using System.Reflection;
using backend.Application.Abstractions.Auth;
using backend.Controllers;
using backend.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Tests.Unit.Auth;

public sealed class AuthorizationBoundaryMetadataTests
{
    [Fact]
    public void EveryNonAuthControllerRequiresAuthenticationAtClassLevel()
    {
        var intentionallyAnonymousControllers = new HashSet<Type>
        {
            typeof(AuthController),
            typeof(AuthSessionController)
        };
        var controllerTypes = typeof(GameController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        var unprotectedControllers = controllerTypes
            .Where(type => !intentionallyAnonymousControllers.Contains(type))
            .Where(type => !type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any())
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(unprotectedControllers);
    }

    [Fact]
    public void AdministrativeControllersAndSetupHubRemainAdminOnly()
    {
        AssertEffectiveRoles(typeof(GameLifecycleController), methodName: null, AuthRoleCodes.Admin);
        AssertEffectiveRoles(typeof(GameSetupController), methodName: null, AuthRoleCodes.Admin);
        AssertEffectiveRoles(typeof(GameSetupCellMediaController), methodName: null, AuthRoleCodes.Admin);
        AssertEffectiveRoles(typeof(GameSetupHub), methodName: null, AuthRoleCodes.Admin);
    }

    [Fact]
    public void SensitiveActionsKeepTheirExpectedRoleBoundaries()
    {
        var adminActions = new Dictionary<Type, string[]>
        {
            [typeof(GameController)] = [nameof(GameController.OpenCell)],
            [typeof(GameModifierController)] =
            [
                nameof(GameModifierController.GetAdminPlayers),
                nameof(GameModifierController.GetAdminState),
                nameof(GameModifierController.GetAdminActiveActivations),
                nameof(GameModifierController.Create),
                nameof(GameModifierController.Preview),
                nameof(GameModifierController.Update),
                nameof(GameModifierController.Delete),
                nameof(GameModifierController.EmergencyDisable),
                nameof(GameModifierController.AdminActivate),
                nameof(GameModifierController.CancelActivation)
            ]
        };
        foreach (var (controllerType, methodNames) in adminActions)
        {
            foreach (var methodName in methodNames)
            {
                AssertEffectiveRoles(controllerType, methodName, AuthRoleCodes.Admin);
            }
        }

        AssertAllHttpActionsHaveRoles(typeof(GameQuestionController), AuthRoleCodes.Admin);
        AssertAllHttpActionsHaveRoles(typeof(GameQuizController), AuthRoleCodes.ModeratorOrAdmin);

        var moderatorOrAdminActions = new Dictionary<Type, string[]>
        {
            [typeof(GameController)] =
            [nameof(GameController.SetActiveTeam), nameof(GameController.SetTeamPlayedState)],
            [typeof(GameRegistrationController)] =
            [
                nameof(GameRegistrationController.ListTeams),
                nameof(GameRegistrationController.GetAdminSnapshot),
                nameof(GameRegistrationController.CreateAdminTeam),
                nameof(GameRegistrationController.UpdateAdminTeamName),
                nameof(GameRegistrationController.AssignPlayer),
                nameof(GameRegistrationController.RemovePlayerFromTeam),
                nameof(GameRegistrationController.CancelTeamInvitation),
                nameof(GameRegistrationController.MoveTeam),
                nameof(GameRegistrationController.ConfirmTeam),
                nameof(GameRegistrationController.RejectTeam),
                nameof(GameRegistrationController.DisbandConfirmedTeam),
                nameof(GameRegistrationController.CreateInvitation)
            ],
            [typeof(GameRoundController)] =
            [
                nameof(GameRoundController.GetEligibleTeams),
                nameof(GameRoundController.Start),
                nameof(GameRoundController.Review),
                nameof(GameRoundController.Prepare),
                nameof(GameRoundController.Rebuild),
                nameof(GameRoundController.BeginGameplay),
                nameof(GameRoundController.ResumeGameplay),
                nameof(GameRoundController.TechnicalCancel),
                nameof(GameRoundController.Finalize),
                nameof(GameRoundController.PreviewScore)
            ]
        };
        foreach (var (controllerType, methodNames) in moderatorOrAdminActions)
        {
            foreach (var methodName in methodNames)
            {
                AssertEffectiveRoles(controllerType, methodName, AuthRoleCodes.ModeratorOrAdmin);
            }
        }
    }

    [Fact]
    public void BoardHubRequiresAuthenticationWithoutGrantingAdministrativeRole()
    {
        Assert.True(typeof(Hub).IsAssignableFrom(typeof(GameBoardHub)));
        var authorize = Assert.Single(
            typeof(GameBoardHub).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
        );
        Assert.True(string.IsNullOrWhiteSpace(authorize.Roles));
    }

    private static void AssertAllHttpActionsHaveRoles(Type controllerType, string expectedRoles)
    {
        var actions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
            .ToArray();

        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            AssertEffectiveRoles(controllerType, action.Name, expectedRoles);
        }
    }

    private static void AssertEffectiveRoles(
        Type endpointType,
        string? methodName,
        string expectedRoles
    )
    {
        var methodAttributes = methodName is null
            ? []
            : endpointType
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .ToArray();
        var attributes = methodAttributes.Length > 0
            ? methodAttributes
            : endpointType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();

        var authorize = Assert.Single(attributes);
        Assert.Equal(expectedRoles, authorize.Roles);
    }
}
