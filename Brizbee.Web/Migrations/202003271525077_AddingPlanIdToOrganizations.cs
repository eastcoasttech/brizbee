namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingPlanIdToOrganizations : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "PlanId", c => c.Int(nullable: false));
            CreateIndex("dbo.QuickBooksDesktopExports", "CommitId");
            AddForeignKey("dbo.QuickBooksDesktopExports", "CommitId", "dbo.Commits", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuickBooksDesktopExports", "CommitId", "dbo.Commits");
            DropIndex("dbo.QuickBooksDesktopExports", new[] { "CommitId" });
            DropColumn("dbo.Organizations", "PlanId");
        }
    }
}
