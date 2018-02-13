namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingPin : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "Code", c => c.String(nullable: false));
            AddColumn("dbo.Users", "Pin", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "Pin");
            DropColumn("dbo.Organizations", "Code");
        }
    }
}
