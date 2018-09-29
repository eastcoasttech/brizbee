namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingSource : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Punches", "Source", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punches", "Source");
        }
    }
}
