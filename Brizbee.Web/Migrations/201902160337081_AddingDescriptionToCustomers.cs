namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingDescriptionToCustomers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Customers", "Description", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Customers", "Description");
        }
    }
}
