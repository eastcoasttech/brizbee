namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingQBEmployee : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "QuickBooksEmployee", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "QuickBooksEmployee");
        }
    }
}
