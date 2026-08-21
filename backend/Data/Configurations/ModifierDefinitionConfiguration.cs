using backend.Data.Entities;
using backend.Domain.GameModifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class ModifierDefinitionConfiguration : IEntityTypeConfiguration<ModifierDefinition>
{
    private static readonly DateTime SeedTimestamp = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

    private static string BehaviorJson(string code) =>
        ModifierBehaviorV2Json.Serialize(BuiltInModifierBehaviorCatalog.Get(code).Behavior);

    private static string[] Tags(string code) =>
        BuiltInModifierBehaviorCatalog.Get(code).NormalizedTags.ToArray();

    public void Configure(EntityTypeBuilder<ModifierDefinition> builder)
    {
        builder.ToTable(
            "modifier_definitions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_definitions_cost_non_negative",
                    "activation_cost >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_definitions_limit_positive_or_null",
                    "max_activations_per_round IS NULL OR max_activations_per_round > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_definitions_category_allowed",
                    "category IN ('preparation','round','result')"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_definitions_revision_positive",
                    "revision >= 1"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_modifier_definitions_behavior_v2_schema",
                    "behavior_v2_json ->> 'schemaVersion' = '2'"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Revision).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(32).IsRequired().HasDefaultValue("round");
        builder.Property(x => x.IconEmoji).HasMaxLength(16);
        builder.Property(x => x.ActivationCommand).HasMaxLength(128);
        builder.Property(x => x.ActivationCost).IsRequired();
        builder.Property(x => x.MaxActivationsPerRound);
        builder.Property(x => x.NormalizedTags).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.BehaviorV2Json).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasData(
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Chirik,
                Name = "Чирик",
                Description = "Первые 60 секунд разрешено перемещаться только на корточках.",

                Category = "round",
                IconEmoji = "💰",
                ActivationCommand = "!активировать чирик",
                ActivationCost = 3,
                MaxActivationsPerRound = 5,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Chirik),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Chirik),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Zhazhda,
                Name = "Жажда",
                Description = "Убийства дают нарастающий бонус +5, миссия без убийств даёт штраф 25.",

                Category = "result",
                IconEmoji = "💉",
                ActivationCommand = "!активировать жажда",
                ActivationCost = 3,
                MaxActivationsPerRound = 2,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Zhazhda),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Zhazhda),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Rashodnik,
                Name = "Расходник",
                Description = "Игроки могут заменить один расходник на свой выбор.",

                Category = "preparation",
                IconEmoji = "🎯",
                ActivationCommand = "!активировать расходник",
                ActivationCost = 4,
                MaxActivationsPerRound = 4,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Rashodnik),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Rashodnik),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Trupy,
                Name = "Трупы",
                Description = "Запрет на сжигание трупов.",

                Category = "round",
                IconEmoji = "🔥",
                ActivationCommand = "!активировать трупы",
                ActivationCost = 4,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Trupy),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Trupy),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Navyki,
                Name = "Навыки",
                Description = "Количество доступных очков навыков уменьшено на 20% (-2 при 10).",

                Category = "preparation",
                IconEmoji = "⚙️",
                ActivationCommand = "!активировать навыки",
                ActivationCost = 4,
                MaxActivationsPerRound = 5,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Navyki),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Navyki),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Patron,
                Name = "Патрон",
                Description = "Если враг убит первой пулей, команда получает +1 убийство в счётчик.",

                Category = "result",
                IconEmoji = "🔫",
                ActivationCommand = "!активировать патрон",
                ActivationCost = 4,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Patron),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Patron),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Prokaznik,
                Name = "Проказник",
                Description = "Ментор пакостит 5 минут или пока не кончатся обманки.",

                Category = "round",
                IconEmoji = "🙊",
                ActivationCommand = "!активировать проказник",
                ActivationCost = 6,
                MaxActivationsPerRound = 2,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Prokaznik),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Prokaznik),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Diareya,
                Name = "Диарея",
                Description = "При упоминании/обнаружении туалета игрок обязан зайти в него (если нет врага в поле зрения).",

                Category = "round",
                IconEmoji = "💩",
                ActivationCommand = "!активировать диарея",
                ActivationCost = 7,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Diareya),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Diareya),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Mentorbait,
                Name = "Менторбайт",
                Description = "Ментор с шумелками на 5 минут, команда решает как использовать.",

                Category = "round",
                IconEmoji = "📣",
                ActivationCommand = "!активировать менторбайт",
                ActivationCost = 8,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Mentorbait),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Mentorbait),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Kep,
                Name = "Кэп",
                Description = "Только капитан команды может пользоваться голосовым чатом.",

                Category = "round",
                IconEmoji = "🔇",
                ActivationCommand = "!активировать кэп",
                ActivationCost = 10,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Kep),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Kep),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Feyerverk,
                Name = "Фейерверк",
                Description = "Ментор раз в минуту стреляет осветительными снарядами в небо 5 минут.",

                Category = "round",
                IconEmoji = "🎆",
                ActivationCommand = "!активировать фейерверк",
                ActivationCost = 11,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Feyerverk),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Feyerverk),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Krysa,
                Name = "Крыса",
                Description = "Ментор с полным набором ловушек; убийства ментора идут в счёт команды.",

                Category = "result",
                IconEmoji = "🐀",
                ActivationCommand = "!активировать крыса",
                ActivationCost = 12,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Krysa),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Krysa),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Shot,
                Name = "Шот",
                Description = "Ментор получает оружие с одним выстрелом, убийство идёт в счёт команды.",

                Category = "result",
                IconEmoji = "🥠",
                ActivationCommand = "!активировать шот",
                ActivationCost = 13,
                MaxActivationsPerRound = null,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Shot),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Shot),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Podem,
                Name = "Подъём",
                Description = "Нельзя поднимать союзника, пока не убит враг.",

                Category = "round",
                IconEmoji = "☠️",
                ActivationCommand = "!активировать подъём",
                ActivationCost = 14,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Podem),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Podem),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            },
            new ModifierDefinition
            {
                Id = ModifierDefinitionSeedIds.Hard75,
                Name = "Хард75",
                Description = "Каждое убийство получает множитель +0.75 до восстановления полосок.",

                Category = "result",
                IconEmoji = "💀",
                ActivationCommand = "!активировать хард75",
                ActivationCost = 18,
                MaxActivationsPerRound = 1,
                NormalizedTags = Tags(BuiltInModifierBehaviorCatalog.Hard75),
                BehaviorV2Json = BehaviorJson(BuiltInModifierBehaviorCatalog.Hard75),
                CreatedAtUtc = SeedTimestamp,
                UpdatedAtUtc = SeedTimestamp
            }
        );
    }
}
