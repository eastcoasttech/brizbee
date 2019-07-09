namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingPunchCount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Commits", "PunchCount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Commits", "PunchCount");
        }
    }
}
