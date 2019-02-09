namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingUniqueOptionForEmailAddress : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Users", new[] { "EmailAddress" });
        }
        
        public override void Down()
        {
            CreateIndex("dbo.Users", "EmailAddress", unique: true);
        }
    }
}
