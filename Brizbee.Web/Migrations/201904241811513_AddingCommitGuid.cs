namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingCommitGuid : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Commits", "Guid", c => c.Guid(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Commits", "Guid");
        }
    }
}
