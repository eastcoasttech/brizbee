namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingStripeColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "StripeSourceCardBrand", c => c.String());
            AddColumn("dbo.Organizations", "StripeSourceCardExpirationMonth", c => c.String());
            AddColumn("dbo.Organizations", "StripeSourceCardExpirationYear", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Organizations", "StripeSourceCardExpirationYear");
            DropColumn("dbo.Organizations", "StripeSourceCardExpirationMonth");
            DropColumn("dbo.Organizations", "StripeSourceCardBrand");
        }
    }
}
