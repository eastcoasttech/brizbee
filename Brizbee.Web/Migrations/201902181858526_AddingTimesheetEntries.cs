namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTimesheetEntries : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TimesheetEntries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        EnteredAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Minutes = c.Int(nullable: false),
                        Notes = c.String(),
                        TaskId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Tasks", t => t.TaskId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.TaskId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TimesheetEntries", "UserId", "dbo.Users");
            DropForeignKey("dbo.TimesheetEntries", "TaskId", "dbo.Tasks");
            DropIndex("dbo.TimesheetEntries", new[] { "UserId" });
            DropIndex("dbo.TimesheetEntries", new[] { "TaskId" });
            DropTable("dbo.TimesheetEntries");
        }
    }
}
