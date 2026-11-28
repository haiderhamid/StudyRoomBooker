using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueRoomNumberPerBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_BuildingId",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingId_RoomNumber",
                table: "Rooms",
                columns: new[] { "BuildingId", "RoomNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_BuildingId_RoomNumber",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingId",
                table: "Rooms",
                column: "BuildingId");
        }
    }
}
