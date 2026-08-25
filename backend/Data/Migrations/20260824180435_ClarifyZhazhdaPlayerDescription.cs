using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations;

public partial class ClarifyZhazhdaPlayerDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE modifier_definitions
            SET description = $new_description$«Жажда» увеличивает стоимость одного убийства на 5 очков за каждое убийство в раунде. Каждая активация добавляет такой бонус отдельно. Пример: карточка 100, одна активация и три убийства — бонус 15 к стоимости, поэтому итог равен 115 × 3 = 345. Если убийств нет, каждая активация даёт штраф 25 очков; штраф пустой карточки применяется отдельно.$new_description$
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND description = $old_description$Убийства дают нарастающий бонус +5, миссия без убийств даёт штраф 25.$old_description$;

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                behavior_v2_json,
                '{rule}',
                to_jsonb($new_rule$В конце раунда за каждую активацию к стоимости одного убийства добавляется 5 × количество убийств. Новая стоимость умножается на количество убийств. Если убийств нет, каждая активация даёт штраф 25 очков.$new_rule$::text),
                FALSE
            )
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND behavior_v2_json ->> 'rule' = $old_rule$Каждая активация даёт нарастающие очки за убийства и штраф при нуле убийств.$old_rule$;
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE modifier_definitions
            SET description = $old_description$Убийства дают нарастающий бонус +5, миссия без убийств даёт штраф 25.$old_description$
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND description = $new_description$«Жажда» увеличивает стоимость одного убийства на 5 очков за каждое убийство в раунде. Каждая активация добавляет такой бонус отдельно. Пример: карточка 100, одна активация и три убийства — бонус 15 к стоимости, поэтому итог равен 115 × 3 = 345. Если убийств нет, каждая активация даёт штраф 25 очков; штраф пустой карточки применяется отдельно.$new_description$;

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                behavior_v2_json,
                '{rule}',
                to_jsonb($old_rule$Каждая активация даёт нарастающие очки за убийства и штраф при нуле убийств.$old_rule$::text),
                FALSE
            )
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND behavior_v2_json ->> 'rule' = $new_rule$В конце раунда за каждую активацию к стоимости одного убийства добавляется 5 × количество убийств. Новая стоимость умножается на количество убийств. Если убийств нет, каждая активация даёт штраф 25 очков.$new_rule$;
            """
        );
    }
}
