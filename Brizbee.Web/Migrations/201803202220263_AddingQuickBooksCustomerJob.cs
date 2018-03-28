namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingQuickBooksCustomerJob : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Jobs", "QuickBooksCustomerJob", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Jobs", "QuickBooksCustomerJob");
        }
    }
}
