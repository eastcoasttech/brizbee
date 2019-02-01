namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenamingCountryToCountryCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "CountryCode", c => c.String());
            DropColumn("dbo.Organizations", "Country");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Organizations", "Country", c => c.String());
            DropColumn("dbo.Organizations", "CountryCode");
        }
    }
}
