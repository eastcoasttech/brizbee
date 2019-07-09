namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CommitIdNotRequired : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Punches", "CommitId", "dbo.Commits");
            DropIndex("dbo.Punches", new[] { "CommitId" });
            AlterColumn("dbo.Punches", "CommitId", c => c.Int());
            CreateIndex("dbo.Punches", "CommitId");
            AddForeignKey("dbo.Punches", "CommitId", "dbo.Commits", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Punches", "CommitId", "dbo.Commits");
            DropIndex("dbo.Punches", new[] { "CommitId" });
            AlterColumn("dbo.Punches", "CommitId", c => c.Int(nullable: false));
            CreateIndex("dbo.Punches", "CommitId");
            AddForeignKey("dbo.Punches", "CommitId", "dbo.Commits", "Id", cascadeDelete: true);
        }
    }
}
