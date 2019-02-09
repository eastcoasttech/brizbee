namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakingIsDeletedRequiredAndRemovingPassword : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Users", "Pin", c => c.String(nullable: false));
            AlterColumn("dbo.Users", "TimeZone", c => c.String(nullable: false));
            DropColumn("dbo.Users", "Password");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Users", "Password", c => c.String());
            AlterColumn("dbo.Users", "TimeZone", c => c.String());
            AlterColumn("dbo.Users", "Pin", c => c.String());
        }
    }
}
