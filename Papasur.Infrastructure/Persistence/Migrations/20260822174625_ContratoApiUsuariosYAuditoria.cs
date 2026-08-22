using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papasur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContratoApiUsuariosYAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "employee_number",
                table: "user",
                newName: "employee_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_employee_number",
                table: "user",
                newName: "ix_user_employee_id");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "user",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "user",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Migración de datos ANTES de borrar las columnas viejas:
            // "Ana Pérez García" -> first_name "Ana", last_name "Pérez García"; is_active -> status.
            migrationBuilder.Sql(@"
                UPDATE ""user"" SET
                    first_name = COALESCE(NULLIF(split_part(name, ' ', 1), ''), name, ''),
                    last_name  = COALESCE(NULLIF(substring(name from position(' ' in name) + 1), ''), ''),
                    status     = CASE WHEN is_active THEN 'active' ELSE 'inactive' END;");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user");

            migrationBuilder.DropColumn(
                name: "name",
                table: "user");

            migrationBuilder.AddColumn<string>(
                name: "actor_name",
                table: "audit_entry",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "actor_role",
                table: "audit_entry",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "changes",
                table: "audit_entry",
                type: "jsonb",
                nullable: true);

            // La auditoría vieja no tenía actor desnormalizado: se completa desde el usuario
            // referenciado (es lo mejor disponible; de acá en adelante se copia al escribir).
            migrationBuilder.Sql(@"
                UPDATE audit_entry a SET
                    actor_name = COALESCE(TRIM(u.first_name || ' ' || u.last_name), ''),
                    actor_role = COALESCE(r.name, '')
                FROM ""user"" u
                LEFT JOIN role r ON r.id = u.role_id
                WHERE a.user_id = u.id;");

            migrationBuilder.CreateTable(
                name: "password_reset_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_token_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "role",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "agent");

            migrationBuilder.CreateIndex(
                name: "ix_user_status",
                table: "user",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entry_actor_role",
                table: "audit_entry",
                column: "actor_role");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_token_token_hash",
                table: "password_reset_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_token_user_id",
                table: "password_reset_token",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_token");

            migrationBuilder.DropIndex(
                name: "ix_user_status",
                table: "user");

            migrationBuilder.DropIndex(
                name: "ix_audit_entry_actor_role",
                table: "audit_entry");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "user");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "user");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "user");

            migrationBuilder.DropColumn(
                name: "status",
                table: "user");

            migrationBuilder.DropColumn(
                name: "actor_name",
                table: "audit_entry");

            migrationBuilder.DropColumn(
                name: "actor_role",
                table: "audit_entry");

            migrationBuilder.DropColumn(
                name: "changes",
                table: "audit_entry");

            migrationBuilder.RenameColumn(
                name: "employee_id",
                table: "user",
                newName: "employee_number");

            migrationBuilder.RenameIndex(
                name: "ix_user_employee_id",
                table: "user",
                newName: "ix_user_employee_number");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "user",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "role",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "agente");
        }
    }
}
