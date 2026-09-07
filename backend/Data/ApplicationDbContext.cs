using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using backend.Data.Configurations;
using backend.Data.Entities;

namespace backend.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameBoard> GameBoards => Set<GameBoard>();
    public DbSet<BoardCell> BoardCells => Set<BoardCell>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<BoardCellMedia> BoardCellMedia => Set<BoardCellMedia>();
    public DbSet<GameTeamSlot> GameTeamSlots => Set<GameTeamSlot>();
    public DbSet<GameTeam> GameTeams => Set<GameTeam>();
    public DbSet<GameTeamMember> GameTeamMembers => Set<GameTeamMember>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<GameRoundTransitionAudit> GameRoundTransitionAudits =>
        Set<GameRoundTransitionAudit>();
    public DbSet<GameRoundCellMedia> GameRoundCellMedia => Set<GameRoundCellMedia>();
    public DbSet<GameRoundParticipant> GameRoundParticipants => Set<GameRoundParticipant>();
    public DbSet<GameRoundModifierResult> GameRoundModifierResults =>
        Set<GameRoundModifierResult>();
    public DbSet<GameTeamInvitation> GameTeamInvitations =>
        Set<GameTeamInvitation>();
    public DbSet<GameEnabledModifier> GameEnabledModifiers => Set<GameEnabledModifier>();
    public DbSet<GameModifierActivation> GameModifierActivations => Set<GameModifierActivation>();
    public DbSet<ModifierDefinition> ModifierDefinitions => Set<ModifierDefinition>();
    public DbSet<ModifierDefinitionVersion> ModifierDefinitionVersions =>
        Set<ModifierDefinitionVersion>();
    public DbSet<ModifierDefinitionVersionConflict> ModifierDefinitionVersionConflicts =>
        Set<ModifierDefinitionVersionConflict>();
    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();
    public DbSet<QuestionDefinition> QuestionDefinitions => Set<QuestionDefinition>();
    public DbSet<GameQuizRound> GameQuizRounds => Set<GameQuizRound>();
    public DbSet<GameEnabledQuestion> GameEnabledQuestions => Set<GameEnabledQuestion>();
    public DbSet<GameQuizManualAward> GameQuizManualAwards => Set<GameQuizManualAward>();
    public DbSet<GameUserNotification> GameUserNotifications => Set<GameUserNotification>();
    public DbSet<GameFinalization> GameFinalizations => Set<GameFinalization>();
    public DbSet<GameTeamFinalResult> GameTeamFinalResults => Set<GameTeamFinalResult>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectVersionMutation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        RejectVersionMutation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejectVersionMutation()
    {
        var hasMutation = ChangeTracker.Entries()
            .Any(entry =>
                (entry.Entity is ModifierDefinitionVersion
                    || entry.Entity is ModifierDefinitionVersionConflict)
                && entry.State is EntityState.Modified or EntityState.Deleted);
        if (hasMutation)
        {
            throw new InvalidOperationException("Modifier revision rows are immutable.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new GameBoardConfiguration());
        modelBuilder.ApplyConfiguration(new BoardCellConfiguration());
        modelBuilder.ApplyConfiguration(new MediaAssetConfiguration());
        modelBuilder.ApplyConfiguration(new BoardCellMediaConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamSlotConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new GameRoundConfiguration());
        modelBuilder.ApplyConfiguration(new GameRoundTransitionAuditConfiguration());
        modelBuilder.ApplyConfiguration(new GameRoundCellMediaConfiguration());
        modelBuilder.ApplyConfiguration(new GameRoundParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new GameRoundModifierResultConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierDefinitionVersionConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierDefinitionVersionConflictConfiguration());
        modelBuilder.ApplyConfiguration(new GameEnabledModifierConfiguration());
        modelBuilder.ApplyConfiguration(new GameModifierActivationConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new GameQuizRoundConfiguration());
        modelBuilder.ApplyConfiguration(new GameEnabledQuestionConfiguration());
        modelBuilder.ApplyConfiguration(new GameQuizManualAwardConfiguration());
        modelBuilder.ApplyConfiguration(new GameUserNotificationConfiguration());
        modelBuilder.ApplyConfiguration(new GameFinalizationConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamFinalResultConfiguration());

        ApplySnakeCaseRelationalNames(modelBuilder);
    }

    private static void ApplySnakeCaseRelationalNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entityType.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }
    }

    private static string ToSnakeCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i += 1)
        {
            var current = value[i];
            if (current == '_')
            {
                builder.Append(current);
                continue;
            }

            if (char.IsUpper(current))
            {
                var previous = i > 0 ? value[i - 1] : '\0';
                var next = i + 1 < value.Length ? value[i + 1] : '\0';
                var startsNewWord = i > 0
                    && previous != '_'
                    && (!char.IsUpper(previous) || (next != '\0' && char.IsLower(next)));

                if (startsNewWord)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
