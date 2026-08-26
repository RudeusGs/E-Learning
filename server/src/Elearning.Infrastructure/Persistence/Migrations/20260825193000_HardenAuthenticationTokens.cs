using System;
using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Elearning.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ElearningDbContext))]
[Migration("20260825193000_HardenAuthenticationTokens")]
public partial class HardenAuthenticationTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Refresh tokens are credentials, not business data. Invalidating all existing
        // sessions is safer than attempting to migrate plaintext legacy token values.
        migrationBuilder.DropTable(name: "BlacklistedTokens");
        migrationBuilder.DropTable(name: "RefreshTokens");

        migrationBuilder.CreateTable(
            name: "BlacklistedTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TokenId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_BlacklistedTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                SecurityStampHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AccessTokenJti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AccessTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                RevocationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                UserId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BlacklistedTokens_ExpiresAtUtc",
            table: "BlacklistedTokens",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_BlacklistedTokens_TokenId",
            table: "BlacklistedTokens",
            column: "TokenId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BlacklistedTokens_UserId",
            table: "BlacklistedTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_AccessTokenJti",
            table: "RefreshTokens",
            column: "AccessTokenJti",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ExpiresAtUtc",
            table: "RefreshTokens",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

#pragma warning disable CA1861
        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId_FamilyId",
            table: "RefreshTokens",
            columns: new[] { "UserId", "FamilyId" });
#pragma warning restore CA1861
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BlacklistedTokens");
        migrationBuilder.DropTable(name: "RefreshTokens");

        migrationBuilder.CreateTable(
            name: "BlacklistedTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TokenId = table.Column<string>(type: "text", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "text", nullable: false),
                JwtId = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");
    }
}
