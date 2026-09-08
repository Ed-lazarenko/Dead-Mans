using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
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

namespace backend.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansApplication(this IServiceCollection services)
    {
        services.AddScoped<IGameBoardService, GameBoardService>();
        services.AddScoped<IGameRoundService, GameRoundService>();
        services.AddScoped<IGameHistoryService, GameHistoryService>();
        services.AddScoped<IGameSetupService, GameSetupService>();
        services.AddScoped<IGameSetupCellMediaService, GameSetupCellMediaService>();
        services.AddScoped<IGameModifierService, GameModifierService>();
        services.AddScoped<IGameNotificationService, GameNotificationService>();
        services.AddScoped<IGameQuestionService, GameQuestionService>();
        services.AddScoped<IGameQuizService, GameQuizService>();
        services.AddScoped<IGameRegistrationService, GameRegistrationService>();
        services.AddScoped<IGameLifecycleService, GameLifecycleService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<ITwitchAuthFlowService, TwitchAuthFlowService>();

        return services;
    }
}
