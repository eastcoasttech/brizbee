namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingSourceDetailsAndRates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Rates",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        IsDeleted = c.Boolean(nullable: false),
                        Name = c.String(maxLength: 40),
                        OrganizationId = c.Int(nullable: false),
                        ParentRateId = c.Int(),
                        QBDPayrollItem = c.String(),
                        QBDServiceItem = c.String(),
                        QBOPayrollItem = c.String(),
                        QBOServiceItem = c.String(),
                        Type = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: true)
                .ForeignKey("dbo.Rates", t => t.ParentRateId)
                .Index(t => t.OrganizationId)
                .Index(t => t.ParentRateId)
                .Index(t => t.IsDeleted)
                .Index(t => t.Type);
            
            AddColumn("dbo.Punches", "InAtSourceHardware", c => c.String(maxLength: 12));
            AddColumn("dbo.Punches", "InAtSourceHostname", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "InAtSourceIpAddress", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "InAtSourceOperatingSystem", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "InAtSourceOperatingSystemVersion", c => c.String(maxLength: 20));
            AddColumn("dbo.Punches", "InAtSourceBrowser", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "InAtSourceBrowserVersion", c => c.String(maxLength: 20));
            AddColumn("dbo.Punches", "InAtSourcePhoneNumber", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "OutAtSourceHardware", c => c.String(maxLength: 12));
            AddColumn("dbo.Punches", "OutAtSourceHostname", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "OutAtSourceIpAddress", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "OutAtSourceOperatingSystem", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "OutAtSourceOperatingSystemVersion", c => c.String(maxLength: 20));
            AddColumn("dbo.Punches", "OutAtSourceBrowser", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "OutAtSourceBrowserVersion", c => c.String(maxLength: 20));
            AddColumn("dbo.Punches", "OutAtSourcePhoneNumber", c => c.String(maxLength: 30));
            AddColumn("dbo.Punches", "ServiceRateId", c => c.Int());
            AddColumn("dbo.Punches", "PayrollRateId", c => c.Int());
            AddColumn("dbo.Tasks", "BaseServiceRateId", c => c.Int());
            AddColumn("dbo.Tasks", "BasePayrollRateId", c => c.Int());
            CreateIndex("dbo.Punches", "ServiceRateId");
            CreateIndex("dbo.Punches", "PayrollRateId");
            CreateIndex("dbo.Tasks", "BaseServiceRateId");
            CreateIndex("dbo.Tasks", "BasePayrollRateId");
            AddForeignKey("dbo.Punches", "PayrollRateId", "dbo.Rates", "Id");
            AddForeignKey("dbo.Punches", "ServiceRateId", "dbo.Rates", "Id");
            AddForeignKey("dbo.Tasks", "BasePayrollRateId", "dbo.Rates", "Id");
            AddForeignKey("dbo.Tasks", "BaseServiceRateId", "dbo.Rates", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Tasks", "BaseServiceRateId", "dbo.Rates");
            DropForeignKey("dbo.Tasks", "BasePayrollRateId", "dbo.Rates");
            DropForeignKey("dbo.Punches", "ServiceRateId", "dbo.Rates");
            DropForeignKey("dbo.Punches", "PayrollRateId", "dbo.Rates");
            DropForeignKey("dbo.Rates", "ParentRateId", "dbo.Rates");
            DropForeignKey("dbo.Rates", "OrganizationId", "dbo.Organizations");
            DropIndex("dbo.Tasks", new[] { "BasePayrollRateId" });
            DropIndex("dbo.Tasks", new[] { "BaseServiceRateId" });
            DropIndex("dbo.Rates", new[] { "Type" });
            DropIndex("dbo.Rates", new[] { "IsDeleted" });
            DropIndex("dbo.Rates", new[] { "ParentRateId" });
            DropIndex("dbo.Rates", new[] { "OrganizationId" });
            DropIndex("dbo.Punches", new[] { "PayrollRateId" });
            DropIndex("dbo.Punches", new[] { "ServiceRateId" });
            DropColumn("dbo.Tasks", "BasePayrollRateId");
            DropColumn("dbo.Tasks", "BaseServiceRateId");
            DropColumn("dbo.Punches", "PayrollRateId");
            DropColumn("dbo.Punches", "ServiceRateId");
            DropColumn("dbo.Punches", "OutAtSourcePhoneNumber");
            DropColumn("dbo.Punches", "OutAtSourceBrowserVersion");
            DropColumn("dbo.Punches", "OutAtSourceBrowser");
            DropColumn("dbo.Punches", "OutAtSourceOperatingSystemVersion");
            DropColumn("dbo.Punches", "OutAtSourceOperatingSystem");
            DropColumn("dbo.Punches", "OutAtSourceIpAddress");
            DropColumn("dbo.Punches", "OutAtSourceHostname");
            DropColumn("dbo.Punches", "OutAtSourceHardware");
            DropColumn("dbo.Punches", "InAtSourcePhoneNumber");
            DropColumn("dbo.Punches", "InAtSourceBrowserVersion");
            DropColumn("dbo.Punches", "InAtSourceBrowser");
            DropColumn("dbo.Punches", "InAtSourceOperatingSystemVersion");
            DropColumn("dbo.Punches", "InAtSourceOperatingSystem");
            DropColumn("dbo.Punches", "InAtSourceIpAddress");
            DropColumn("dbo.Punches", "InAtSourceHostname");
            DropColumn("dbo.Punches", "InAtSourceHardware");
            DropTable("dbo.Rates");
        }
    }
}
