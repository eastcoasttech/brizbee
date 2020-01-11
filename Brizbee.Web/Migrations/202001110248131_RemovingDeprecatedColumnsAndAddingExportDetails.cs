namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingDeprecatedColumnsAndAddingExportDetails : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.QuickBooksOnlineExports", name: "UserId", newName: "CreatedByUserId");
            RenameIndex(table: "dbo.QuickBooksOnlineExports", name: "IX_UserId", newName: "IX_CreatedByUserId");
            AddColumn("dbo.QuickBooksOnlineExports", "ReversedAt", c => c.DateTime(precision: 7, storeType: "datetime2"));
            AddColumn("dbo.QuickBooksOnlineExports", "ReversedByUserId", c => c.Int());
            CreateIndex("dbo.QuickBooksOnlineExports", "ReversedByUserId");
            AddForeignKey("dbo.QuickBooksOnlineExports", "ReversedByUserId", "dbo.Users", "Id");
            DropColumn("dbo.QuickBooksOnlineExports", "AccessToken");
            DropColumn("dbo.QuickBooksOnlineExports", "AccessTokenExpiresAt");
            DropColumn("dbo.QuickBooksOnlineExports", "RefreshToken");
            DropColumn("dbo.QuickBooksOnlineExports", "RefreshTokenExpiresAt");
        }
        
        public override void Down()
        {
            AddColumn("dbo.QuickBooksOnlineExports", "RefreshTokenExpiresAt", c => c.String());
            AddColumn("dbo.QuickBooksOnlineExports", "RefreshToken", c => c.String());
            AddColumn("dbo.QuickBooksOnlineExports", "AccessTokenExpiresAt", c => c.String());
            AddColumn("dbo.QuickBooksOnlineExports", "AccessToken", c => c.String());
            DropForeignKey("dbo.QuickBooksOnlineExports", "ReversedByUserId", "dbo.Users");
            DropIndex("dbo.QuickBooksOnlineExports", new[] { "ReversedByUserId" });
            DropColumn("dbo.QuickBooksOnlineExports", "ReversedByUserId");
            DropColumn("dbo.QuickBooksOnlineExports", "ReversedAt");
            RenameIndex(table: "dbo.QuickBooksOnlineExports", name: "IX_CreatedByUserId", newName: "IX_UserId");
            RenameColumn(table: "dbo.QuickBooksOnlineExports", name: "CreatedByUserId", newName: "UserId");
        }
    }
}
