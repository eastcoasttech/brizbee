namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTimeZones : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "TimeZone", c => c.String());
            AddColumn("dbo.Users", "TimeZone", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "TimeZone");
            DropColumn("dbo.Organizations", "TimeZone");
        }
    }
}
