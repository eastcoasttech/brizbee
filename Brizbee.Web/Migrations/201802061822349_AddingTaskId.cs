namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTaskId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "TaskId", c => c.Int(nullable: false));
            CreateIndex("dbo.Punches", "TaskId");
            AddForeignKey("dbo.Punches", "TaskId", "dbo.Tasks", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Punches", "TaskId", "dbo.Tasks");
            DropIndex("dbo.Punches", new[] { "TaskId" });
            DropColumn("dbo.Punches", "TaskId");
        }
    }
}
