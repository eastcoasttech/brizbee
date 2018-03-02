namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingStripeDetails : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "StripeCustomerId", c => c.String(nullable: false));
            AddColumn("dbo.Organizations", "StripeSubscriptionId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Organizations", "StripeSubscriptionId");
            DropColumn("dbo.Organizations", "StripeCustomerId");
        }
    }
}
