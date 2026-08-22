using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papasur.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrazabilidadYDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    establecimiento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    pivote = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cuadrante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plantilla_documento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    organismo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    pais_destino = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantilla_documento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transportista",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transportista", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "variedad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variedad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campo_plantilla",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plantilla_documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    etiqueta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_dato = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    obligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    regla_mapeo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campo_plantilla", x => x.id);
                    table.ForeignKey(
                        name: "fk_campo_plantilla_plantilla_documento_plantilla_documento_id",
                        column: x => x.plantilla_documento_id,
                        principalTable: "plantilla_documento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lote",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    variedad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    superficie_ha = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lote", x => x.id);
                    table.ForeignKey(
                        name: "fk_lote_campo_campo_id",
                        column: x => x.campo_id,
                        principalTable: "campo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lote_variedad_variedad_id",
                        column: x => x.variedad_id,
                        principalTable: "variedad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    numero_remito = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    kilogramos = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    bolsas = table.Column<int>(type: "integer", nullable: true),
                    kg_promedio = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    presentacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calibre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    transportista_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comisionista = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    destino = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    dtv = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimiento", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimiento_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimiento_lote_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimiento_transportista_transportista_id",
                        column: x => x.transportista_id,
                        principalTable: "transportista",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documento_exportacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantilla_documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_plantilla = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documento_exportacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_documento_exportacion_lote_lote_id",
                        column: x => x.lote_id,
                        principalTable: "lote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_exportacion_movimiento_movimiento_id",
                        column: x => x.movimiento_id,
                        principalTable: "movimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_exportacion_plantilla_documento_plantilla_documen",
                        column: x => x.plantilla_documento_id,
                        principalTable: "plantilla_documento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_exportacion_status_status_id",
                        column: x => x.status_id,
                        principalTable: "status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_documento_exportacion_user_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valor_campo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_exportacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campo_plantilla_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    inferido_desde = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valor_campo", x => x.id);
                    table.ForeignKey(
                        name: "fk_valor_campo_campo_plantilla_campo_plantilla_id",
                        column: x => x.campo_plantilla_id,
                        principalTable: "campo_plantilla",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_valor_campo_documento_exportacion_documento_exportacion_id",
                        column: x => x.documento_exportacion_id,
                        principalTable: "documento_exportacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campo_nombre",
                table: "campo",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "ix_campo_plantilla_plantilla_documento_id_clave",
                table: "campo_plantilla",
                columns: new[] { "plantilla_documento_id", "clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cliente_nombre",
                table: "cliente",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_created_by_user_id",
                table: "documento_exportacion",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_lote_id",
                table: "documento_exportacion",
                column: "lote_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_movimiento_id",
                table: "documento_exportacion",
                column: "movimiento_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_plantilla_documento_id",
                table: "documento_exportacion",
                column: "plantilla_documento_id");

            migrationBuilder.CreateIndex(
                name: "ix_documento_exportacion_status_id",
                table: "documento_exportacion",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_lote_campo_id",
                table: "lote",
                column: "campo_id");

            migrationBuilder.CreateIndex(
                name: "ix_lote_codigo",
                table: "lote",
                column: "codigo");

            migrationBuilder.CreateIndex(
                name: "ix_lote_variedad_id",
                table: "lote",
                column: "variedad_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_cliente_id",
                table: "movimiento",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_dtv",
                table: "movimiento",
                column: "dtv");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_fecha",
                table: "movimiento",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_lote_id",
                table: "movimiento",
                column: "lote_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_numero_remito",
                table: "movimiento",
                column: "numero_remito");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_tipo",
                table: "movimiento",
                column: "tipo");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_transportista_id",
                table: "movimiento",
                column: "transportista_id");

            migrationBuilder.CreateIndex(
                name: "ix_plantilla_documento_nombre_version",
                table: "plantilla_documento",
                columns: new[] { "nombre", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plantilla_documento_tipo",
                table: "plantilla_documento",
                column: "tipo");

            migrationBuilder.CreateIndex(
                name: "ix_transportista_nombre",
                table: "transportista",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valor_campo_campo_plantilla_id",
                table: "valor_campo",
                column: "campo_plantilla_id");

            migrationBuilder.CreateIndex(
                name: "ix_valor_campo_documento_exportacion_id_campo_plantilla_id",
                table: "valor_campo",
                columns: new[] { "documento_exportacion_id", "campo_plantilla_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variedad_nombre",
                table: "variedad",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "valor_campo");

            migrationBuilder.DropTable(
                name: "campo_plantilla");

            migrationBuilder.DropTable(
                name: "documento_exportacion");

            migrationBuilder.DropTable(
                name: "movimiento");

            migrationBuilder.DropTable(
                name: "plantilla_documento");

            migrationBuilder.DropTable(
                name: "cliente");

            migrationBuilder.DropTable(
                name: "lote");

            migrationBuilder.DropTable(
                name: "transportista");

            migrationBuilder.DropTable(
                name: "campo");

            migrationBuilder.DropTable(
                name: "variedad");
        }
    }
}
