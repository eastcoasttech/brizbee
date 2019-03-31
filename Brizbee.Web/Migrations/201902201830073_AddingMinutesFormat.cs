namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingMinutesFormat : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "MinutesFormat", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Organizations", "MinutesFormat");
        }
    }
}
