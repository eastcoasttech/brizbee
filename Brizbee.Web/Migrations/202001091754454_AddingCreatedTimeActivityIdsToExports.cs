namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingCreatedTimeActivityIdsToExports : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.QuickBooksOnlineExports", "CreatedTimeActivitiesIds", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.QuickBooksOnlineExports", "CreatedTimeActivitiesIds");
        }
    }
}
