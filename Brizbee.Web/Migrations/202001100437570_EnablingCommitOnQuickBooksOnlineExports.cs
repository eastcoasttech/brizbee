namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EnablingCommitOnQuickBooksOnlineExports : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.QuickBooksOnlineExports", "CommitId");
            AddForeignKey("dbo.QuickBooksOnlineExports", "CommitId", "dbo.Commits", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuickBooksOnlineExports", "CommitId", "dbo.Commits");
            DropIndex("dbo.QuickBooksOnlineExports", new[] { "CommitId" });
        }
    }
}
