namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RefactoringGuid : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "Guid", c => c.Guid(nullable: false));
            DropColumn("dbo.Commits", "QuickBooksExportedGuid");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Commits", "QuickBooksExportedGuid", c => c.Guid());
            DropColumn("dbo.Punches", "Guid");
        }
    }
}
