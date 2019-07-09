namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIndexAndSetStringLengths : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Users", "EmailAddress", c => c.String(nullable: false, maxLength: 254));
            AlterColumn("dbo.Users", "Name", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.Users", "Role", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.Users", "EmailAddress", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", new[] { "EmailAddress" });
            AlterColumn("dbo.Users", "Role", c => c.String(nullable: false));
            AlterColumn("dbo.Users", "Name", c => c.String(nullable: false));
            AlterColumn("dbo.Users", "EmailAddress", c => c.String(nullable: false));
        }
    }
}
