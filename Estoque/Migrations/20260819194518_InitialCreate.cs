using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estoque.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PRODUTO",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    CODIGO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    SALDO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    RESERVADO = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUTO", x => x.ID);
                    table.CheckConstraint("CK_PRODUTO_RESERVADO", "\"RESERVADO\" >= 0");
                    table.CheckConstraint("CK_PRODUTO_SALDO", "\"SALDO\" >= 0");
                    table.CheckConstraint("CK_PRODUTO_SALDO_RESERVADO", "\"SALDO\" >= \"RESERVADO\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PRODUTO_CODIGO",
                table: "PRODUTO",
                column: "CODIGO",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PRODUTO");
        }
    }
}
