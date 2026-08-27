const translations = {
  en: {
    actions: {
      edit: 'Edit',
      view: 'View',
      delete: 'Delete',
    },
    common: {
      yes: 'Yes',
      no: 'No',
    },
    errors: {
      duplicateCode: 'This code is already used by another entry.',
      notFound: 'The entry was not found. It may have been removed.',
      invalidRequest: 'Some fields are invalid. Check the form and try again.',
      contentLocked: 'This modifier is locked because it is included in the active game.',
      categoryNotFound: 'The question category was not found. It may have been removed.',
      categoryNotEmpty: 'A category that still contains questions cannot be deleted.',
      categoryProtected: 'The system fallback category cannot be renamed or deleted.',
      generic: 'The operation could not be completed. Please try again.',
    },
    validation: {
      required: 'This field is required.',
      code: 'Use lowercase latin letters, digits and underscore.',
      number: 'Enter a non-negative whole number.',
      limit: 'Leave empty or enter a positive whole number.',
      formula: 'Enter a valid formula expression.',
      tags: 'Use at most five unique tags, up to 32 characters each.',
    },
    modifiers: {
      title: 'Modifier catalog',
      description:
        'Master list of modifiers. Create, edit and remove the modifiers available to any game.',
      add: 'Add modifier',
      loading: 'Loading modifiers…',
      error: 'Failed to load the modifier catalog.',
      empty: 'No modifiers yet. Add the first one.',
      emptyCategory: 'No modifiers in this category.',
      menuTitle: 'Modifier tools',
      menuDescription:
        'Search the catalog, add modifiers and filter by category and result behavior.',
      menuHint: 'Use the buttons on each row to edit or remove an existing modifier.',
      categoryCount: '{{count}} modifiers',
      roundSummaryTitle: 'Round summary behavior',
      allRoundSummaries: 'All behaviors',
      roundSummaryCount: '{{count}} modifiers',
      meta: '{{category}} · cost {{cost}}',
      hostControlBadge: 'Host tracks this modifier',
      contentLockedBadge: 'Locked by active game',
      contentLockedReason:
        'This modifier is included in the active game. Its content is read-only until the game finishes or is archived.',
      createTitle: 'New modifier',
      editTitle: 'Edit modifier',
      deleteTitle: 'Remove modifier',
      deleteConfirm:
        'Remove "{{name}}" from the catalog? It will no longer be available for new games.',
      fields: {
        name: 'Name',
        description: 'Description',
        category: 'When the modifier applies',
        categoryHint: 'Choose the stage of the game where this modifier works.',
        requiresHostControl: 'Requires host control',
        mechanicType: 'Modifier mechanics',
        activationCost: 'Activation cost',
        activationLimitCount: 'Activation limit',
        limitHint: 'Leave empty for unlimited.',
        conflicts: 'Conflicts',
        conflictsHint: 'Modifiers that cannot be active together with this one.',
        iconEmoji: 'Icon (emoji)',
        activationCommand: 'Activation command',
        durationSeconds: 'Duration, seconds',
        ruleText: 'Short rule text',
        perKillBonus: 'Points per kill',
        failurePenaltyPoints: 'Failure penalty',
        killDeltaMode: 'Kill counter mode',
        killDeltaValue: 'Kill bonus',
        killCondition: 'Condition',
        excludedWeapons: 'Excluded weapons',
        csvHint: 'Comma-separated values.',
        multiplierTarget: 'Multiplier target',
        multiplierDelta: 'Multiplier delta',
        activeWindow: 'Active window',
        stopCondition: 'Stop condition',
        mentorLoadoutText: 'Mentor loadout',
        mentorCanBeRevived: 'Mentor can be revived',
        mentorCanBeKilled: 'Mentor can be killed',
        mentorKillsCreditToTeam: 'Mentor kills count for team',
      },
      sections: {
        basic: 'Basic information',
        mechanics: 'Mechanics',
        availability: 'Availability, limits and conflicts',
      },
      mechanics: {
        rule_only: 'Rule without points',
        restriction_with_reward: 'Restriction with reward or penalty',
        kill_counter: 'Kill counter change',
        multiplier: 'Points or kills multiplier',
        mentor: 'Mentor modifier',
      },
      roundSummaryType: {
        passive: 'Does not affect round totals',
        automatic: 'Calculated automatically',
        condition: 'Host confirms a condition',
        manual_count: 'Host enters a count',
      },
      preview: {
        title: 'Formula preview',
        unlimited: 'without limit',
        limit: 'up to {{count}} per game',
        body: '{{category}} · {{mechanic}} · {{scoringType}} · {{limit}}',
        roundSummary: 'Round summary behavior: {{category}}.',
        resultInput: 'The host will also enter: {{input}}.',
        scoreFormula: 'Score formula: {{formula}}.',
        successExpression: 'Success expression: {{expression}}.',
        failureExpression: 'Failure expression: {{expression}}.',
      },
      wizard: {
        step: 'Step {{current}} of {{total}}',
        steps: { 0: 'Card', 1: 'Conditions and activation', 2: 'Result calculation', 3: 'Review' },
        stepDescriptions: {
          0: 'Describe the modifier exactly as players will see it in the catalog.',
          1: 'Choose who follows the rule, when it applies, its price, limits, and conflicts.',
          2: 'Choose what is counted first, then define what every counted unit adds.',
          3: 'Review the player and host cards and verify the calculated example before saving.',
        },
        sections: {
          behavior: 'How the rule works',
          behaviorDescription:
            'These settings define the game stage, performer, and host responsibilities.',
          activation: 'Purchase and compatibility',
          activationDescription:
            'These settings define price, purchase limits, and compatibility with other modifiers.',
        },
        kind: 'What does this modifier do?',
        kinds: { rule: 'Rule without score changes', scoring: 'Affects the round result' },
        tags: 'Search tags',
        tagsHint: 'Choose suggestions or enter up to five custom tags.',
        suggestedTags: {
          combat: 'combat',
          mentor: 'mentor',
          movement: 'movement',
          equipment: 'equipment',
          communication: 'communication',
          revival: 'revival',
          environment: 'environment',
          restriction: 'restriction',
          weapon: 'weapon',
          bonus: 'bonus',
          penalty: 'penalty',
          timer: 'timer',
        },
        phase: 'When does the modifier apply?',
        phases: {
          preparation: 'Before the round',
          round: 'During the round',
          result: 'At scoring',
        },
        phaseDescriptions: {
          preparation: 'Card, equipment and skill selection, and team preparation.',
          round: 'The condition or restriction applies while the game is being played.',
          result: 'The result is recorded after gameplay ends.',
        },
        performer: 'Who must fulfil the condition?',
        performers: { activeTeam: 'Team', mentor: 'Host' },
        performerDescriptions: {
          activeTeam: 'The action or restriction applies to the active team.',
          mentor: 'The host performs the action and the result counts for the team.',
        },
        rule: 'Rule for the team and host',
        requiresHostMonitoring: 'Does the host need to verify the result manually?',
        monitoringAnswers: { yes: 'Yes, verify it', no: 'No, calculate automatically' },
        monitoringDescriptions: {
          yes: 'The host confirms fulfilment or enters the result after gameplay.',
          no: 'The application reads the required round metrics automatically.',
        },
        durationQuestion: 'Is there a time limit?',
        durationAnswers: { yes: 'Yes, use a timer', no: 'No time limit' },
        durationDescriptions: {
          yes: 'Every activation adds a separate time interval.',
          no: 'The rule applies without a countdown.',
        },
        durationHint: 'Optional. Leave empty when there is no timer.',
        commandHint: 'Leave empty to generate a command from the modifier name.',
        advancedSettings: 'Advanced settings',
        advancedSettingsDescription: 'The activation command is usually generated automatically.',
        reward: 'What changes',
        rewards: { points: 'Points', bonusKills: 'Bonus kills' },
        resolution: 'How the fact is recorded',
        resolutions: {
          automaticRoundMetric: 'Automatically from round metrics',
          boolean: 'Host selects succeeded / not succeeded',
          nonNegativeCount: 'Host enters a non-negative count',
        },
        formula: 'Calculation preset',
        formulaHint: 'Only presets compatible with the selected result are shown.',
        impactGuideTitle: 'No hand-written formulas',
        impactGuideDescription:
          'Choose the matching scenario. The form configures the reward and the data the host must provide.',
        formulaConfigTitle: 'Configured automatically',
        formulaConfig: 'Result: {{reward}}. Recorded as: {{resolution}}.',
        impactTargetQuestion: 'What does the modifier add to the result?',
        impactTargetHint: 'Choose the result first. The calculation method comes next.',
        impactTargets: {
          points: {
            title: 'Points',
            description: 'Changes the round score without changing the kill count.',
          },
          bonusKills: {
            title: 'Additional kills',
            description: 'Adds virtual kills before the card score is calculated.',
          },
        },
        impactMethodQuestions: {
          points: 'How are the extra points awarded?',
          bonusKills: 'How is the number of additional kills determined?',
        },
        formulas: {
          growing_kill_value: 'For every team kill',
          bonus_kill_on_condition: 'Once when a condition succeeds',
          bonus_kills_by_count: 'Based on a counted event',
          window_kill_bonus_points: 'Only for qualifying kills',
        },
        formulaDescriptions: {
          growing_kill_value:
            'The application reads the total kill count automatically. Suitable for mechanics such as Thirst.',
          bonus_kill_on_condition:
            'The host selects Fulfilled or Not fulfilled. Success grants a fixed kill bonus.',
          bonus_kills_by_count:
            'The host enters a trigger count. Every trigger adds the configured number of kills.',
          window_kill_bonus_points:
            'The host enters the qualifying kill count. Each grants a percentage of the card value.',
        },
        impactSettingsTitle: 'Configure the result',
        impactSettings: {
          growing_kill_value:
            'Increase the card value for every kill and optionally apply a penalty when there are no kills.',
          bonus_kill_on_condition:
            'Set the number of kills granted when the host confirms the condition.',
          bonus_kills_by_count: 'Set the number of kills added by every event entered by the host.',
          window_kill_bonus_points:
            'Set the percentage of card value awarded for every qualifying kill.',
        },
        units: { points: 'points', kills: 'kills', seconds: 'sec.' },
        calculationExampleTitle: 'How this is calculated',
        calculationExamples: {
          growing_kill_value:
            'The card is worth {{cardValue}} points and the team gets {{killsCount}} kills.\nNew value: {{cardValue}} + {{killsCount}} × {{increment}} = {{increasedCardValue}}.\nResult: {{increasedCardValue}} × {{killsCount}} = {{result}} points. With zero kills, the penalty is {{penalty}} points.',
          bonus_kill_on_condition:
            'The card is worth {{cardValue}} points, the team gets {{killsCount}} kills, and fulfils the condition.\nCounted kills: {{killsCount}} + {{bonus}} = {{resultUnits}}.\nResult: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} points.',
          bonus_kills_by_count:
            'The card is worth {{cardValue}} points and the team gets {{killsCount}} kills. The host enters {{inputCount}} events.\nBonus: {{inputCount}} × {{perUnit}} = {{bonus}} kills.\nResult: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} points.',
          window_kill_bonus_points:
            'The card is worth {{cardValue}} points and the team gets {{killsCount}} kills, including {{inputCount}} qualifying kills.\nBonus: {{inputCount}} × {{cardValue}} × {{percent}}% = {{bonus}} points.\nResult: {{killsCount}} × {{cardValue}} + {{bonus}} = {{result}} points.',
        },
        parameters: {
          incrementPointsPerKill: 'Value increase per kill',
          zeroKillPenaltyPoints: 'Penalty when there are no kills',
          successBonusKills: 'Bonus kills on success',
          bonusKillsPerUnit: 'Bonus kills per unit',
          bonusRate: 'Card value percentage per kill',
        },
        help: {
          kind: 'Choose a rule for restrictions that do not change the score. Choose round impact when points or kills must change.',
          name: 'Shown to players and hosts in the catalog and round history.',
          description:
            'Player-facing explanation of what happens after purchase. Keep every important condition visible here.',
          iconEmoji: 'Shown next to the name to make the modifier easier to recognize.',
          tags: 'Used only for search and filtering. They do not affect rules or scoring.',
          phase:
            'Controls whether the modifier applies during preparation, gameplay, or round results.',
          performer: 'Defines who performs the action: the active team or the host.',
          rule: 'Exact host and team instruction used to monitor the modifier and resolve the round.',
          requiresHostMonitoring:
            'Adds a manual-monitoring marker. Enable it when the application cannot verify the condition itself.',
          durationSeconds:
            'Duration of one activation. Every additional activation adds the same interval.',
          activationCost: 'Quiz points deducted from the buyer for every activation.',
          activationLimitCount:
            'Maximum activations of this modifier in one round. Leave empty for no limit.',
          conflicts:
            'If a selected modifier is already active in the round, this one cannot be purchased, and vice versa.',
          activationCommand:
            'Command used to order the modifier. Leave empty to generate it from the name.',
          formulaCode:
            'The preset defines the reward, host input, and calculation. Choose by gameplay meaning, not technical wording.',
          incrementPointsPerKill:
            'How many points one activation adds to kill value for every kill made.',
          zeroKillPenaltyPoints: 'Points deducted by each activation when the team makes no kills.',
          successBonusKills: 'Bonus kills added when the host marks the condition as successful.',
          bonusKillsPerUnit: 'Bonus kills granted for every trigger counted by the host.',
          bonusRate: 'Percentage of card value per qualifying kill, for example 75%.',
          eventInputLabel:
            'This becomes the label of the field the host fills in at round results.',
        },
        measurement: {
          title: '1. What is counted',
          description:
            'First define the source of the calculation. It does not decide the reward yet.',
          question: 'What triggers the modifier?',
          domains: {
            kills: {
              title: 'Team kills',
              description: 'Use all round kills or only kills that meet the modifier condition.',
            },
            event: {
              title: 'Another event or condition',
              description:
                'Any measurable action: a crouch, a successful shot, a completed objective, or an activation.',
            },
          },
          killQuestion: 'Which kills are counted?',
          killModes: {
            all: {
              title: 'All team kills',
              description: 'The application reads the round kill count automatically.',
            },
            qualifying: {
              title: 'Only qualifying kills',
              description: 'The host enters how many kills met the condition.',
            },
          },
          eventQuestion: 'How is the event recorded?',
          eventModes: {
            condition: {
              title: 'Condition: yes or no',
              description: 'One confirmation at round results.',
            },
            count: {
              title: 'Enter event count',
              description: 'The host enters the total number of successful events.',
            },
            perActivation: {
              title: 'Once per activation',
              description: 'Every purchase counts automatically as one event.',
            },
          },
          inputLabel: 'What should the host enter?',
          inputLabelHint: 'Write a concrete label, for example “Successful shots by the host”.',
          maximumQuestion: 'Should the count be limited by activations?',
          maximumKinds: {
            none: {
              title: 'No activation limit',
              description: 'The event may happen any number of times.',
            },
            activations: {
              title: 'Limited by activations',
              description: 'The total cannot exceed activations × events per activation.',
            },
          },
          eventsPerActivation: 'Maximum events per activation',
          eventsPerActivationHint:
            'For Lucky Shot this is 1: six activations allow at most six successful shots.',
        },
        payout: {
          title: '2. What each counted unit gives',
          description:
            'The same effect can be attached to kills, an arbitrary event, a condition, or an activation.',
          question: 'Choose the effect',
          kinds: {
            fixedPoints: {
              title: 'Fixed points',
              description: 'Adds the specified number of points for every unit.',
            },
            cardPercent: {
              title: 'Percentage of card value',
              description: 'Adds or deducts a percentage of the card value for every unit.',
            },
            bonusKills: {
              title: 'Bonus kills',
              description: 'Increases the kill counter used to calculate the card score.',
            },
            killValueIncrease: {
              title: 'Increase value of every kill',
              description: 'Every unit increases the value of each team kill. This covers Thirst.',
            },
          },
          values: {
            fixedPoints: 'Points per unit',
            cardPercent: 'Card value per unit',
            bonusKills: 'Bonus kills per unit',
            killValueIncrease: 'Kill-value increase per unit',
          },
          valueHints: {
            fixedPoints: 'A negative value can be used as a penalty.',
            cardPercent: 'For example, 75 adds 75%; −25 deducts 25% of card value.',
            bonusKills: 'A positive whole number.',
            killValueIncrease: 'A positive whole number of points.',
          },
          zeroCountPenalty: 'Penalty when the source count is zero',
          zeroCountPenaltyHint: 'Set 0 when no separate penalty is needed.',
          summary: 'Source: {{source}}. Effect: {{effect}} ({{value}} per unit).',
        },
        previewLoading: 'Building modifier preview',
        previewError: 'The authoritative preview could not be built.',
        playerView: 'Player view',
        hostView: 'Host view',
        commandPreview: 'Activation command: {{command}}',
        exampleTitle: 'Authoritative example',
        exampleResolution: {
          completed: 'rule completed',
          automatic: 'calculated automatically',
          succeeded: 'condition met',
          perActivation: 'one activation',
        },
        exampleFacts:
          'Card {{cardValue}}, kills {{killsCount}}, bounties {{bountyCount}}, input: {{resolutionExample}}.',
        exampleResult:
          'Modifier points {{pointsDelta}}, bonus kills {{bonusKillsDelta}}, final score {{finalScore}}.',
        discardTitle: 'Discard modifier draft?',
        discardDescription: 'The unsaved changes in this wizard will be lost.',
        discardConfirm: 'Discard',
      },
    },
    questions: {
      title: 'Question catalog',
      description:
        'Master list of quiz questions. Create, edit and remove questions available to any game.',
      add: 'Add question',
      importJson: 'Upload JSON',
      downloadTemplate: 'Download template',
      importGroupTitle: 'JSON import',
      importGroupDescription:
        'Bulk import: download the template, add your questions, and upload the JSON file.',
      importGroupExpand: 'Expand JSON import',
      importGroupCollapse: 'Collapse JSON import',
      categoryGroupDescription:
        'Create a category here. To rename or delete one, select it in the list below. The system category cannot be changed.',
      categoryGroupExpand: 'Expand category management',
      categoryGroupCollapse: 'Collapse category management',
      importSuccess: 'Imported {{count}} questions.',
      importPartial: 'Imported {{count}} questions. Skipped: {{skipped}}.',
      importSkippedTitle: 'Skipped questions',
      importSkippedDescription:
        'Some questions were not imported. Download the report to see which rows failed and why.',
      importErrorDescription:
        'The import could not be completed. Download the report to keep the file name and error details together.',
      downloadImportReport: 'Download report',
      importReasons: {
        invalidFields:
          'Required fields are missing or invalid. Each question must include text, answer, and a non-negative reward.',
        duplicateCodeInFile: 'The question code is duplicated inside the uploaded file.',
        categoryUnresolved: 'The selected category could not be resolved.',
        duplicateCodeExisting: 'The question code already exists in the catalog.',
      },
      addCategory: 'Add category',
      renameCategory: 'Rename category',
      deleteCategory: 'Delete category',
      deleteCategoryTitle: 'Delete category',
      deleteCategoryConfirm: 'Delete "{{name}}" from the catalog?',
      searchLabel: 'Search questions',
      loading: 'Loading questions…',
      error: 'Failed to load the question catalog.',
      empty: 'No questions yet. Add the first one.',
      loadingCategories: 'Loading categories…',
      errorCategories: 'Failed to load categories.',
      emptyCategories: 'No categories yet.',
      menuTitle: 'Question tools',
      menuDescription: 'Search the catalog, add new entries and manage categories.',
      categoryCount: '{{count}} questions',
      categoryMeta: 'Category: {{category}}',
      rewardMeta: 'Reward: {{reward}}',
      answerMeta: 'Answer: {{answer}}',
      askedMeta: 'Asked: {{asked}}',
      meta: '{{category}} · reward {{reward}} · answer: {{answer}}',
      disabledBadge: 'globally disabled',
      noCategories: 'Create at least one category before adding questions.',
      createTitle: 'New question',
      editTitle: 'Edit question',
      deleteTitle: 'Remove question',
      deleteConfirm: 'Remove this question from the catalog?',
      categoryDialog: {
        title: 'New category',
        editTitle: 'Rename category',
        description: 'Create a global category that questions can belong to.',
        editDescription: 'Update the category name used by existing questions.',
        nameLabel: 'Category name',
      },
      fields: {
        category: 'Category',
        text: 'Question',
        answer: 'Answer',
        reward: 'Reward',
        priority: 'Priority',
        isEnabled: 'Available for selection',
      },
    },
  },
  ru: {
    actions: {
      edit: 'Изменить',
      view: 'Просмотр',
      delete: 'Удалить',
    },
    common: {
      yes: 'Да',
      no: 'Нет',
    },
    errors: {
      duplicateCode: 'Такой код уже используется другой записью.',
      notFound: 'Запись не найдена. Возможно, она была удалена.',
      invalidRequest: 'Некоторые поля заполнены неверно. Проверьте форму и повторите.',
      contentLocked: 'Модификатор заблокирован, потому что он включён в активную игру.',
      categoryNotFound: 'Категория вопросов не найдена. Возможно, она была удалена.',
      categoryNotEmpty: 'Категорию, в которой есть вопросы, удалить нельзя.',
      categoryProtected: 'Системную категорию по умолчанию нельзя переименовать или удалить.',
      generic: 'Не удалось выполнить операцию. Попробуйте ещё раз.',
    },
    validation: {
      required: 'Поле обязательно.',
      code: 'Только строчные латинские буквы, цифры и подчёркивание.',
      number: 'Введите целое неотрицательное число.',
      limit: 'Оставьте пустым или введите целое положительное число.',
      formula: 'Введите корректное выражение формулы.',
      tags: 'Не больше пяти уникальных тегов длиной до 32 символов каждый.',
    },
    modifiers: {
      title: 'Каталог модификаторов',
      description:
        'Общий список модификаторов. Создавайте, редактируйте и удаляйте модификаторы, доступные любым играм.',
      add: 'Добавить модификатор',
      loading: 'Загрузка модификаторов…',
      error: 'Не удалось загрузить каталог модификаторов.',
      empty: 'Модификаторов пока нет. Добавьте первый.',
      emptyCategory: 'В этой категории модификаторов нет.',
      menuTitle: 'Инструменты каталога',
      menuDescription:
        'Ищите в каталоге, добавляйте модификаторы и фильтруйте по категориям и поведению в итогах.',
      menuHint: 'Чтобы изменить или удалить запись, используйте кнопки в строке модификатора.',
      categoryCount: 'Модификаторов: {{count}}',
      roundSummaryTitle: 'Поведение в итогах раунда',
      allRoundSummaries: 'Все варианты',
      roundSummaryCount: 'Модификаторов: {{count}}',
      meta: '{{category}} · стоимость {{cost}}',
      hostControlBadge: 'Нужен контроль ведущего',
      contentLockedBadge: 'Заблокирован активной игрой',
      contentLockedReason:
        'Модификатор включён в активную игру. Его содержимое доступно только для просмотра до завершения или архивации игры.',
      createTitle: 'Новый модификатор',
      editTitle: 'Редактирование модификатора',
      deleteTitle: 'Удалить модификатор',
      deleteConfirm: 'Удалить «{{name}}» из каталога? Он больше не будет доступен для новых игр.',
      fields: {
        name: 'Название',
        description: 'Описание',
        category: 'Когда действует модификатор',
        categoryHint: 'Выберите, на каком этапе игры работает этот модификатор.',
        requiresHostControl: 'Нужен контроль ведущего',
        mechanicType: 'Тип механики',
        activationCost: 'Стоимость активации',
        activationLimitCount: 'Лимит активаций',
        limitHint: 'Оставьте пустым, чтобы без лимита.',
        conflicts: 'Конфликты',
        conflictsHint: 'Модификаторы, которые не могут быть активны вместе с этим.',
        iconEmoji: 'Иконка (эмодзи)',
        activationCommand: 'Команда активации',
        durationSeconds: 'Длительность, секунд',
        ruleText: 'Короткое правило',
        perKillBonus: 'Очки за убийство',
        failurePenaltyPoints: 'Штраф за провал',
        killDeltaMode: 'Режим счётчика убийств',
        killDeltaValue: 'Бонус убийств',
        killCondition: 'Условие',
        excludedWeapons: 'Исключённое оружие',
        csvHint: 'Значения через запятую.',
        multiplierTarget: 'Цель множителя',
        multiplierDelta: 'Прибавка множителя',
        activeWindow: 'Окно действия',
        stopCondition: 'Условие остановки',
        mentorLoadoutText: 'Снаряжение Ментора',
        mentorCanBeRevived: 'Ментора можно поднять',
        mentorCanBeKilled: 'Ментора можно убить',
        mentorKillsCreditToTeam: 'Убийства Ментора идут команде',
      },
      sections: {
        basic: 'Основная информация',
        mechanics: 'Механика',
        availability: 'Доступность, лимиты и конфликты',
      },
      mechanics: {
        rule_only: 'Правило без очков',
        restriction_with_reward: 'Ограничение с наградой или штрафом',
        kill_counter: 'Изменение счётчика убийств',
        multiplier: 'Множитель очков или убийств',
        mentor: 'Модификатор с Ментором',
      },
      roundSummaryType: {
        passive: 'Не влияет на итоги раунда',
        automatic: 'Считается автоматически',
        condition: 'Ведущий подтверждает условие',
        manual_count: 'Ведущий вводит количество',
      },
      preview: {
        title: 'Предпросмотр формулы',
        unlimited: 'без лимита',
        limit: 'до {{count}} за игру',
        body: '{{category}} · {{mechanic}} · {{scoringType}} · {{limit}}',
        roundSummary: 'Поведение в итогах раунда: {{category}}.',
        resultInput: 'Ведущий также будет вводить: {{input}}.',
        scoreFormula: 'Формула подсчёта: {{formula}}.',
        successExpression: 'Выражение при успехе: {{expression}}.',
        failureExpression: 'Выражение при провале: {{expression}}.',
      },
      wizard: {
        step: 'Шаг {{current}} из {{total}}',
        steps: {
          0: 'Карточка',
          1: 'Условия и активация',
          2: 'Расчёт результата',
          3: 'Проверка',
        },
        stepDescriptions: {
          0: 'Опишите модификатор так, как его увидит игрок в каталоге.',
          1: 'Укажите, кто и когда выполняет правило, сколько стоит активация и с чем она несовместима.',
          2: 'Сначала выберите, что мы считаем, а затем — что даёт каждая учтённая единица.',
          3: 'Сверьте карточки игрока и ведущего и проверьте расчётный пример перед сохранением.',
        },
        sections: {
          behavior: 'Как работает правило',
          behaviorDescription:
            'Эти настройки определяют игровой этап, исполнителя и действия ведущего.',
          activation: 'Покупка и совместимость',
          activationDescription:
            'Эти настройки определяют цену, количество покупок и сочетание с другими модификаторами.',
        },
        kind: 'Что делает модификатор?',
        kinds: { rule: 'Правило без изменения счёта', scoring: 'Влияет на итог раунда' },
        tags: 'Теги для поиска',
        tagsHint: 'Выберите подсказки или введите до пяти собственных тегов.',
        suggestedTags: {
          combat: 'бой',
          mentor: 'ментор',
          movement: 'движение',
          equipment: 'снаряжение',
          communication: 'коммуникация',
          revival: 'оживление',
          environment: 'окружение',
          restriction: 'ограничение',
          weapon: 'оружие',
          bonus: 'бонус',
          penalty: 'штраф',
          timer: 'таймер',
        },
        phase: 'В какой момент действует модификатор?',
        phases: {
          preparation: 'До начала раунда',
          round: 'Во время раунда',
          result: 'При подведении итогов',
        },
        phaseDescriptions: {
          preparation: 'Выбор карточки, снаряжения, навыков и подготовка команды.',
          round: 'Условие или ограничение действует непосредственно во время игры.',
          result: 'Результат фиксируется после завершения игры.',
        },
        performer: 'Кто должен выполнить условие?',
        performers: { activeTeam: 'Команда', mentor: 'Ведущий' },
        performerDescriptions: {
          activeTeam: 'Действие или ограничение относится к активной команде.',
          mentor: 'Действие выполняет ведущий, а результат учитывается для команды.',
        },
        rule: 'Правило для команды и ведущего',
        requiresHostMonitoring: 'Нужно ли ведущему вручную проверить выполнение?',
        monitoringAnswers: { yes: 'Да, нужно проверить', no: 'Нет, всё считается автоматически' },
        monitoringDescriptions: {
          yes: 'Ведущий подтвердит выполнение или введёт результат после игры.',
          no: 'Приложение получит необходимые показатели раунда автоматически.',
        },
        durationQuestion: 'Есть ли ограничение по времени?',
        durationAnswers: { yes: 'Да, есть таймер', no: 'Нет ограничения' },
        durationDescriptions: {
          yes: 'Каждая активация добавляет отдельный интервал времени.',
          no: 'Правило действует без обратного отсчёта.',
        },
        durationHint: 'Необязательно. Оставьте пустым, если таймера нет.',
        commandHint: 'Оставьте пустым — команда сформируется из названия.',
        advancedSettings: 'Дополнительные настройки',
        advancedSettingsDescription: 'Команда активации обычно создаётся автоматически.',
        reward: 'Что изменяется',
        rewards: { points: 'Очки', bonusKills: 'Бонусные убийства' },
        resolution: 'Как фиксируется факт',
        resolutions: {
          automaticRoundMetric: 'Автоматически из показателей раунда',
          boolean: 'Ведущий выбирает «Удалось / Не удалось»',
          nonNegativeCount: 'Ведущий вводит неотрицательное количество',
        },
        formula: 'Способ расчёта',
        formulaHint: 'Показаны только совместимые встроенные способы.',
        impactGuideTitle: 'Никаких формул вручную',
        impactGuideDescription:
          'Выберите подходящий сценарий. Форма сама настроит, что начисляется и какие данные должен ввести ведущий.',
        formulaConfigTitle: 'Будет настроено автоматически',
        formulaConfig: 'Результат: {{reward}}. Факт фиксируется: {{resolution}}.',
        impactTargetQuestion: 'Что модификатор добавляет к результату?',
        impactTargetHint: 'Сначала выберите результат. Способ расчёта появится следующим вопросом.',
        impactTargets: {
          points: {
            title: 'Очки',
            description: 'Меняется сумма очков за раунд, но не количество убийств.',
          },
          bonusKills: {
            title: 'Дополнительные убийства',
            description: 'К фактическим убийствам добавляются виртуальные перед расчётом карточки.',
          },
        },
        impactMethodQuestions: {
          points: 'Как начисляются дополнительные очки?',
          bonusKills: 'Как определяется количество дополнительных убийств?',
        },
        formulas: {
          growing_kill_value: 'За все убийства команды',
          bonus_kill_on_condition: 'Один раз, если условие выполнено',
          bonus_kills_by_count: 'По количеству событий',
          window_kill_bonus_points: 'Только за подходящие убийства',
        },
        formulaDescriptions: {
          growing_kill_value:
            'Приложение само возьмёт общее количество убийств. Подходит для механики вроде «Жажды».',
          bonus_kill_on_condition:
            'Ведущий ответит «Выполнено / Не выполнено». При успехе команда получит фиксированный бонус.',
          bonus_kills_by_count:
            'Ведущий введёт число срабатываний, а каждое добавит заданное количество убийств.',
          window_kill_bonus_points:
            'Ведущий введёт количество подходящих убийств. Каждое принесёт процент стоимости карточки.',
        },
        impactSettingsTitle: 'Настройте результат',
        impactSettings: {
          growing_kill_value:
            'За каждое убийство повышаем стоимость карточки; при нуле убийств можно применить штраф.',
          bonus_kill_on_condition:
            'Укажите, сколько убийств получит команда, если ведущий подтвердит выполнение условия.',
          bonus_kills_by_count:
            'Укажите, сколько убийств добавляет каждое событие, введённое ведущим.',
          window_kill_bonus_points:
            'Укажите, какой процент стоимости карточки приносит каждое подходящее убийство.',
        },
        units: { points: 'очков', kills: 'убийств', seconds: 'сек.' },
        calculationExampleTitle: 'Как это будет считаться',
        calculationExamples: {
          growing_kill_value:
            'Карточка стоит {{cardValue}} очков, команда сделала {{killsCount}} убийства.\nНовая стоимость: {{cardValue}} + {{killsCount}} × {{increment}} = {{increasedCardValue}}.\nИтог: {{increasedCardValue}} × {{killsCount}} = {{result}} очков. При нуле убийств штраф составит {{penalty}} очков.',
          bonus_kill_on_condition:
            'Карточка стоит {{cardValue}} очков, команда сделала {{killsCount}} убийства и выполнила условие.\nУчитываемые убийства: {{killsCount}} + {{bonus}} = {{resultUnits}}.\nИтог: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} очков.',
          bonus_kills_by_count:
            'Карточка стоит {{cardValue}} очков, команда сделала {{killsCount}} убийства. Ведущий ввёл {{inputCount}} события.\nБонус: {{inputCount}} × {{perUnit}} = {{bonus}} убийства.\nИтог: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} очков.',
          window_kill_bonus_points:
            'Карточка стоит {{cardValue}} очков, команда сделала {{killsCount}} убийства, из них {{inputCount}} подходят под условие.\nБонус: {{inputCount}} × {{cardValue}} × {{percent}}% = {{bonus}} очков.\nИтог: {{killsCount}} × {{cardValue}} + {{bonus}} = {{result}} очков.',
        },
        parameters: {
          incrementPointsPerKill: 'Рост стоимости за убийство',
          zeroKillPenaltyPoints: 'Штраф при отсутствии убийств',
          successBonusKills: 'Бонусных убийств при успехе',
          bonusKillsPerUnit: 'Бонусных убийств за единицу',
          bonusRate: 'Процент стоимости карточки за убийство',
        },
        help: {
          kind: 'Выберите правило, если модификатор только ограничивает действия. Выберите влияние на итог, если он меняет очки или число убийств.',
          name: 'Отображается игрокам, ведущему, в каталоге и истории раундов.',
          description:
            'Публичное объяснение для игрока: что произойдёт после покупки. Не прячьте здесь важные условия.',
          iconEmoji: 'Показывается рядом с названием и помогает быстро найти модификатор.',
          tags: 'Используются только для поиска и фильтрации. На расчёт и правила не влияют.',
          phase:
            'Определяет, когда правило применяется: до старта карточки, во время игры или при подведении итогов.',
          performer: 'Указывает, кто выполняет действие: активная команда или ведущий.',
          rule: 'Точная инструкция для ведущего и команды. По ней ведущий контролирует выполнение и закрывает раунд.',
          requiresHostMonitoring:
            'Добавляет отметку о ручном контроле. Включайте, если приложение не может проверить условие самостоятельно.',
          durationSeconds:
            'Время действия одной активации. Каждая дополнительная активация добавляет ещё такой же интервал.',
          activationCost: 'Столько очков викторины списывается у игрока при каждой покупке.',
          activationLimitCount:
            'Максимальное число активаций этого модификатора в одном раунде. Пустое поле снимает лимит.',
          conflicts:
            'Если выбранный модификатор уже активен в раунде, купить этот будет нельзя — и наоборот.',
          activationCommand:
            'Команда, которой игрок заказывает модификатор. Если оставить пустой, она создастся из названия.',
          formulaCode:
            'Готовый сценарий определяет результат, ввод ведущего и формулу. Выбирайте по смыслу правила, а не по техническому названию.',
          incrementPointsPerKill:
            'На сколько очков одна активация увеличивает стоимость убийства за каждое сделанное убийство.',
          zeroKillPenaltyPoints:
            'Сколько очков снимает каждая активация, если команда не сделала ни одного убийства.',
          successBonusKills:
            'Сколько бонусных убийств добавить, когда ведущий отмечает условие выполненным.',
          bonusKillsPerUnit:
            'Сколько бонусных убийств даёт каждое срабатывание, введённое ведущим.',
          bonusRate: 'Процент стоимости карточки за одно подходящее убийство: например, 75%.',
          eventInputLabel:
            'Это название поля, которое ведущий увидит при подведении итогов раунда.',
        },
        measurement: {
          title: '1. Что считаем',
          description: 'Сначала определите источник расчёта. Награду выберем отдельно.',
          question: 'От чего срабатывает модификатор?',
          domains: {
            kills: {
              title: 'Убийства команды',
              description: 'Можно взять все убийства раунда или только подходящие под условие.',
            },
            event: {
              title: 'Другое событие или условие',
              description:
                'Любое измеримое действие: приседание, успешный выстрел, выполненная цель или активация.',
            },
          },
          killQuestion: 'Какие убийства учитывать?',
          killModes: {
            all: {
              title: 'Все убийства команды',
              description: 'Приложение автоматически возьмёт итоговый счётчик убийств.',
            },
            qualifying: {
              title: 'Только подходящие убийства',
              description: 'Ведущий вручную укажет, сколько убийств выполнили условие.',
            },
          },
          eventQuestion: 'Как фиксируется событие?',
          eventModes: {
            condition: {
              title: 'Условие: да или нет',
              description: 'При подведении итогов ведущий один раз подтверждает выполнение.',
            },
            count: {
              title: 'Ввести количество событий',
              description: 'Ведущий указывает общее число успешных событий.',
            },
            perActivation: {
              title: 'Один раз за активацию',
              description: 'Каждая покупка автоматически считается одним событием.',
            },
          },
          inputLabel: 'Что должен ввести ведущий?',
          inputLabelHint: 'Напишите конкретно, например: «Успешные убийства ведущего».',
          maximumQuestion: 'Ограничить количество числом активаций?',
          maximumKinds: {
            none: {
              title: 'Без ограничения по активациям',
              description: 'Событие может произойти сколько угодно раз.',
            },
            activations: {
              title: 'Зависит от активаций',
              description: 'Итог не превысит число активаций × событий на активацию.',
            },
          },
          eventsPerActivation: 'Максимум событий на одну активацию',
          eventsPerActivationHint:
            'Для Lucky Shot это 1: шесть активаций допускают максимум шесть успешных выстрелов.',
        },
        payout: {
          title: '2. Что даёт каждая единица',
          description:
            'Один и тот же эффект можно связать с убийствами, произвольным событием, условием или активацией.',
          question: 'Выберите эффект',
          kinds: {
            fixedPoints: {
              title: 'Фиксированные очки',
              description: 'Добавляет указанное количество очков за каждую единицу.',
            },
            cardPercent: {
              title: 'Процент стоимости карточки',
              description: 'За каждую единицу добавляет или вычитает процент стоимости карточки.',
            },
            bonusKills: {
              title: 'Бонусные убийства',
              description: 'Увеличивает счётчик убийств, по которому считается стоимость карточки.',
            },
            killValueIncrease: {
              title: 'Рост стоимости каждого убийства',
              description:
                'Каждая единица повышает стоимость всех убийств команды. Так работает «Жажда».',
            },
          },
          values: {
            fixedPoints: 'Очков за единицу',
            cardPercent: 'Процент карточки за единицу',
            bonusKills: 'Бонусных убийств за единицу',
            killValueIncrease: 'Рост стоимости убийства за единицу',
          },
          valueHints: {
            fixedPoints: 'Отрицательное значение можно использовать как штраф.',
            cardPercent: 'Например, 75 добавит 75%, а −25 вычтет 25% стоимости карточки.',
            bonusKills: 'Положительное целое число.',
            killValueIncrease: 'Положительное целое количество очков.',
          },
          zeroCountPenalty: 'Штраф, если источник равен нулю',
          zeroCountPenaltyHint: 'Укажите 0, если отдельный штраф не нужен.',
          summary: 'Источник: {{source}}. Эффект: {{effect}} ({{value}} за единицу).',
        },
        previewLoading: 'Формируем предпросмотр модификатора',
        previewError: 'Не удалось построить авторитетный предпросмотр.',
        playerView: 'Карточка игрока',
        hostView: 'Карточка ведущего',
        commandPreview: 'Команда активации: {{command}}',
        exampleTitle: 'Проверочный пример',
        exampleResolution: {
          completed: 'правило выполнено',
          automatic: 'считается автоматически',
          succeeded: 'условие выполнено',
          perActivation: 'одна активация',
        },
        exampleFacts:
          'Карточка {{cardValue}}, убийств {{killsCount}}, наград {{bountyCount}}, ввод: {{resolutionExample}}.',
        exampleResult:
          'Очки модификатора {{pointsDelta}}, бонусные убийства {{bonusKillsDelta}}, итог {{finalScore}}.',
        discardTitle: 'Отменить черновик модификатора?',
        discardDescription: 'Несохранённые изменения в мастере будут потеряны.',
        discardConfirm: 'Отменить изменения',
      },
    },
    questions: {
      title: 'Каталог вопросов',
      description:
        'Общий список вопросов викторины. Создавайте, редактируйте и удаляйте вопросы, доступные любым играм.',
      add: 'Добавить вопрос',
      importJson: 'Загрузить JSON',
      downloadTemplate: 'Скачать шаблон',
      importGroupTitle: 'Импорт из JSON',
      importGroupDescription:
        'Массовая загрузка: скачайте шаблон, заполните вопросы и загрузите JSON.',
      importGroupExpand: 'Развернуть импорт из JSON',
      importGroupCollapse: 'Свернуть импорт из JSON',
      categoryGroupDescription:
        'Здесь можно создать категорию. Чтобы переименовать или удалить — выберите её в списке ниже. «БЕЗ КАТЕГОРИИ» изменить нельзя.',
      categoryGroupExpand: 'Развернуть управление категориями',
      categoryGroupCollapse: 'Свернуть управление категориями',
      importSuccess: 'Импортировано вопросов: {{count}}.',
      importPartial: 'Импортировано вопросов: {{count}}. Пропущено: {{skipped}}.',
      importSkippedTitle: 'Пропущенные вопросы',
      importSkippedDescription:
        'Некоторые вопросы не удалось импортировать. Скачайте отчёт, чтобы увидеть, какие строки не прошли и почему.',
      importErrorDescription:
        'Импорт не удалось завершить. Скачайте отчёт, чтобы сохранить имя файла и детали ошибки вместе.',
      downloadImportReport: 'Скачать отчёт',
      importReasons: {
        invalidFields:
          'Не заполнены обязательные поля или в них есть ошибка. У вопроса должны быть текст, ответ и неотрицательная награда.',
        duplicateCodeInFile: 'Код вопроса дублируется внутри загруженного файла.',
        categoryUnresolved: 'Не удалось определить выбранную категорию.',
        duplicateCodeExisting: 'Такой код вопроса уже есть в каталоге.',
      },
      addCategory: 'Добавить категорию',
      renameCategory: 'Переименовать категорию',
      deleteCategory: 'Удалить категорию',
      deleteCategoryTitle: 'Удалить категорию',
      deleteCategoryConfirm: 'Удалить категорию «{{name}}» из каталога?',
      searchLabel: 'Поиск вопросов',
      loading: 'Загрузка вопросов…',
      error: 'Не удалось загрузить каталог вопросов.',
      empty: 'Вопросов пока нет. Добавьте первый.',
      loadingCategories: 'Загрузка категорий…',
      errorCategories: 'Не удалось загрузить категории.',
      emptyCategories: 'Категорий пока нет.',
      menuTitle: 'Инструменты каталога',
      menuDescription: 'Ищите вопросы, добавляйте новые записи и управляйте категориями.',
      categoryCount: 'Вопросов: {{count}}',
      categoryMeta: 'Категория: {{category}}',
      rewardMeta: 'Награда: {{reward}}',
      answerMeta: 'Ответ: {{answer}}',
      askedMeta: 'Задавали: {{asked}}',
      meta: '{{category}} · награда {{reward}} · ответ: {{answer}}',
      disabledBadge: 'выключен глобально',
      noCategories: 'Сначала создайте хотя бы одну категорию, а потом добавляйте вопросы.',
      createTitle: 'Новый вопрос',
      editTitle: 'Редактирование вопроса',
      deleteTitle: 'Удалить вопрос',
      deleteConfirm: 'Удалить этот вопрос из каталога?',
      categoryDialog: {
        title: 'Новая категория',
        editTitle: 'Переименовать категорию',
        description: 'Создайте глобальную категорию, к которой затем можно привязывать вопросы.',
        editDescription: 'Измените название категории, которая уже используется вопросами.',
        nameLabel: 'Название категории',
      },
      fields: {
        category: 'Категория',
        text: 'Вопрос',
        answer: 'Ответ',
        reward: 'Награда',
        priority: 'Приоритет',
        isEnabled: 'Доступен для выбора',
      },
    },
  },
  uk: {
    actions: {
      edit: 'Редагувати',
      view: 'Переглянути',
      delete: 'Видалити',
    },
    common: {
      yes: 'Так',
      no: 'Ні',
    },
    errors: {
      duplicateCode: 'Цей код вже використовується іншим записом.',
      notFound: 'Запис не знайдено. Можливо, його було видалено.',
      invalidRequest: 'Деякі поля заповнені неправильно. Перевірте форму та повторіть.',
      contentLocked: 'Модифікатор заблоковано, тому що його включено до активної гри.',
      categoryNotFound: 'Категорію запитань не знайдено. Можливо, її було видалено.',
      categoryNotEmpty: 'Категорію, у якій є запитання, не можна видалити.',
      categoryProtected: 'Системну категорію за замовчуванням не можна перейменувати або видалити.',
      generic: 'Не вдалося виконати операцію. Спробуйте ще раз.',
    },
    validation: {
      required: 'Поле обовʼязкове.',
      code: 'Лише малі латинські літери, цифри та підкреслення.',
      number: 'Введіть ціле невідʼємне число.',
      limit: 'Залиште порожнім або введіть ціле додатне число.',
      formula: 'Введіть коректний вираз формули.',
      tags: 'Не більше п’яти унікальних тегів довжиною до 32 символів кожен.',
    },
    modifiers: {
      title: 'Каталог модифікаторів',
      description:
        'Загальний список модифікаторів. Створюйте, редагуйте та видаляйте модифікатори, доступні будь-яким іграм.',
      add: 'Додати модифікатор',
      loading: 'Завантаження модифікаторів…',
      error: 'Не вдалося завантажити каталог модифікаторів.',
      empty: 'Модифікаторів поки немає. Додайте перший.',
      emptyCategory: 'У цій категорії модифікаторів немає.',
      menuTitle: 'Інструменти каталогу',
      menuDescription:
        'Шукайте в каталозі, додавайте модифікатори та фільтруйте за категоріями й поведінкою у підсумках.',
      menuHint: 'Щоб змінити або видалити запис, використовуйте кнопки в рядку модифікатора.',
      categoryCount: 'Модифікаторів: {{count}}',
      roundSummaryTitle: 'Поведінка у підсумках раунду',
      allRoundSummaries: 'Усі варіанти',
      roundSummaryCount: 'Модифікаторів: {{count}}',
      meta: '{{category}} · вартість {{cost}}',
      hostControlBadge: 'Потрібен контроль ведучого',
      contentLockedBadge: 'Заблоковано активною грою',
      contentLockedReason:
        'Модифікатор включено до активної гри. Його вміст доступний лише для перегляду до завершення або архівації гри.',
      createTitle: 'Новий модифікатор',
      editTitle: 'Редагування модифікатора',
      deleteTitle: 'Видалити модифікатор',
      deleteConfirm: 'Видалити «{{name}}» з каталогу? Він більше не буде доступний для нових ігор.',
      fields: {
        name: 'Назва',
        description: 'Опис',
        category: 'Коли діє модифікатор',
        categoryHint: 'Оберіть, на якому етапі гри працює цей модифікатор.',
        requiresHostControl: 'Потрібен контроль ведучого',
        mechanicType: 'Тип механіки',
        activationCost: 'Вартість активації',
        activationLimitCount: 'Ліміт активацій',
        limitHint: 'Залиште порожнім, щоб без ліміту.',
        conflicts: 'Конфлікти',
        conflictsHint: 'Модифікатори, які не можуть бути активні разом із цим.',
        iconEmoji: 'Іконка (емодзі)',
        activationCommand: 'Команда активації',
        durationSeconds: 'Тривалість, секунд',
        ruleText: 'Коротке правило',
        perKillBonus: 'Очки за вбивство',
        failurePenaltyPoints: 'Штраф за провал',
        killDeltaMode: 'Режим лічильника вбивств',
        killDeltaValue: 'Бонус вбивств',
        killCondition: 'Умова',
        excludedWeapons: 'Виключена зброя',
        csvHint: 'Значення через кому.',
        multiplierTarget: 'Ціль множника',
        multiplierDelta: 'Додаток множника',
        activeWindow: 'Вікно дії',
        stopCondition: 'Умова зупинки',
        mentorLoadoutText: 'Спорядження Ментора',
        mentorCanBeRevived: 'Ментора можна підняти',
        mentorCanBeKilled: 'Ментора можна вбити',
        mentorKillsCreditToTeam: 'Вбивства Ментора йдуть команді',
      },
      sections: {
        basic: 'Основна інформація',
        mechanics: 'Механіка',
        availability: 'Доступність, ліміти та конфлікти',
      },
      mechanics: {
        rule_only: 'Правило без очок',
        restriction_with_reward: 'Обмеження з нагородою або штрафом',
        kill_counter: 'Зміна лічильника вбивств',
        multiplier: 'Множник очок або вбивств',
        mentor: 'Модифікатор з Ментором',
      },
      roundSummaryType: {
        passive: 'Не впливає на підсумки раунду',
        automatic: 'Розраховується автоматично',
        condition: 'Ведучий підтверджує умову',
        manual_count: 'Ведучий вводить кількість',
      },
      preview: {
        title: 'Попередній перегляд формули',
        unlimited: 'без ліміту',
        limit: 'до {{count}} за гру',
        body: '{{category}} · {{mechanic}} · {{scoringType}} · {{limit}}',
        roundSummary: 'Поведінка у підсумках раунду: {{category}}.',
        resultInput: 'Ведучий також буде вводити: {{input}}.',
        scoreFormula: 'Формула підрахунку: {{formula}}.',
        successExpression: 'Вираз при успіху: {{expression}}.',
        failureExpression: 'Вираз при провалі: {{expression}}.',
      },
      wizard: {
        step: 'Крок {{current}} з {{total}}',
        steps: {
          0: 'Картка',
          1: 'Умови й активація',
          2: 'Розрахунок результату',
          3: 'Перевірка',
        },
        stepDescriptions: {
          0: 'Опишіть модифікатор так, як його побачить гравець у каталозі.',
          1: 'Вкажіть, хто й коли виконує правило, скільки коштує активація та з чим вона несумісна.',
          2: 'Спочатку оберіть, що рахуємо, а потім — що дає кожна врахована одиниця.',
          3: 'Звірте картки гравця й ведучого та перевірте розрахунковий приклад перед збереженням.',
        },
        sections: {
          behavior: 'Як працює правило',
          behaviorDescription: 'Ці налаштування визначають етап гри, виконавця та дії ведучого.',
          activation: 'Купівля та сумісність',
          activationDescription:
            'Ці налаштування визначають ціну, кількість покупок і сумісність з іншими модифікаторами.',
        },
        kind: 'Що робить модифікатор?',
        kinds: { rule: 'Правило без зміни рахунку', scoring: 'Впливає на підсумок раунду' },
        tags: 'Теги для пошуку',
        tagsHint: 'Оберіть підказки або введіть до п’яти власних тегів.',
        suggestedTags: {
          combat: 'бій',
          mentor: 'ментор',
          movement: 'рух',
          equipment: 'спорядження',
          communication: 'комунікація',
          revival: 'оживлення',
          environment: 'оточення',
          restriction: 'обмеження',
          weapon: 'зброя',
          bonus: 'бонус',
          penalty: 'штраф',
          timer: 'таймер',
        },
        phase: 'Коли діє модифікатор?',
        phases: {
          preparation: 'До початку раунду',
          round: 'Під час раунду',
          result: 'Під час підбиття підсумків',
        },
        phaseDescriptions: {
          preparation: 'Вибір картки, спорядження, навичок і підготовка команди.',
          round: 'Умова або обмеження діє безпосередньо під час гри.',
          result: 'Результат фіксується після завершення гри.',
        },
        performer: 'Хто має виконати умову?',
        performers: { activeTeam: 'Команда', mentor: 'Ведучий' },
        performerDescriptions: {
          activeTeam: 'Дія або обмеження стосується активної команди.',
          mentor: 'Дію виконує ведучий, а результат зараховується команді.',
        },
        rule: 'Правило для команди та ведучого',
        requiresHostMonitoring: 'Чи має ведучий перевірити виконання вручну?',
        monitoringAnswers: { yes: 'Так, потрібно перевірити', no: 'Ні, рахувати автоматично' },
        monitoringDescriptions: {
          yes: 'Ведучий підтвердить виконання або введе результат після гри.',
          no: 'Застосунок отримає потрібні показники раунду автоматично.',
        },
        durationQuestion: 'Чи є обмеження за часом?',
        durationAnswers: { yes: 'Так, є таймер', no: 'Немає обмеження' },
        durationDescriptions: {
          yes: 'Кожна активація додає окремий часовий інтервал.',
          no: 'Правило діє без зворотного відліку.',
        },
        durationHint: 'Необов’язково. Залиште порожнім, якщо таймера немає.',
        commandHint: 'Залиште порожнім — команда сформується з назви.',
        advancedSettings: 'Додаткові налаштування',
        advancedSettingsDescription: 'Команда активації зазвичай створюється автоматично.',
        reward: 'Що змінюється',
        rewards: { points: 'Очки', bonusKills: 'Бонусні вбивства' },
        resolution: 'Як фіксується факт',
        resolutions: {
          automaticRoundMetric: 'Автоматично з показників раунду',
          boolean: 'Ведучий обирає «Вдалося / Не вдалося»',
          nonNegativeCount: 'Ведучий вводить невід’ємну кількість',
        },
        formula: 'Спосіб розрахунку',
        formulaHint: 'Показано лише сумісні вбудовані способи.',
        impactGuideTitle: 'Жодних формул вручну',
        impactGuideDescription:
          'Оберіть відповідний сценарій. Форма сама налаштує результат і дані, які має ввести ведучий.',
        formulaConfigTitle: 'Буде налаштовано автоматично',
        formulaConfig: 'Результат: {{reward}}. Факт фіксується: {{resolution}}.',
        impactTargetQuestion: 'Що модифікатор додає до результату?',
        impactTargetHint: 'Спочатку оберіть результат. Спосіб розрахунку з’явиться далі.',
        impactTargets: {
          points: {
            title: 'Очки',
            description: 'Змінюється сума очок за раунд, але не кількість убивств.',
          },
          bonusKills: {
            title: 'Додаткові вбивства',
            description: 'До фактичних убивств додаються віртуальні перед розрахунком картки.',
          },
        },
        impactMethodQuestions: {
          points: 'Як нараховуються додаткові очки?',
          bonusKills: 'Як визначається кількість додаткових убивств?',
        },
        formulas: {
          growing_kill_value: 'За всі вбивства команди',
          bonus_kill_on_condition: 'Один раз, якщо умову виконано',
          bonus_kills_by_count: 'За кількістю подій',
          window_kill_bonus_points: 'Лише за відповідні вбивства',
        },
        formulaDescriptions: {
          growing_kill_value:
            'Застосунок сам отримає загальну кількість убивств. Підходить для механіки на кшталт «Спраги».',
          bonus_kill_on_condition:
            'Ведучий обере «Виконано / Не виконано». За успіх команда отримає фіксований бонус.',
          bonus_kills_by_count:
            'Ведучий введе кількість спрацювань, а кожне додасть задану кількість убивств.',
          window_kill_bonus_points:
            'Ведучий введе кількість відповідних убивств. Кожне принесе відсоток вартості картки.',
        },
        impactSettingsTitle: 'Налаштуйте результат',
        impactSettings: {
          growing_kill_value:
            'За кожне вбивство підвищуємо вартість картки; за нуль убивств можна застосувати штраф.',
          bonus_kill_on_condition:
            'Укажіть кількість убивств, яку отримає команда після підтвердження умови.',
          bonus_kills_by_count: 'Укажіть, скільки вбивств додає кожна введена ведучим подія.',
          window_kill_bonus_points:
            'Укажіть відсоток вартості картки за кожне відповідне вбивство.',
        },
        units: { points: 'очок', kills: 'убивств', seconds: 'сек.' },
        calculationExampleTitle: 'Як це буде розраховано',
        calculationExamples: {
          growing_kill_value:
            'Картка коштує {{cardValue}} очок, команда зробила {{killsCount}} вбивства.\nНова вартість: {{cardValue}} + {{killsCount}} × {{increment}} = {{increasedCardValue}}.\nРезультат: {{increasedCardValue}} × {{killsCount}} = {{result}} очок. За нуль убивств штраф становить {{penalty}} очок.',
          bonus_kill_on_condition:
            'Картка коштує {{cardValue}} очок, команда зробила {{killsCount}} вбивства й виконала умову.\nЗараховані вбивства: {{killsCount}} + {{bonus}} = {{resultUnits}}.\nРезультат: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} очок.',
          bonus_kills_by_count:
            'Картка коштує {{cardValue}} очок, команда зробила {{killsCount}} вбивства. Ведучий увів {{inputCount}} події.\nБонус: {{inputCount}} × {{perUnit}} = {{bonus}} вбивства.\nРезультат: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} очок.',
          window_kill_bonus_points:
            'Картка коштує {{cardValue}} очок, команда зробила {{killsCount}} вбивства, з них {{inputCount}} відповідають умові.\nБонус: {{inputCount}} × {{cardValue}} × {{percent}}% = {{bonus}} очок.\nРезультат: {{killsCount}} × {{cardValue}} + {{bonus}} = {{result}} очок.',
        },
        parameters: {
          incrementPointsPerKill: 'Зростання вартості за вбивство',
          zeroKillPenaltyPoints: 'Штраф за відсутності вбивств',
          successBonusKills: 'Бонусних вбивств при успіху',
          bonusKillsPerUnit: 'Бонусних вбивств за одиницю',
          bonusRate: 'Відсоток вартості картки за вбивство',
        },
        help: {
          kind: 'Оберіть правило, якщо модифікатор лише обмежує дії. Оберіть вплив на підсумок, якщо він змінює очки або вбивства.',
          name: 'Відображається гравцям і ведучому в каталозі та історії раундів.',
          description:
            'Публічне пояснення для гравця: що станеться після купівлі. Не приховуйте тут важливі умови.',
          iconEmoji: 'Показується поруч із назвою та допомагає швидко впізнати модифікатор.',
          tags: 'Використовуються лише для пошуку й фільтрації. На правила та підрахунок не впливають.',
          phase: 'Визначає, коли діє правило: під час підготовки, гри або підбиття підсумків.',
          performer: 'Вказує, хто виконує дію: активна команда або ведучий.',
          rule: 'Точна інструкція для ведучого й команди, за якою контролюється виконання.',
          requiresHostMonitoring:
            'Додає позначку ручного контролю. Увімкніть, якщо застосунок не може перевірити умову самостійно.',
          durationSeconds:
            'Тривалість однієї активації. Кожна додаткова активація додає такий самий інтервал.',
          activationCost: 'Стільки очок вікторини списується з гравця за кожну активацію.',
          activationLimitCount:
            'Максимальна кількість активацій у межах одного раунду. Порожнє поле знімає ліміт.',
          conflicts:
            'Якщо обраний модифікатор уже активний у раунді, цей купити не можна — і навпаки.',
          activationCommand:
            'Команда для замовлення модифікатора. Якщо залишити порожньою, вона створиться з назви.',
          formulaCode:
            'Готовий сценарій визначає результат, ввід ведучого та формулу. Обирайте за змістом правила.',
          incrementPointsPerKill:
            'На скільки очок одна активація збільшує вартість вбивства за кожне зроблене вбивство.',
          zeroKillPenaltyPoints:
            'Скільки очок знімає кожна активація, якщо команда не зробила жодного вбивства.',
          successBonusKills: 'Скільки бонусних вбивств додати, коли ведучий відмічає успіх.',
          bonusKillsPerUnit: 'Скільки бонусних вбивств дає кожне спрацювання, введене ведучим.',
          bonusRate: 'Відсоток вартості картки за одне відповідне вбивство, наприклад 75%.',
          eventInputLabel: 'Назва поля, яке ведучий побачить під час підбиття підсумків.',
        },
        measurement: {
          title: '1. Що рахуємо',
          description: 'Спочатку визначте джерело розрахунку. Нагорода налаштовується окремо.',
          question: 'Від чого спрацьовує модифікатор?',
          domains: {
            kills: {
              title: 'Вбивства команди',
              description: 'Усі вбивства або лише ті, що відповідають умові.',
            },
            event: {
              title: 'Інша подія або умова',
              description: 'Будь-яка вимірювана дія, успішний постріл, ціль або активація.',
            },
          },
          killQuestion: 'Які вбивства враховувати?',
          killModes: {
            all: {
              title: 'Усі вбивства команди',
              description: 'Застосунок автоматично бере підсумковий лічильник.',
            },
            qualifying: {
              title: 'Лише відповідні вбивства',
              description: 'Ведучий вводить кількість вбивств, що виконали умову.',
            },
          },
          eventQuestion: 'Як фіксується подія?',
          eventModes: {
            condition: {
              title: 'Умова: так або ні',
              description: 'Одне підтвердження під час підбиття підсумків.',
            },
            count: {
              title: 'Ввести кількість подій',
              description: 'Ведучий вводить загальну кількість успішних подій.',
            },
            perActivation: {
              title: 'Один раз за активацію',
              description: 'Кожна покупка автоматично рахується як одна подія.',
            },
          },
          inputLabel: 'Що має ввести ведучий?',
          inputLabelHint: 'Напишіть конкретну назву поля.',
          maximumQuestion: 'Обмежити кількість активаціями?',
          maximumKinds: {
            none: {
              title: 'Без обмеження',
              description: 'Подія може відбутися будь-яку кількість разів.',
            },
            activations: {
              title: 'Залежить від активацій',
              description: 'Підсумок не перевищує активації × події на активацію.',
            },
          },
          eventsPerActivation: 'Максимум подій на активацію',
          eventsPerActivationHint:
            'Для Lucky Shot це 1: шість активацій дозволяють максимум шість успішних пострілів.',
        },
        payout: {
          title: '2. Що дає кожна одиниця',
          description:
            'Ефект можна пов’язати з убивствами, довільною подією, умовою або активацією.',
          question: 'Оберіть ефект',
          kinds: {
            fixedPoints: {
              title: 'Фіксовані очки',
              description: 'Додає вказану кількість очок за кожну одиницю.',
            },
            cardPercent: {
              title: 'Відсоток вартості картки',
              description: 'За кожну одиницю додає або віднімає відсоток вартості картки.',
            },
            bonusKills: {
              title: 'Бонусні вбивства',
              description: 'Збільшує лічильник убивств для розрахунку картки.',
            },
            killValueIncrease: {
              title: 'Зростання вартості вбивства',
              description: 'Кожна одиниця підвищує вартість усіх убивств команди.',
            },
          },
          values: {
            fixedPoints: 'Очок за одиницю',
            cardPercent: 'Відсоток картки за одиницю',
            bonusKills: 'Бонусних убивств за одиницю',
            killValueIncrease: 'Зростання вартості за одиницю',
          },
          valueHints: {
            fixedPoints: 'Від’ємне значення можна використати як штраф.',
            cardPercent: 'Наприклад, 75 додасть 75%, а −25 відніме 25% вартості картки.',
            bonusKills: 'Додатне ціле число.',
            killValueIncrease: 'Додатне ціле число очок.',
          },
          zeroCountPenalty: 'Штраф, якщо джерело дорівнює нулю',
          zeroCountPenaltyHint: 'Вкажіть 0, якщо окремий штраф не потрібен.',
          summary: 'Джерело: {{source}}. Ефект: {{effect}} ({{value}} за одиницю).',
        },
        previewLoading: 'Формуємо попередній перегляд модифікатора',
        previewError: 'Не вдалося побудувати авторитетний попередній перегляд.',
        playerView: 'Картка гравця',
        hostView: 'Картка ведучого',
        commandPreview: 'Команда активації: {{command}}',
        exampleTitle: 'Перевірочний приклад',
        exampleResolution: {
          completed: 'правило виконано',
          automatic: 'обчислюється автоматично',
          succeeded: 'умову виконано',
          perActivation: 'одна активація',
        },
        exampleFacts:
          'Картка {{cardValue}}, вбивств {{killsCount}}, нагород {{bountyCount}}, ввід: {{resolutionExample}}.',
        exampleResult:
          'Очки модифікатора {{pointsDelta}}, бонусні вбивства {{bonusKillsDelta}}, підсумок {{finalScore}}.',
        discardTitle: 'Відкинути чернетку модифікатора?',
        discardDescription: 'Незбережені зміни в майстрі буде втрачено.',
        discardConfirm: 'Відкинути',
      },
    },
    questions: {
      title: 'Каталог запитань',
      description:
        'Загальний список запитань вікторини. Створюйте, редагуйте та видаляйте запитання, доступні будь-яким іграм.',
      add: 'Додати запитання',
      importJson: 'Завантажити JSON',
      downloadTemplate: 'Завантажити шаблон',
      importGroupTitle: 'Імпорт з JSON',
      importGroupDescription:
        'Масове додавання: завантажте шаблон, заповніть запитання та завантажте JSON.',
      importGroupExpand: 'Розгорнути імпорт з JSON',
      importGroupCollapse: 'Згорнути імпорт з JSON',
      categoryGroupDescription:
        'Тут можна створити категорію. Щоб перейменувати або видалити — оберіть її в списку нижче. «БЕЗ КАТЕГОРИИ» змінити не можна.',
      categoryGroupExpand: 'Розгорнути керування категоріями',
      categoryGroupCollapse: 'Згорнути керування категоріями',
      importSuccess: 'Імпортовано запитань: {{count}}.',
      importPartial: 'Імпортовано запитань: {{count}}. Пропущено: {{skipped}}.',
      importSkippedTitle: 'Пропущені запитання',
      importSkippedDescription:
        'Деякі запитання не вдалося імпортувати. Завантажте звіт, щоб побачити, які рядки не пройшли і чому.',
      importErrorDescription:
        'Імпорт не вдалося завершити. Завантажте звіт, щоб зберегти ім’я файлу та деталі помилки разом.',
      downloadImportReport: 'Завантажити звіт',
      importReasons: {
        invalidFields:
          'Не заповнені обов’язкові поля або в них є помилка. Запитання повинно містити текст, відповідь і невід’ємну нагороду.',
        duplicateCodeInFile: 'Код запитання дублюється всередині завантаженого файлу.',
        categoryUnresolved: 'Не вдалося визначити вибрану категорію.',
        duplicateCodeExisting: 'Такий код запитання вже є в каталозі.',
      },
      addCategory: 'Додати категорію',
      renameCategory: 'Перейменувати категорію',
      deleteCategory: 'Видалити категорію',
      deleteCategoryTitle: 'Видалити категорію',
      deleteCategoryConfirm: 'Видалити категорію «{{name}}» з каталогу?',
      searchLabel: 'Пошук запитань',
      loading: 'Завантаження запитань…',
      error: 'Не вдалося завантажити каталог запитань.',
      empty: 'Запитань поки немає. Додайте перше.',
      loadingCategories: 'Завантаження категорій…',
      errorCategories: 'Не вдалося завантажити категорії.',
      emptyCategories: 'Категорій поки немає.',
      menuTitle: 'Інструменти каталогу',
      menuDescription: 'Шукайте запитання, додавайте нові записи та керуйте категоріями.',
      categoryCount: 'Запитань: {{count}}',
      categoryMeta: 'Категорія: {{category}}',
      rewardMeta: 'Нагорода: {{reward}}',
      answerMeta: 'Відповідь: {{answer}}',
      askedMeta: 'Ставили: {{asked}}',
      meta: '{{category}} · нагорода {{reward}} · відповідь: {{answer}}',
      disabledBadge: 'вимкнено глобально',
      noCategories: 'Спочатку створіть хоча б одну категорію, а вже потім додавайте запитання.',
      createTitle: 'Нове запитання',
      editTitle: 'Редагування запитання',
      deleteTitle: 'Видалити запитання',
      deleteConfirm: 'Видалити це запитання з каталогу?',
      categoryDialog: {
        title: 'Нова категорія',
        editTitle: 'Перейменувати категорію',
        description: 'Створіть глобальну категорію, до якої потім можна прив’язувати запитання.',
        editDescription: 'Змініть назву категорії, яка вже використовується запитаннями.',
        nameLabel: 'Назва категорії',
      },
      fields: {
        category: 'Категорія',
        text: 'Запитання',
        answer: 'Відповідь',
        reward: 'Нагорода',
        priority: 'Пріоритет',
        isEnabled: 'Доступне для вибору',
      },
    },
  },
  pl: {
    actions: {
      edit: 'Edytuj',
      view: 'Wyświetl',
      delete: 'Usuń',
    },
    common: {
      yes: 'Tak',
      no: 'Nie',
    },
    errors: {
      duplicateCode: 'Ten kod jest już używany przez inny wpis.',
      notFound: 'Nie znaleziono wpisu. Mógł zostać usunięty.',
      invalidRequest: 'Niektóre pola są nieprawidłowe. Sprawdź formularz i spróbuj ponownie.',
      contentLocked: 'Modyfikator jest zablokowany, ponieważ należy do aktywnej gry.',
      categoryNotFound: 'Nie znaleziono kategorii pytań. Mogła zostać usunięta.',
      categoryNotEmpty: 'Nie można usunąć kategorii, która nadal zawiera pytania.',
      categoryProtected: 'Systemowej kategorii domyślnej nie można zmienić ani usunąć.',
      generic: 'Nie udało się wykonać operacji. Spróbuj ponownie.',
    },
    validation: {
      required: 'Pole jest wymagane.',
      code: 'Tylko małe litery łacińskie, cyfry i podkreślenie.',
      number: 'Podaj nieujemną liczbę całkowitą.',
      limit: 'Pozostaw puste lub podaj dodatnią liczbę całkowitą.',
      formula: 'Podaj poprawne wyrażenie formuły.',
      tags: 'Użyj maksymalnie pięciu unikalnych tagów po 32 znaki.',
    },
    modifiers: {
      title: 'Katalog modyfikatorów',
      description:
        'Główna lista modyfikatorów. Twórz, edytuj i usuwaj modyfikatory dostępne dla dowolnej gry.',
      add: 'Dodaj modyfikator',
      loading: 'Ładowanie modyfikatorów…',
      error: 'Nie udało się załadować katalogu modyfikatorów.',
      empty: 'Brak modyfikatorów. Dodaj pierwszy.',
      emptyCategory: 'Brak modyfikatorów w tej kategorii.',
      menuTitle: 'Narzędzia katalogu',
      menuDescription:
        'Przeszukuj katalog, dodawaj modyfikatory i filtruj według kategorii oraz zachowania w podsumowaniu.',
      menuHint: 'Aby edytować lub usunąć wpis, użyj przycisków w wierszu modyfikatora.',
      categoryCount: 'Modyfikatorów: {{count}}',
      roundSummaryTitle: 'Zachowanie w podsumowaniu rundy',
      allRoundSummaries: 'Wszystkie warianty',
      roundSummaryCount: 'Modyfikatorów: {{count}}',
      meta: '{{category}} · koszt {{cost}}',
      hostControlBadge: 'Wymaga kontroli prowadzącego',
      contentLockedBadge: 'Zablokowany przez aktywną grę',
      contentLockedReason:
        'Modyfikator jest używany w aktywnej grze. Jego zawartość pozostaje tylko do odczytu do zakończenia lub archiwizacji gry.',
      createTitle: 'Nowy modyfikator',
      editTitle: 'Edycja modyfikatora',
      deleteTitle: 'Usuń modyfikator',
      deleteConfirm: 'Usunąć „{{name}}” z katalogu? Nie będzie już dostępny dla nowych gier.',
      fields: {
        name: 'Nazwa',
        description: 'Opis',
        category: 'Kiedy działa modyfikator',
        categoryHint: 'Wybierz etap gry, na którym działa ten modyfikator.',
        requiresHostControl: 'Wymaga kontroli prowadzącego',
        mechanicType: 'Typ mechaniki',
        activationCost: 'Koszt aktywacji',
        activationLimitCount: 'Limit aktywacji',
        limitHint: 'Pozostaw puste, aby bez limitu.',
        conflicts: 'Konflikty',
        conflictsHint: 'Modyfikatory, które nie mogą być aktywne razem z tym.',
        iconEmoji: 'Ikona (emoji)',
        activationCommand: 'Komenda aktywacji',
        durationSeconds: 'Czas trwania, sekundy',
        ruleText: 'Krótkie zasady',
        perKillBonus: 'Punkty za zabójstwo',
        failurePenaltyPoints: 'Kara za porażkę',
        killDeltaMode: 'Tryb licznika zabójstw',
        killDeltaValue: 'Bonus zabójstw',
        killCondition: 'Warunek',
        excludedWeapons: 'Wykluczona broń',
        csvHint: 'Wartości oddzielone przecinkami.',
        multiplierTarget: 'Cel mnożnika',
        multiplierDelta: 'Przyrost mnożnika',
        activeWindow: 'Okno działania',
        stopCondition: 'Warunek zatrzymania',
        mentorLoadoutText: 'Wyposażenie Mentora',
        mentorCanBeRevived: 'Mentora można podnieść',
        mentorCanBeKilled: 'Mentora można zabić',
        mentorKillsCreditToTeam: 'Zabójstwa Mentora liczą się dla drużyny',
      },
      sections: {
        basic: 'Podstawowe informacje',
        mechanics: 'Mechanika',
        availability: 'Dostępność, limity i konflikty',
      },
      mechanics: {
        rule_only: 'Zasada bez punktów',
        restriction_with_reward: 'Ograniczenie z nagrodą lub karą',
        kill_counter: 'Zmiana licznika zabójstw',
        multiplier: 'Mnożnik punktów lub zabójstw',
        mentor: 'Modyfikator z Mentorem',
      },
      roundSummaryType: {
        passive: 'Nie wpływa na podsumowanie rundy',
        automatic: 'Obliczany automatycznie',
        condition: 'Prowadzący potwierdza warunek',
        manual_count: 'Prowadzący wpisuje liczbę',
      },
      preview: {
        title: 'Podgląd formuły',
        unlimited: 'bez limitu',
        limit: 'do {{count}} na grę',
        body: '{{category}} · {{mechanic}} · {{scoringType}} · {{limit}}',
        roundSummary: 'Zachowanie w podsumowaniu rundy: {{category}}.',
        resultInput: 'Prowadzący dodatkowo wpisze: {{input}}.',
        scoreFormula: 'Formuła punktacji: {{formula}}.',
        successExpression: 'Wyrażenie przy sukcesie: {{expression}}.',
        failureExpression: 'Wyrażenie przy porażce: {{expression}}.',
      },
      wizard: {
        step: 'Krok {{current}} z {{total}}',
        steps: {
          0: 'Karta',
          1: 'Warunki i aktywacja',
          2: 'Obliczenie wyniku',
          3: 'Sprawdzenie',
        },
        stepDescriptions: {
          0: 'Opisz modyfikator dokładnie tak, jak zobaczy go gracz w katalogu.',
          1: 'Określ, kto i kiedy wykonuje regułę, koszt aktywacji, limit oraz konflikty.',
          2: 'Najpierw wybierz, co liczymy, a potem określ efekt każdej jednostki.',
          3: 'Sprawdź karty gracza i prowadzącego oraz przykład obliczenia przed zapisem.',
        },
        sections: {
          behavior: 'Jak działa reguła',
          behaviorDescription:
            'Te ustawienia określają etap gry, wykonawcę i obowiązki prowadzącego.',
          activation: 'Zakup i zgodność',
          activationDescription:
            'Te ustawienia określają cenę, limit zakupów i zgodność z innymi modyfikatorami.',
        },
        kind: 'Co robi ten modyfikator?',
        kinds: { rule: 'Reguła bez zmiany wyniku', scoring: 'Wpływa na wynik rundy' },
        tags: 'Tagi wyszukiwania',
        tagsHint: 'Wybierz podpowiedzi lub wpisz do pięciu własnych tagów.',
        suggestedTags: {
          combat: 'walka',
          mentor: 'mentor',
          movement: 'ruch',
          equipment: 'wyposażenie',
          communication: 'komunikacja',
          revival: 'wskrzeszenie',
          environment: 'otoczenie',
          restriction: 'ograniczenie',
          weapon: 'broń',
          bonus: 'premia',
          penalty: 'kara',
          timer: 'timer',
        },
        phase: 'Kiedy działa modyfikator?',
        phases: {
          preparation: 'Przed rundą',
          round: 'Podczas rundy',
          result: 'Podczas podsumowania',
        },
        phaseDescriptions: {
          preparation: 'Wybór karty, wyposażenia, umiejętności i przygotowanie drużyny.',
          round: 'Warunek lub ograniczenie działa bezpośrednio podczas rozgrywki.',
          result: 'Wynik jest zapisywany po zakończeniu gry.',
        },
        performer: 'Kto musi spełnić warunek?',
        performers: { activeTeam: 'Drużyna', mentor: 'Prowadzący' },
        performerDescriptions: {
          activeTeam: 'Działanie lub ograniczenie dotyczy aktywnej drużyny.',
          mentor: 'Działanie wykonuje prowadzący, a wynik jest zaliczany drużynie.',
        },
        rule: 'Reguła dla drużyny i prowadzącego',
        requiresHostMonitoring: 'Czy prowadzący musi ręcznie sprawdzić wykonanie?',
        monitoringAnswers: { yes: 'Tak, trzeba sprawdzić', no: 'Nie, oblicz automatycznie' },
        monitoringDescriptions: {
          yes: 'Prowadzący potwierdzi wykonanie lub wpisze wynik po grze.',
          no: 'Aplikacja automatycznie pobierze potrzebne dane rundy.',
        },
        durationQuestion: 'Czy obowiązuje limit czasu?',
        durationAnswers: { yes: 'Tak, użyj timera', no: 'Bez limitu czasu' },
        durationDescriptions: {
          yes: 'Każda aktywacja dodaje osobny przedział czasu.',
          no: 'Reguła działa bez odliczania.',
        },
        durationHint: 'Opcjonalnie. Pozostaw puste, jeśli nie ma timera.',
        commandHint: 'Pozostaw puste, aby wygenerować komendę z nazwy.',
        advancedSettings: 'Ustawienia dodatkowe',
        advancedSettingsDescription: 'Komenda aktywacji jest zwykle generowana automatycznie.',
        reward: 'Co się zmienia',
        rewards: { points: 'Punkty', bonusKills: 'Dodatkowe zabójstwa' },
        resolution: 'Jak zapisywany jest fakt',
        resolutions: {
          automaticRoundMetric: 'Automatycznie z danych rundy',
          boolean: 'Prowadzący wybiera udało się / nie udało się',
          nonNegativeCount: 'Prowadzący wpisuje nieujemną liczbę',
        },
        formula: 'Sposób obliczania',
        formulaHint: 'Pokazane są tylko zgodne wbudowane sposoby.',
        impactGuideTitle: 'Bez ręcznego pisania formuł',
        impactGuideDescription:
          'Wybierz odpowiedni scenariusz. Formularz sam ustawi wynik i dane wymagane od prowadzącego.',
        formulaConfigTitle: 'Ustawione automatycznie',
        formulaConfig: 'Wynik: {{reward}}. Sposób zapisu: {{resolution}}.',
        impactTargetQuestion: 'Co modyfikator dodaje do wyniku?',
        impactTargetHint: 'Najpierw wybierz wynik. Sposób obliczania pojawi się dalej.',
        impactTargets: {
          points: {
            title: 'Punkty',
            description: 'Zmienia wynik punktowy rundy, ale nie liczbę zabójstw.',
          },
          bonusKills: {
            title: 'Dodatkowe zabójstwa',
            description: 'Dodaje wirtualne zabójstwa przed obliczeniem wartości karty.',
          },
        },
        impactMethodQuestions: {
          points: 'Jak przyznawane są dodatkowe punkty?',
          bonusKills: 'Jak ustalana jest liczba dodatkowych zabójstw?',
        },
        formulas: {
          growing_kill_value: 'Za wszystkie zabójstwa drużyny',
          bonus_kill_on_condition: 'Raz po spełnieniu warunku',
          bonus_kills_by_count: 'Według liczby zdarzeń',
          window_kill_bonus_points: 'Tylko za pasujące zabójstwa',
        },
        formulaDescriptions: {
          growing_kill_value:
            'Aplikacja automatycznie pobierze łączną liczbę zabójstw. Pasuje do mechaniki takiej jak Pragnienie.',
          bonus_kill_on_condition:
            'Prowadzący wybierze „Spełniono / Nie spełniono”. Sukces daje stałą premię.',
          bonus_kills_by_count:
            'Prowadzący wpisze liczbę zdarzeń, a każde doda ustaloną liczbę zabójstw.',
          window_kill_bonus_points:
            'Prowadzący wpisze pasujące zabójstwa. Każde daje procent wartości karty.',
        },
        impactSettingsTitle: 'Skonfiguruj wynik',
        impactSettings: {
          growing_kill_value:
            'Zwiększaj wartość karty za każde zabójstwo i opcjonalnie nalicz karę przy zerze.',
          bonus_kill_on_condition:
            'Ustaw liczbę zabójstw przyznawaną po potwierdzeniu warunku przez prowadzącego.',
          bonus_kills_by_count:
            'Ustaw liczbę zabójstw dodawaną przez każde zdarzenie wpisane przez prowadzącego.',
          window_kill_bonus_points:
            'Ustaw procent wartości karty przyznawany za każde pasujące zabójstwo.',
        },
        units: { points: 'pkt', kills: 'zabójstw', seconds: 'sek.' },
        calculationExampleTitle: 'Jak zostanie to obliczone',
        calculationExamples: {
          growing_kill_value:
            'Karta jest warta {{cardValue}} punktów, a drużyna zdobyła {{killsCount}} zabójstwa.\nNowa wartość: {{cardValue}} + {{killsCount}} × {{increment}} = {{increasedCardValue}}.\nWynik: {{increasedCardValue}} × {{killsCount}} = {{result}} punktów. Przy zerze zabójstw kara wynosi {{penalty}} punktów.',
          bonus_kill_on_condition:
            'Karta jest warta {{cardValue}} punktów, drużyna zdobyła {{killsCount}} zabójstwa i spełniła warunek.\nZaliczone zabójstwa: {{killsCount}} + {{bonus}} = {{resultUnits}}.\nWynik: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} punktów.',
          bonus_kills_by_count:
            'Karta jest warta {{cardValue}} punktów, a drużyna zdobyła {{killsCount}} zabójstwa. Prowadzący wpisał {{inputCount}} zdarzenia.\nPremia: {{inputCount}} × {{perUnit}} = {{bonus}} zabójstwa.\nWynik: ({{killsCount}} + {{bonus}}) × {{cardValue}} = {{result}} punktów.',
          window_kill_bonus_points:
            'Karta jest warta {{cardValue}} punktów, drużyna zdobyła {{killsCount}} zabójstwa, z czego {{inputCount}} pasują do warunku.\nPremia: {{inputCount}} × {{cardValue}} × {{percent}}% = {{bonus}} punktów.\nWynik: {{killsCount}} × {{cardValue}} + {{bonus}} = {{result}} punktów.',
        },
        parameters: {
          incrementPointsPerKill: 'Wzrost wartości za zabójstwo',
          zeroKillPenaltyPoints: 'Kara za brak zabójstw',
          successBonusKills: 'Dodatkowe zabójstwa przy sukcesie',
          bonusKillsPerUnit: 'Dodatkowe zabójstwa na jednostkę',
          bonusRate: 'Procent wartości karty za zabójstwo',
        },
        help: {
          kind: 'Wybierz regułę dla ograniczeń bez zmiany wyniku. Wybierz wpływ na rundę, gdy zmieniają się punkty lub zabójstwa.',
          name: 'Widoczna dla graczy i prowadzącego w katalogu oraz historii rund.',
          description:
            'Publiczne wyjaśnienie dla gracza: co stanie się po zakupie. Umieść tu wszystkie ważne warunki.',
          iconEmoji: 'Wyświetlana obok nazwy i ułatwia szybkie rozpoznanie modyfikatora.',
          tags: 'Służą tylko do wyszukiwania i filtrowania. Nie wpływają na reguły ani punktację.',
          phase:
            'Określa, czy reguła działa podczas przygotowania, rozgrywki czy podsumowania rundy.',
          performer: 'Określa, kto wykonuje działanie: aktywna drużyna czy prowadzący.',
          rule: 'Dokładna instrukcja dla prowadzącego i drużyny używana do kontroli wykonania.',
          requiresHostMonitoring:
            'Dodaje oznaczenie ręcznej kontroli. Włącz, gdy aplikacja nie może sama sprawdzić warunku.',
          durationSeconds:
            'Czas jednej aktywacji. Każda kolejna aktywacja dodaje taki sam przedział.',
          activationCost: 'Tyle punktów quizu jest odejmowane kupującemu przy każdej aktywacji.',
          activationLimitCount:
            'Maksymalna liczba aktywacji w jednej rundzie. Puste pole oznacza brak limitu.',
          conflicts:
            'Jeśli wybrany modyfikator jest już aktywny w rundzie, tego nie można kupić — i odwrotnie.',
          activationCommand:
            'Komenda służąca do zamówienia modyfikatora. Puste pole wygeneruje ją z nazwy.',
          formulaCode:
            'Gotowy scenariusz określa wynik, dane prowadzącego i obliczenie. Wybieraj według sensu reguły.',
          incrementPointsPerKill:
            'O ile punktów jedna aktywacja zwiększa wartość zabójstwa za każde wykonane zabójstwo.',
          zeroKillPenaltyPoints:
            'Ile punktów odejmuje każda aktywacja, gdy drużyna nie wykona żadnego zabójstwa.',
          successBonusKills: 'Liczba dodatkowych zabójstw po oznaczeniu warunku jako spełnionego.',
          bonusKillsPerUnit:
            'Liczba dodatkowych zabójstw za każde uruchomienie wpisane przez prowadzącego.',
          bonusRate: 'Procent wartości karty za pasujące zabójstwo, na przykład 75%.',
          eventInputLabel: 'Etykieta pola widocznego dla prowadzącego podczas podsumowania rundy.',
        },
        measurement: {
          title: '1. Co liczymy',
          description: 'Najpierw określ źródło obliczeń. Efekt wybierzesz osobno.',
          question: 'Co uruchamia modyfikator?',
          domains: {
            kills: {
              title: 'Zabójstwa drużyny',
              description: 'Wszystkie zabójstwa lub tylko spełniające warunek.',
            },
            event: {
              title: 'Inne zdarzenie lub warunek',
              description: 'Dowolne mierzalne działanie, udany strzał, cel lub aktywacja.',
            },
          },
          killQuestion: 'Które zabójstwa liczyć?',
          killModes: {
            all: {
              title: 'Wszystkie zabójstwa',
              description: 'Aplikacja automatycznie pobiera końcowy licznik rundy.',
            },
            qualifying: {
              title: 'Tylko pasujące zabójstwa',
              description: 'Prowadzący wpisuje liczbę zabójstw spełniających warunek.',
            },
          },
          eventQuestion: 'Jak zapisywane jest zdarzenie?',
          eventModes: {
            condition: {
              title: 'Warunek: tak lub nie',
              description: 'Jedno potwierdzenie przy podsumowaniu rundy.',
            },
            count: {
              title: 'Wpisz liczbę zdarzeń',
              description: 'Prowadzący wpisuje łączną liczbę udanych zdarzeń.',
            },
            perActivation: {
              title: 'Raz na aktywację',
              description: 'Każdy zakup automatycznie liczy się jako jedno zdarzenie.',
            },
          },
          inputLabel: 'Co ma wpisać prowadzący?',
          inputLabelHint: 'Podaj konkretną nazwę pola.',
          maximumQuestion: 'Ograniczyć liczbę aktywacjami?',
          maximumKinds: {
            none: {
              title: 'Bez limitu',
              description: 'Zdarzenie może wystąpić dowolną liczbę razy.',
            },
            activations: {
              title: 'Zależne od aktywacji',
              description: 'Suma nie przekroczy aktywacje × zdarzenia na aktywację.',
            },
          },
          eventsPerActivation: 'Maksimum zdarzeń na aktywację',
          eventsPerActivationHint:
            'Dla Lucky Shot to 1: sześć aktywacji pozwala na maksymalnie sześć udanych strzałów.',
        },
        payout: {
          title: '2. Co daje każda jednostka',
          description: 'Efekt można powiązać z zabójstwami, zdarzeniem, warunkiem lub aktywacją.',
          question: 'Wybierz efekt',
          kinds: {
            fixedPoints: {
              title: 'Stałe punkty',
              description: 'Dodaje określoną liczbę punktów za każdą jednostkę.',
            },
            cardPercent: {
              title: 'Procent wartości karty',
              description: 'Dodaje lub odejmuje procent wartości karty za każdą jednostkę.',
            },
            bonusKills: {
              title: 'Bonusowe zabójstwa',
              description: 'Zwiększa licznik zabójstw używany do obliczania karty.',
            },
            killValueIncrease: {
              title: 'Wzrost wartości zabójstwa',
              description: 'Każda jednostka podnosi wartość wszystkich zabójstw drużyny.',
            },
          },
          values: {
            fixedPoints: 'Punkty na jednostkę',
            cardPercent: 'Procent karty na jednostkę',
            bonusKills: 'Bonusowe zabójstwa na jednostkę',
            killValueIncrease: 'Wzrost wartości na jednostkę',
          },
          valueHints: {
            fixedPoints: 'Wartość ujemna może być karą.',
            cardPercent: 'Na przykład 75 dodaje 75%, a −25 odejmuje 25% wartości karty.',
            bonusKills: 'Dodatnia liczba całkowita.',
            killValueIncrease: 'Dodatnia liczba całkowita punktów.',
          },
          zeroCountPenalty: 'Kara, gdy licznik źródła wynosi zero',
          zeroCountPenaltyHint: 'Ustaw 0, jeśli osobna kara nie jest potrzebna.',
          summary: 'Źródło: {{source}}. Efekt: {{effect}} ({{value}} na jednostkę).',
        },
        previewLoading: 'Tworzenie podglądu modyfikatora',
        previewError: 'Nie udało się utworzyć wiarygodnego podglądu.',
        playerView: 'Widok gracza',
        hostView: 'Widok prowadzącego',
        commandPreview: 'Komenda aktywacji: {{command}}',
        exampleTitle: 'Przykład kontrolny',
        exampleResolution: {
          completed: 'zasada wykonana',
          automatic: 'obliczane automatycznie',
          succeeded: 'warunek spełniony',
          perActivation: 'jedna aktywacja',
        },
        exampleFacts:
          'Karta {{cardValue}}, zabójstwa {{killsCount}}, nagrody {{bountyCount}}, dane: {{resolutionExample}}.',
        exampleResult:
          'Punkty modyfikatora {{pointsDelta}}, dodatkowe zabójstwa {{bonusKillsDelta}}, wynik {{finalScore}}.',
        discardTitle: 'Odrzucić szkic modyfikatora?',
        discardDescription: 'Niezapisane zmiany w kreatorze zostaną utracone.',
        discardConfirm: 'Odrzuć',
      },
    },
    questions: {
      title: 'Katalog pytań',
      description:
        'Główna lista pytań quizu. Twórz, edytuj i usuwaj pytania dostępne dla dowolnej gry.',
      add: 'Dodaj pytanie',
      importJson: 'Prześlij JSON',
      downloadTemplate: 'Pobierz szablon',
      importGroupTitle: 'Import z JSON',
      importGroupDescription:
        'Import zbiorczy: pobierz szablon, uzupełnij pytania i prześlij plik JSON.',
      importGroupExpand: 'Rozwiń import z JSON',
      importGroupCollapse: 'Zwiń import z JSON',
      categoryGroupDescription:
        'Tu dodasz kategorię. Aby zmienić nazwę lub usunąć — najpierw wybierz ją na liście poniżej. Kategorii systemowej nie można edytować.',
      categoryGroupExpand: 'Rozwiń zarządzanie kategoriami',
      categoryGroupCollapse: 'Zwiń zarządzanie kategoriami',
      importSuccess: 'Zaimportowano pytań: {{count}}.',
      importPartial: 'Zaimportowano pytań: {{count}}. Pominięto: {{skipped}}.',
      importSkippedTitle: 'Pominięte pytania',
      importSkippedDescription:
        'Niektórych pytań nie udało się zaimportować. Pobierz raport, aby zobaczyć, które wiersze nie przeszły i dlaczego.',
      importErrorDescription:
        'Nie udało się zakończyć importu. Pobierz raport, aby zachować nazwę pliku i szczegóły błędu razem.',
      downloadImportReport: 'Pobierz raport',
      importReasons: {
        invalidFields:
          'Brakuje wymaganych pól lub są one nieprawidłowe. Każde pytanie musi zawierać treść, odpowiedź i nieujemną nagrodę.',
        duplicateCodeInFile: 'Kod pytania powtarza się w przesłanym pliku.',
        categoryUnresolved: 'Nie udało się ustalić wybranej kategorii.',
        duplicateCodeExisting: 'Taki kod pytania już istnieje w katalogu.',
      },
      addCategory: 'Dodaj kategorię',
      renameCategory: 'Zmień nazwę kategorii',
      deleteCategory: 'Usuń kategorię',
      deleteCategoryTitle: 'Usuń kategorię',
      deleteCategoryConfirm: 'Usunąć kategorię „{{name}}” z katalogu?',
      searchLabel: 'Szukaj pytań',
      loading: 'Ładowanie pytań…',
      error: 'Nie udało się załadować katalogu pytań.',
      empty: 'Brak pytań. Dodaj pierwsze.',
      loadingCategories: 'Ładowanie kategorii…',
      errorCategories: 'Nie udało się załadować kategorii.',
      emptyCategories: 'Brak kategorii.',
      menuTitle: 'Narzędzia katalogu',
      menuDescription: 'Szukaj pytań, dodawaj nowe wpisy i zarządzaj kategoriami.',
      categoryCount: 'Pytań: {{count}}',
      categoryMeta: 'Kategoria: {{category}}',
      rewardMeta: 'Nagroda: {{reward}}',
      answerMeta: 'Odpowiedź: {{answer}}',
      askedMeta: 'Zadano: {{asked}}',
      meta: '{{category}} · nagroda {{reward}} · odpowiedź: {{answer}}',
      disabledBadge: 'wyłączone globalnie',
      noCategories: 'Najpierw utwórz co najmniej jedną kategorię, a dopiero potem dodawaj pytania.',
      createTitle: 'Nowe pytanie',
      editTitle: 'Edycja pytania',
      deleteTitle: 'Usuń pytanie',
      deleteConfirm: 'Usunąć to pytanie z katalogu?',
      categoryDialog: {
        title: 'Nowa kategoria',
        editTitle: 'Zmień nazwę kategorii',
        description: 'Utwórz globalną kategorię, do której później można przypisywać pytania.',
        editDescription: 'Zmień nazwę kategorii używanej już przez istniejące pytania.',
        nameLabel: 'Nazwa kategorii',
      },
      fields: {
        category: 'Kategoria',
        text: 'Pytanie',
        answer: 'Odpowiedź',
        reward: 'Nagroda',
        priority: 'Priorytet',
        isEnabled: 'Dostępne do wyboru',
      },
    },
  },
}

export default translations
