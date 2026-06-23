using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameQuestionSortOrderToPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_question_definitions_IsDeleted_IsEnabled_AskedTotalCount_La~",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_SortOrder",
                table: "question_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_question_definitions_sort_order_non_negative",
                table: "question_definitions");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "question_definitions",
                newName: "Priority");

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "question_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_IsDeleted_IsEnabled_AskedTotalCount_Pr~",
                table: "question_definitions",
                columns: new[] { "IsDeleted", "IsEnabled", "AskedTotalCount", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_Priority",
                table: "question_definitions",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_question_definitions_IsDeleted_IsEnabled_AskedTotalCount_Pr~",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_Priority",
                table: "question_definitions");

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "question_definitions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "question_definitions",
                newName: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_IsDeleted_IsEnabled_AskedTotalCount_La~",
                table: "question_definitions",
                columns: new[] { "IsDeleted", "IsEnabled", "AskedTotalCount", "LastAskedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_SortOrder",
                table: "question_definitions",
                column: "SortOrder");

            migrationBuilder.AddCheckConstraint(
                name: "CK_question_definitions_sort_order_non_negative",
                table: "question_definitions",
                sql: "\"SortOrder\" >= 0");
        }
    }
}
