namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingTimeZoneToUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "TimeZone", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "TimeZone");
        }
    }
}
