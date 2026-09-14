using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItAssetManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssetConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. This migration exists only to record that Assets.UpdatedAt
            // is now an optimistic concurrency token, which is model metadata rather than
            // schema: EF starts putting the column in each UPDATE's WHERE clause, and the
            // table itself is unchanged. It is kept rather than deleted so the model snapshot
            // stays in step with the migration history.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
