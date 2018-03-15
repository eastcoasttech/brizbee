namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PaymentIsOptional : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Organizations", "StripePaymentId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Organizations", "StripePaymentId", c => c.String(nullable: false));
        }
    }
}
