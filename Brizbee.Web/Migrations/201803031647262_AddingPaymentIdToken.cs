namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingPaymentIdToken : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Organizations", "StripePaymentId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Organizations", "StripePaymentId");
        }
    }
}
