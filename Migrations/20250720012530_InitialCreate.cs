using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace net_news_html.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PassKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedNews",
                columns: table => new
                {
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    SaveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PassKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedNews", x => x.Url);
                    table.ForeignKey(
                        name: "FK_SavedNews_PassKeys_PassKeyId",
                        column: x => x.PassKeyId,
                        principalTable: "PassKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedNews_PassKeyId",
                table: "SavedNews",
                column: "PassKeyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedNews");

            migrationBuilder.DropTable(
                name: "PassKeys");
        }
    }
}
