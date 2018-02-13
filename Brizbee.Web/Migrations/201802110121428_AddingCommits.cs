namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingCommits : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Commits",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false),
                        InAt = c.DateTime(nullable: false),
                        OrganizationId = c.Int(nullable: false),
                        OutAt = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.OrganizationId)
                .Index(t => t.UserId);
            
            AddColumn("dbo.Punches", "CommitId", c => c.Int(nullable: false));
            CreateIndex("dbo.Punches", "CommitId");

            // Do not delete punches just because commit may get deleted !!
            AddForeignKey("dbo.Punches", "CommitId", "dbo.Commits", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Punches", "CommitId", "dbo.Commits");
            DropForeignKey("dbo.Commits", "UserId", "dbo.Users");
            DropForeignKey("dbo.Commits", "OrganizationId", "dbo.Organizations");
            DropIndex("dbo.Punches", new[] { "CommitId" });
            DropIndex("dbo.Commits", new[] { "UserId" });
            DropIndex("dbo.Commits", new[] { "OrganizationId" });
            DropColumn("dbo.Punches", "CommitId");
            DropTable("dbo.Commits");
        }
    }
}
