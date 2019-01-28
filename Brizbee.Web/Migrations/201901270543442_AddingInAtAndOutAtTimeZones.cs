namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingInAtAndOutAtTimeZones : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "InAtTimeZone", c => c.String());
            AddColumn("dbo.Punches", "OutAtTimeZone", c => c.String());
            DropColumn("dbo.Punches", "TimeZone");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Punches", "TimeZone", c => c.String());
            DropColumn("dbo.Punches", "OutAtTimeZone");
            DropColumn("dbo.Punches", "InAtTimeZone");
        }
    }
}
