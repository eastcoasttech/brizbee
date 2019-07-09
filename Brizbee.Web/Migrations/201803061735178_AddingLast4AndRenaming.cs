namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingLast4AndRenaming : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "StripeSourceCardLast4", c => c.String());
            AddColumn("dbo.Organizations", "StripeSourceId", c => c.String());
            DropColumn("dbo.Organizations", "StripePaymentId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Organizations", "StripePaymentId", c => c.String());
            DropColumn("dbo.Organizations", "StripeSourceId");
            DropColumn("dbo.Organizations", "StripeSourceCardLast4");
        }
    }
}
