using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faturamento.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NOTA_FISCAL",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NUMERO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "ABERTA"),
                    DATA_CRIACAO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTA_FISCAL", x => x.ID);
                    table.CheckConstraint("CK_NOTA_FISCAL_STATUS", "\"STATUS\" IN (''ABERTA'', ''FECHADA'')");
                });

            migrationBuilder.CreateTable(
                name: "ITEM_NOTA_FISCAL",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOTA_FISCAL_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PRODUTO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    QUANTIDADE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEM_NOTA_FISCAL", x => x.ID);
                    table.CheckConstraint("CK_ITEM_NOTA_FISCAL_QUANTIDADE", "\"QUANTIDADE\" > 0");
                    table.ForeignKey(
                        name: "FK_ITEM_NOTA_FISCAL_NOTA_FISCAL_NOTA_FISCAL_ID",
                        column: x => x.NOTA_FISCAL_ID,
                        principalTable: "NOTA_FISCAL",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_NOTA_FISCAL_NOTA_FISCAL_ID",
                table: "ITEM_NOTA_FISCAL",
                column: "NOTA_FISCAL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_NOTA_FISCAL_NUMERO",
                table: "NOTA_FISCAL",
                column: "NUMERO",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ITEM_NOTA_FISCAL");

            migrationBuilder.DropTable(
                name: "NOTA_FISCAL");
        }
    }
}
