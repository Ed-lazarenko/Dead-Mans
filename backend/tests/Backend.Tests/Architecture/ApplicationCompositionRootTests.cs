using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.DependencyInjection;
using backend.Application.Features.Auth;
using backend.Application.Features.GameBoard;
using backend.Application.Features.GameHistory;
using backend.Application.Features.GameLifecycle;
using backend.Application.Features.GameModifiers;
using backend.Application.Features.GameNotifications;
using backend.Application.Features.GameQuestions;
using backend.Application.Features.GameRegistration;
using backend.Application.Features.GameRounds;
using backend.Application.Features.GameSetup;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Architecture;

public sealed class ApplicationCompositionRootTests
{
    [Theory]
    [MemberData(nameof(ApplicationServiceRegistrations))]
    public void AddDeadMansApplication_RegistersEachUseCaseAgainstItsInterface(
        Type serviceType,
        Type implementationType
    )
    {
        var services = new ServiceCollection();

        services.AddDeadMansApplication();

        var registration = Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);
        Assert.Equal(implementationType, registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    public static TheoryData<Type, Type> ApplicationServiceRegistrations =>
        new()
        {
            { typeof(IGameBoardService), typeof(GameBoardService) },
            { typeof(IGameRoundService), typeof(GameRoundService) },
            { typeof(IGameHistoryService), typeof(GameHistoryService) },
            { typeof(IGameSetupService), typeof(GameSetupService) },
            { typeof(IGameSetupCellMediaService), typeof(GameSetupCellMediaService) },
            { typeof(IGameModifierService), typeof(GameModifierService) },
            { typeof(IGameNotificationService), typeof(GameNotificationService) },
            { typeof(IGameQuestionService), typeof(GameQuestionService) },
            { typeof(IGameQuizService), typeof(GameQuizService) },
            { typeof(IGameRegistrationService), typeof(GameRegistrationService) },
            { typeof(IGameLifecycleService), typeof(GameLifecycleService) },
            { typeof(IAuthSessionService), typeof(AuthSessionService) },
            { typeof(ITwitchAuthFlowService), typeof(TwitchAuthFlowService) }
        };
}
