namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingSourceForInAndOut : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "SourceForInAt", c => c.String());
            AddColumn("dbo.Punches", "SourceForOutAt", c => c.String());
            DropColumn("dbo.Punches", "Source");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Punches", "Source", c => c.String());
            DropColumn("dbo.Punches", "SourceForOutAt");
            DropColumn("dbo.Punches", "SourceForInAt");
        }
    }
}
