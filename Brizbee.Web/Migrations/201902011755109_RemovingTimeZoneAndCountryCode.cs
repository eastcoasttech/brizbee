namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingTimeZoneAndCountryCode : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Organizations", "CountryCode");
            DropColumn("dbo.Organizations", "TimeZone");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Organizations", "TimeZone", c => c.String());
            AddColumn("dbo.Organizations", "CountryCode", c => c.String());
        }
    }
}
