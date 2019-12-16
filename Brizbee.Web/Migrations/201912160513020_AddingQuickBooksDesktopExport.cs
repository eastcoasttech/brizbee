namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingQuickBooksDesktopExport : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.QuickBooksDesktopExports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        InAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        OutAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        QBProductName = c.String(maxLength: 40),
                        QBMajorVersion = c.String(maxLength: 10),
                        QBMinorVersion = c.String(maxLength: 10),
                        QBCountry = c.String(maxLength: 10),
                        QBSupportedQBXMLVersions = c.String(maxLength: 100),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuickBooksDesktopExports", "UserId", "dbo.Users");
            DropIndex("dbo.QuickBooksDesktopExports", new[] { "UserId" });
            DropTable("dbo.QuickBooksDesktopExports");
        }
    }
}
