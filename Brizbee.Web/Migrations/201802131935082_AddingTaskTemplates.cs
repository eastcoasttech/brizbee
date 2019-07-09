namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTaskTemplates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TaskTemplates",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false),
                        OrganizationId = c.Int(nullable: false),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: true)
                .Index(t => t.OrganizationId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TaskTemplates", "OrganizationId", "dbo.Organizations");
            DropIndex("dbo.TaskTemplates", new[] { "OrganizationId" });
            DropTable("dbo.TaskTemplates");
        }
    }
}
