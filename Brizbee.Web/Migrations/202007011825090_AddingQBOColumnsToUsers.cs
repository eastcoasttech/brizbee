namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingQBOColumnsToUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "QBOGivenName", c => c.String());
            AddColumn("dbo.Users", "QBOMiddleName", c => c.String());
            AddColumn("dbo.Users", "QBOFamilyName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "QBOFamilyName");
            DropColumn("dbo.Users", "QBOMiddleName");
            DropColumn("dbo.Users", "QBOGivenName");
        }
    }
}
