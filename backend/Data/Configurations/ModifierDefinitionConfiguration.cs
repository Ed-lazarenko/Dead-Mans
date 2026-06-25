using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ModifierDefinitionConfiguration : IEntityTypeConfiguration<ModifierDefinition>
{
    private static readonly DateTime SeedTimestamp = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);
    private const string RuleOnlyMetadata =
        "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";
    private const string RuleOnly60SecondsMetadata =
        "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":60,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";
    private const string ZhazhdaMetadata =
        "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";
    private const string PatronMetadata =
        "{\"effect\":{\"mechanicType\":\"kill_counter\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":1},\"conditions\":[{\"type\":\"first_kill_first_bullet\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":{\"killDeltaMode\":\"conditional_bonus_kill\",\"killDeltaValue\":1,\"condition\":\"first_kill_first_bullet\",\"excludedWeapons\":[\"лук\",\"арбалет\",\"дробовик\"]},\"multiplierEffect\":null,\"mentorEffect\":null}}";
    private const string MentorProkaznikMetadata =
        "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Обманки и полтергейст\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}";
    private const string MentorBaitMetadata =
        "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Набор шумелок\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":false}}}";
    private const string MentorFeyerverkMetadata =
        "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Оружие с осветительными снарядами\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}";
    private const string MentorKillCreditMetadata =
        "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}";
    private const string Hard75Metadata =
        "{\"effect\":{\"mechanicType\":\"multiplier\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":0.75,\"killDelta\":null},\"conditions\":[{\"type\":\"until_health_restored\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"killsDuringWindow\"],\"killEffect\":null,\"multiplierEffect\":{\"target\":\"kills\",\"delta\":0.75,\"activeWindow\":\"until_condition\",\"stopCondition\":\"health_restored\"},\"mentorEffect\":null}}";

    public void Configure(EntityTypeBuilder<ModifierDefinition> builder)
    {
        builder.ToTable(
            "modifier_definitions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_modifier_definitions_cost_non_negative",
                    "\"ActivationCost\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_modifier_definitions_limit_positive_or_null",
                    "\"DefaultLimitPerGame\" IS NULL OR \"DefaultLimitPerGame\" > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ScoringType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IconEmoji).HasMaxLength(16);
        builder.Property(x => x.ActivationCommand).HasMaxLength(128);
        builder.Property(x => x.ActivationCost).IsRequired();
        builder.Property(x => x.DefaultLimitPerGame);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasData(
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Chirik,
                Name = "Чирик",
                Description = "Первые 60 секунд разрешено перемещаться только на корточках.",

                ScoringType = "non_scoring",
                IconEmoji = "💰",
                ActivationCommand = "!активировать чирик",
                ActivationCost = 3,
                DefaultLimitPerGame = 5,
                MetadataJson = RuleOnly60SecondsMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Zhazhda,
                Name = "Жажда",
                Description = "Убийства дают нарастающий бонус +5, миссия без убийств даёт штраф 25.",

                ScoringType = "conditional_bonus_penalty",
                IconEmoji = "💉",
                ActivationCommand = "!активировать жажда",
                ActivationCost = 3,
                DefaultLimitPerGame = 2,
                MetadataJson = ZhazhdaMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Rashodnik,
                Name = "Расходник",
                Description = "Игроки могут заменить один расходник на свой выбор.",

                ScoringType = "non_scoring",
                IconEmoji = "🎯",
                ActivationCommand = "!активировать расходник",
                ActivationCost = 4,
                DefaultLimitPerGame = 4,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Trupy,
                Name = "Трупы",
                Description = "Запрет на сжигание трупов.",

                ScoringType = "non_scoring",
                IconEmoji = "🔥",
                ActivationCommand = "!активировать трупы",
                ActivationCost = 4,
                DefaultLimitPerGame = 1,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Navyki,
                Name = "Навыки",
                Description = "Количество доступных очков навыков уменьшено на 20% (-2 при 10).",

                ScoringType = "non_scoring",
                IconEmoji = "⚙️",
                ActivationCommand = "!активировать навыки",
                ActivationCost = 4,
                DefaultLimitPerGame = 5,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Patron,
                Name = "Патрон",
                Description = "Если враг убит первой пулей, команда получает +1 убийство в счётчик.",

                ScoringType = "conditional_bonus",
                IconEmoji = "🔫",
                ActivationCommand = "!активировать патрон",
                ActivationCost = 4,
                DefaultLimitPerGame = 1,
                MetadataJson = PatronMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Prokaznik,
                Name = "Проказник",
                Description = "Ментор пакостит 5 минут или пока не кончатся обманки.",

                ScoringType = "non_scoring",
                IconEmoji = "🙊",
                ActivationCommand = "!активировать проказник",
                ActivationCost = 6,
                DefaultLimitPerGame = 2,
                MetadataJson = MentorProkaznikMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Diareya,
                Name = "Диарея",
                Description = "При упоминании/обнаружении туалета игрок обязан зайти в него (если нет врага в поле зрения).",

                ScoringType = "non_scoring",
                IconEmoji = "💩",
                ActivationCommand = "!активировать диарея",
                ActivationCost = 7,
                DefaultLimitPerGame = 1,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Mentorbait,
                Name = "Менторбайт",
                Description = "Ментор с шумелками на 5 минут, команда решает как использовать.",

                ScoringType = "non_scoring",
                IconEmoji = "📣",
                ActivationCommand = "!активировать менторбайт",
                ActivationCost = 8,
                DefaultLimitPerGame = 1,
                MetadataJson = MentorBaitMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Kep,
                Name = "Кэп",
                Description = "Только капитан команды может пользоваться голосовым чатом.",

                ScoringType = "non_scoring",
                IconEmoji = "🔇",
                ActivationCommand = "!активировать кэп",
                ActivationCost = 10,
                DefaultLimitPerGame = 1,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Feyerverk,
                Name = "Фейерверк",
                Description = "Ментор раз в минуту стреляет осветительными снарядами в небо 5 минут.",

                ScoringType = "non_scoring",
                IconEmoji = "🎆",
                ActivationCommand = "!активировать фейерверк",
                ActivationCost = 11,
                DefaultLimitPerGame = 1,
                MetadataJson = MentorFeyerverkMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Krysa,
                Name = "Крыса",
                Description = "Ментор с полным набором ловушек; убийства ментора идут в счёт команды.",

                ScoringType = "conditional_bonus",
                IconEmoji = "🐀",
                ActivationCommand = "!активировать крыса",
                ActivationCost = 12,
                DefaultLimitPerGame = 1,
                MetadataJson = MentorKillCreditMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Shot,
                Name = "Шот",
                Description = "Ментор получает оружие с одним выстрелом, убийство идёт в счёт команды.",

                ScoringType = "conditional_bonus",
                IconEmoji = "🥠",
                ActivationCommand = "!активировать шот",
                ActivationCost = 13,
                DefaultLimitPerGame = null,
                MetadataJson = MentorKillCreditMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Podem,
                Name = "Подъём",
                Description = "Нельзя поднимать союзника, пока не убит враг.",

                ScoringType = "non_scoring",
                IconEmoji = "☠️",
                ActivationCommand = "!активировать подъём",
                ActivationCost = 14,
                DefaultLimitPerGame = 1,
                MetadataJson = RuleOnlyMetadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Hard75,
                Name = "Хард75",
                Description = "Каждое убийство получает множитель +0.75 до восстановления полосок.",

                ScoringType = "multiplier",
                IconEmoji = "💀",
                ActivationCommand = "!активировать хард75",
                ActivationCost = 18,
                DefaultLimitPerGame = 1,
                MetadataJson = Hard75Metadata,
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            }
        );
    }
}
