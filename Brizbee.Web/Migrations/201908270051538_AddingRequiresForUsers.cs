namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingRequiresForUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "RequiresLocation", c => c.Boolean(nullable: false));
            AddColumn("dbo.Users", "RequiresPhoto", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "RequiresPhoto");
            DropColumn("dbo.Users", "RequiresLocation");
        }
    }
}
