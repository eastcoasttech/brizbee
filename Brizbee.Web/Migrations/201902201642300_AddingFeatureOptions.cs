namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingFeatureOptions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "UsesMobileClock", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Users", "UsesTouchToneClock", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Users", "UsesWebClock", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Users", "UsesTimesheets", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "UsesTimesheets");
            DropColumn("dbo.Users", "UsesWebClock");
            DropColumn("dbo.Users", "UsesTouchToneClock");
            DropColumn("dbo.Users", "UsesMobileClock");
        }
    }
}
