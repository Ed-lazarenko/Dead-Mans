using Microsoft.EntityFrameworkCore;
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
    public DbSet<GameParticipationSlot> GameParticipationSlots => Set<GameParticipationSlot>();
    public DbSet<GameTeam> GameTeams => Set<GameTeam>();
    public DbSet<GameTeamMember> GameTeamMembers => Set<GameTeamMember>();
    public DbSet<GameParticipationInvitation> GameParticipationInvitations =>
        Set<GameParticipationInvitation>();
    public DbSet<GameModifierSelection> GameModifierSelections => Set<GameModifierSelection>();
    public DbSet<GameActiveModifier> GameActiveModifiers => Set<GameActiveModifier>();
    public DbSet<ModifierDefinition> ModifierDefinitions => Set<ModifierDefinition>();
    public DbSet<ModifierConflict> ModifierConflicts => Set<ModifierConflict>();
    public DbSet<QuestionVector> QuestionVectors => Set<QuestionVector>();
    public DbSet<QuestionDefinition> QuestionDefinitions => Set<QuestionDefinition>();
    public DbSet<GameQuestionRound> GameQuestionRounds => Set<GameQuestionRound>();
    public DbSet<GameQuestionSelection> GameQuestionSelections => Set<GameQuestionSelection>();

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
        modelBuilder.ApplyConfiguration(new GameParticipationSlotConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamConfiguration());
        modelBuilder.ApplyConfiguration(new GameTeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new GameParticipationInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierConflictConfiguration());
        modelBuilder.ApplyConfiguration(new GameModifierSelectionConfiguration());
        modelBuilder.ApplyConfiguration(new GameActiveModifierConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionVectorConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new GameQuestionRoundConfiguration());
        modelBuilder.ApplyConfiguration(new GameQuestionSelectionConfiguration());
    }
}
