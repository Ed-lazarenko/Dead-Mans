namespace backend.Domain.GameModifiers;

public sealed record BuiltInModifierBehavior(
    IReadOnlyList<string> NormalizedTags,
    ModifierBehaviorV2 Behavior
);

public static class BuiltInModifierBehaviorCatalog
{
    public const string Chirik = "chirik";
    public const string Zhazhda = "zhazhda";
    public const string Rashodnik = "rashodnik";
    public const string Trupy = "trupy";
    public const string Navyki = "navyki";
    public const string Patron = "patron";
    public const string Prokaznik = "prokaznik";
    public const string Diareya = "diareya";
    public const string Mentorbait = "mentorbait";
    public const string Kep = "kep";
    public const string Feyerverk = "feyerverk";
    public const string Krysa = "krysa";
    public const string Shot = "shot";
    public const string Podem = "podem";
    public const string Hard75 = "hard75";

    private static readonly IReadOnlyDictionary<string, BuiltInModifierBehavior> Items =
        new Dictionary<string, BuiltInModifierBehavior>(StringComparer.Ordinal)
        {
            [Chirik] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                false,
                "Первые 60 секунд за каждую активацию разрешено перемещаться только на корточках.",
                ["движение", "приседание", "таймер"],
                60
            ),
            [Rashodnik] = Rule(
                ModifierPhase.Preparation,
                ModifierPerformer.ActiveTeam,
                false,
                "Команда может заменить один расходник на свой выбор за каждую активацию.",
                ["снаряжение", "расходники", "замена"]
            ),
            [Trupy] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                true,
                "Запрещено сжигать трупы весь раунд.",
                ["трупы", "огонь", "запрет"]
            ),
            [Navyki] = Rule(
                ModifierPhase.Preparation,
                ModifierPerformer.ActiveTeam,
                true,
                "Внешний лимит навыков уменьшается на 20% за активацию, но не более чем на 100%.",
                ["навыки", "подготовка", "ограничение"]
            ),
            [Diareya] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                true,
                "При упоминании или обнаружении туалета игрок обязан зайти в него, если врага нет в поле зрения.",
                ["окружение", "туалет", "триггер"]
            ),
            [Kep] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                true,
                "Пользоваться голосовым чатом может только капитан.",
                ["коммуникация", "капитан", "голос"]
            ),
            [Podem] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                true,
                "Нельзя поднимать союзника, пока команда не убила врага.",
                ["оживление", "союзник", "условие"]
            ),
            [Prokaznik] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.Mentor,
                true,
                "Ментор с обманками и полтергейстом мешает команде 300 секунд за активацию; его нельзя убить или поднять.",
                ["ментор", "помеха", "обманки", "полтергейст", "таймер"],
                300
            ),
            [Mentorbait] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.Mentor,
                true,
                "Ментор с набором шумелок действует 300 секунд; его можно убить, но нельзя поднять.",
                ["ментор", "шум", "приманка", "таймер"],
                300
            ),
            [Feyerverk] = Rule(
                ModifierPhase.Round,
                ModifierPerformer.Mentor,
                true,
                "Ментор стреляет осветительными снарядами при старте и через 60, 120, 180 и 240 секунд; его нельзя убить или поднять.",
                ["ментор", "сигналы", "осветительные снаряды", "таймер"],
                300
            ),
            [Zhazhda] = Scoring(
                ModifierPerformer.ActiveTeam,
                "В конце раунда за каждую активацию к стоимости одного убийства добавляется 5 × количество убийств. "
                + "Новая стоимость умножается на количество убийств. Если убийств нет, каждая активация даёт штраф 25 очков.",
                new AutomaticRoundMetricResolution("killsCount"),
                ModifierRewardKind.Points,
                new ModifierFormulaReference(
                    ModifierFormulaCodes.GrowingKillValue,
                    ModifierFormulaCodes.Version1,
                    new GrowingKillValueParameters(5, 25)
                ),
                ["убийства", "очки", "бонус", "штраф", "риск"]
            ),
            [Patron] = Scoring(
                ModifierPerformer.ActiveTeam,
                "Если враг убит первой пулей не из лука, арбалета или дробовика, команда получает бонусное убийство.",
                new BooleanResolution(),
                ModifierRewardKind.BonusKills,
                new ModifierFormulaReference(
                    ModifierFormulaCodes.BonusKillOnCondition,
                    ModifierFormulaCodes.Version1,
                    new BonusKillOnConditionParameters(1)
                ),
                ["оружие", "точность", "первая пуля", "исключения"]
            ),
            [Krysa] = Scoring(
                ModifierPerformer.Mentor,
                "Убийства ментора с полным набором ловушек считаются бонусными убийствами команды.",
                new NonNegativeCountResolution(),
                ModifierRewardKind.BonusKills,
                new ModifierFormulaReference(
                    ModifierFormulaCodes.BonusKillsByCount,
                    ModifierFormulaCodes.Version1,
                    new BonusKillsByCountParameters(1)
                ),
                ["ментор", "ловушки", "убийства"]
            ),
            [Shot] = Scoring(
                ModifierPerformer.Mentor,
                "Каждая активация даёт ментору оружие с одним выстрелом; успешный выстрел считается бонусным убийством команды.",
                new BooleanResolution(),
                ModifierRewardKind.BonusKills,
                new ModifierFormulaReference(
                    ModifierFormulaCodes.BonusKillOnCondition,
                    ModifierFormulaCodes.Version1,
                    new BonusKillOnConditionParameters(1)
                ),
                ["ментор", "оружие", "один выстрел", "убийства"]
            ),
            [Hard75] = Scoring(
                ModifierPerformer.ActiveTeam,
                "Подходящие убийства до восстановления здоровья дают дополнительные 75% стоимости карточки.",
                new NonNegativeCountResolution(),
                ModifierRewardKind.Points,
                new ModifierFormulaReference(
                    ModifierFormulaCodes.WindowKillBonusPoints,
                    ModifierFormulaCodes.Version1,
                    new WindowKillBonusPointsParameters(0.75m)
                ),
                ["здоровье", "убийства", "окно действия", "бонус"]
            )
        };

    public static BuiltInModifierBehavior Get(string code) =>
        Items.TryGetValue(code, out var item)
            ? item
            : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown built-in modifier.");

    private static BuiltInModifierBehavior Rule(
        ModifierPhase phase,
        ModifierPerformer performer,
        bool requiresHostMonitoring,
        string rule,
        IReadOnlyList<string> tags,
        int? durationSecondsPerActivation = null
    ) => new(
        tags,
        new ModifierBehaviorV2(
            ModifierBehaviorSchemaVersions.V2,
            ModifierBehaviorKind.Rule,
            phase,
            performer,
            requiresHostMonitoring,
            rule,
            ModifierStackingPolicy.AggregateParameters,
            new RuleStatusResolution(),
            ModifierRewardKind.None,
            null,
            durationSecondsPerActivation
        )
    );

    private static BuiltInModifierBehavior Scoring(
        ModifierPerformer performer,
        string rule,
        ModifierResolution resolution,
        ModifierRewardKind reward,
        ModifierFormulaReference formula,
        IReadOnlyList<string> tags
    ) => new(
        tags,
        new ModifierBehaviorV2(
            ModifierBehaviorSchemaVersions.V2,
            ModifierBehaviorKind.Scoring,
            ModifierPhase.Result,
            performer,
            true,
            rule,
            ModifierStackingPolicy.IndependentInstances,
            resolution,
            reward,
            formula
        )
    );
}
