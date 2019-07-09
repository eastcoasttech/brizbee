namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Changes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Commits", "QuickBooksExportedAt", c => c.DateTime());
            AddColumn("dbo.Commits", "QuickBooksExportedGuid", c => c.Guid());
            DropColumn("dbo.Commits", "ExportedAt");
            DropColumn("dbo.Commits", "ExportedGuid");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Commits", "ExportedGuid", c => c.Guid());
            AddColumn("dbo.Commits", "ExportedAt", c => c.DateTime());
            DropColumn("dbo.Commits", "QuickBooksExportedGuid");
            DropColumn("dbo.Commits", "QuickBooksExportedAt");
        }
    }
}
