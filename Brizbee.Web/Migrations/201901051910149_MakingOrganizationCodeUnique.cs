namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakingOrganizationCodeUnique : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Organizations", "Code", c => c.String(nullable: false, maxLength: 8));
            CreateIndex("dbo.Organizations", "Code", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Organizations", new[] { "Code" });
            AlterColumn("dbo.Organizations", "Code", c => c.String(nullable: false));
        }
    }
}
