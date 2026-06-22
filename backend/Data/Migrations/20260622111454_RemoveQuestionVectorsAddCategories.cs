using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuestionVectorsAddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_question_definitions_question_vectors_VectorCode",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_VectorCode_Category_IsEnabled",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_VectorCode_ExternalCode",
                table: "question_definitions");

            migrationBuilder.DropColumn(
                name: "VectorCode",
                table: "question_definitions");

            migrationBuilder.CreateTable(
                name: "question_categories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_categories", x => x.Name);
                    table.CheckConstraint("CK_question_categories_name_not_blank", "length(trim(\"Name\")) > 0");
                });

            migrationBuilder.Sql("""
                INSERT INTO question_categories ("Name", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT DISTINCT qd."Category", CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM question_definitions AS qd
                WHERE length(trim(qd."Category")) > 0
                ON CONFLICT ("Name") DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_Category_IsEnabled",
                table: "question_definitions",
                columns: new[] { "Category", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_ExternalCode",
                table: "question_definitions",
                column: "ExternalCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_question_definitions_question_categories_Category",
                table: "question_definitions",
                column: "Category",
                principalTable: "question_categories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "question_vectors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_question_definitions_question_categories_Category",
                table: "question_definitions");

            migrationBuilder.DropTable(
                name: "question_categories");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_Category_IsEnabled",
                table: "question_definitions");

            migrationBuilder.DropIndex(
                name: "IX_question_definitions_ExternalCode",
                table: "question_definitions");

            migrationBuilder.AddColumn<string>(
                name: "VectorCode",
                table: "question_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "question_vectors",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_vectors", x => x.Code);
                    table.CheckConstraint("CK_question_vectors_code_not_blank", "length(trim(\"Code\")) > 0");
                });

            migrationBuilder.Sql("""
                INSERT INTO question_vectors ("Code", "Name", "CreatedAtUtc", "UpdatedAtUtc", "IsEnabled")
                SELECT DISTINCT qd."Category", qd."Category", CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, TRUE
                FROM question_definitions AS qd
                WHERE length(trim(qd."Category")) > 0
                ON CONFLICT ("Code") DO NOTHING;
                """);

            migrationBuilder.Sql("""
                UPDATE question_definitions
                SET "VectorCode" = "Category"
                WHERE length(trim("Category")) > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_VectorCode_Category_IsEnabled",
                table: "question_definitions",
                columns: new[] { "VectorCode", "Category", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_question_definitions_VectorCode_ExternalCode",
                table: "question_definitions",
                columns: new[] { "VectorCode", "ExternalCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_question_definitions_question_vectors_VectorCode",
                table: "question_definitions",
                column: "VectorCode",
                principalTable: "question_vectors",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
