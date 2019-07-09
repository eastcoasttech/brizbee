namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingRequiredOutAt : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTime(nullable: false));
        }
    }
}
