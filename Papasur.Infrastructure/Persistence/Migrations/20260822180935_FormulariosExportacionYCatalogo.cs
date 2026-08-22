using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papasur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormulariosExportacionYCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ambito",
                table: "plantilla_documento",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                table: "plantilla_documento",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "campania",
                table: "lote",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "en_cuarentena",
                table: "lote",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "humedad",
                table: "lote",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "poder_germinativo",
                table: "lote",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "posicion",
                table: "lote",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pureza",
                table: "lote",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registro_inase",
                table: "lote",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "storage_location_id",
                table: "lote",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tratamiento",
                table: "lote",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "lote_id",
                table: "documento_exportacion",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "export_form_id",
                table: "documento_exportacion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "cliente",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "cliente",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "cliente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                table: "cliente",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "cliente",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_incoterm",
                table: "cliente",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_port_of_discharge",
                table: "cliente",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_id",
                table: "cliente",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ayuda",
                table: "campo_plantilla",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origen",
                table: "campo_plantilla",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "export_form",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    port_of_loading = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    port_of_discharge = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    incoterm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_terms = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requirement_values = table.Column<string>(type: "jsonb", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_form", x => x.id);
                    table.ForeignKey(
                        name: "fk_export_form_cliente_customer_id",
                        column: x => x.customer_id,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_form_user_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_export_form_user_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storage_location",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    temperature_c = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_location", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_form_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_kg = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    packaging_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    packages_count = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    traceability_lot_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    traceability_species = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    traceability_variety = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    traceability_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    traceability_crop_year = table.Column<int>(type: "integer", nullable: false),
                    traceability_location_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    traceability_germination_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    traceability_purity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    traceability_inase_registration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    traceability_captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_form_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_export_form_item_export_form_export_form_id",
                        column: x => x.export_form_id,
                        principalTable: "export_form",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_export_form_item_lote_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plantilla_documento_ambito_codigo",
                table: "plantilla_documento",
                columns: new[] { "ambito", "codigo" });

            migrationBuilder.CreateIndex(
                name: "ix_lote_storage_location_id",
                table: "lote",
                column: "storage_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_export_form_id",
                table: "documento_exportacion",
                column: "export_form_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_code",
                table: "export_form",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_export_form_created_at",
                table: "export_form",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_created_by_user_id",
                table: "export_form",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_customer_id",
                table: "export_form",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_reviewed_by_user_id",
                table: "export_form",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_status",
                table: "export_form",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_item_export_form_id",
                table: "export_form_item",
                column: "export_form_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_form_item_lot_id",
                table: "export_form_item",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_location_code",
                table: "storage_location",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_documento_exportacion_export_forms_export_form_id",
                table: "documento_exportacion",
                column: "export_form_id",
                principalTable: "export_form",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lote_storage_locations_storage_location_id",
                table: "lote",
                column: "storage_location_id",
                principalTable: "storage_location",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documento_exportacion_export_forms_export_form_id",
                table: "documento_exportacion");

            migrationBuilder.DropForeignKey(
                name: "fk_lote_storage_locations_storage_location_id",
                table: "lote");

            migrationBuilder.DropTable(
                name: "export_form_item");

            migrationBuilder.DropTable(
                name: "storage_location");

            migrationBuilder.DropTable(
                name: "export_form");

            migrationBuilder.DropIndex(
                name: "ix_plantilla_documento_ambito_codigo",
                table: "plantilla_documento");

            migrationBuilder.DropIndex(
                name: "ix_lote_storage_location_id",
                table: "lote");

            migrationBuilder.DropIndex(
                name: "ix_documento_exportacion_export_form_id",
                table: "documento_exportacion");

            migrationBuilder.DropColumn(
                name: "ambito",
                table: "plantilla_documento");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "plantilla_documento");

            migrationBuilder.DropColumn(
                name: "campania",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "en_cuarentena",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "humedad",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "poder_germinativo",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "posicion",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "pureza",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "registro_inase",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "storage_location_id",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "tratamiento",
                table: "lote");

            migrationBuilder.DropColumn(
                name: "export_form_id",
                table: "documento_exportacion");

            migrationBuilder.DropColumn(
                name: "address",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "city",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "contact_name",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "default_incoterm",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "default_port_of_discharge",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "tax_id",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "ayuda",
                table: "campo_plantilla");

            migrationBuilder.DropColumn(
                name: "origen",
                table: "campo_plantilla");

            migrationBuilder.AlterColumn<Guid>(
                name: "lote_id",
                table: "documento_exportacion",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
