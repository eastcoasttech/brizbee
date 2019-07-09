namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingQuickBooksExportDetails : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Commits", "ExportedAt", c => c.DateTime());
            AddColumn("dbo.Commits", "ExportedGuid", c => c.Guid());
            AddColumn("dbo.Tasks", "QuickBooksPayrollItem", c => c.String());
            AddColumn("dbo.Tasks", "QuickBooksServiceItem", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tasks", "QuickBooksServiceItem");
            DropColumn("dbo.Tasks", "QuickBooksPayrollItem");
            DropColumn("dbo.Commits", "ExportedGuid");
            DropColumn("dbo.Commits", "ExportedAt");
        }
    }
}
