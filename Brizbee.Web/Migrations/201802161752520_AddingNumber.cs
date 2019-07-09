namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingNumber : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Customers", "Number", c => c.String(nullable: false));
            AddColumn("dbo.Jobs", "Number", c => c.String(nullable: false));
            AddColumn("dbo.Tasks", "Number", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tasks", "Number");
            DropColumn("dbo.Jobs", "Number");
            DropColumn("dbo.Customers", "Number");
        }
    }
}
