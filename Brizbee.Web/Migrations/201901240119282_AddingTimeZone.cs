namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTimeZone : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "TimeZone", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punches", "TimeZone");
        }
    }
}
