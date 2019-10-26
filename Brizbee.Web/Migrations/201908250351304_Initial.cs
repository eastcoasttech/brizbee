namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Commits",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        QuickBooksExportedAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        Guid = c.Guid(nullable: false),
                        InAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        OrganizationId = c.Int(nullable: false),
                        OutAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        PunchCount = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.OrganizationId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Organizations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        MinutesFormat = c.String(),
                        Name = c.String(nullable: false),
                        Code = c.String(nullable: false, maxLength: 8),
                        StripeCustomerId = c.String(nullable: false),
                        StripeSourceCardBrand = c.String(),
                        StripeSourceCardExpirationMonth = c.String(),
                        StripeSourceCardExpirationYear = c.String(),
                        StripeSourceCardLast4 = c.String(),
                        StripeSourceId = c.String(),
                        StripeSubscriptionId = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Code, unique: true);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        EmailAddress = c.String(maxLength: 254),
                        IsDeleted = c.Boolean(nullable: false),
                        Name = c.String(nullable: false, maxLength: 128),
                        OrganizationId = c.Int(nullable: false),
                        PasswordSalt = c.String(),
                        PasswordHash = c.String(),
                        Pin = c.String(nullable: false),
                        QuickBooksEmployee = c.String(),
                        Role = c.String(nullable: false, maxLength: 128),
                        TimeZone = c.String(nullable: false),
                        UsesMobileClock = c.Boolean(nullable: false),
                        UsesTouchToneClock = c.Boolean(nullable: false),
                        UsesWebClock = c.Boolean(nullable: false),
                        UsesTimesheets = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: false)
                .Index(t => t.OrganizationId);
            
            CreateTable(
                "dbo.Customers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Description = c.String(),
                        Name = c.String(nullable: false),
                        Number = c.String(nullable: false),
                        OrganizationId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: false)
                .Index(t => t.OrganizationId);
            
            CreateTable(
                "dbo.Jobs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        CustomerId = c.Int(nullable: false),
                        Description = c.String(),
                        Name = c.String(nullable: false),
                        Number = c.String(nullable: false),
                        QuickBooksCustomerJob = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Customers", t => t.CustomerId, cascadeDelete: false)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.Punches",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Guid = c.Guid(nullable: false),
                        InAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        InAtTimeZone = c.String(),
                        LatitudeForInAt = c.String(maxLength: 10),
                        LongitudeForInAt = c.String(maxLength: 10),
                        LatitudeForOutAt = c.String(maxLength: 10),
                        LongitudeForOutAt = c.String(maxLength: 10),
                        OutAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        OutAtTimeZone = c.String(),
                        SourceForInAt = c.String(),
                        SourceForOutAt = c.String(),
                        TaskId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Commits", t => t.CommitId)
                .ForeignKey("dbo.Tasks", t => t.TaskId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.CommitId)
                .Index(t => t.TaskId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Tasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        JobId = c.Int(nullable: false),
                        Name = c.String(nullable: false),
                        Number = c.String(nullable: false),
                        QuickBooksPayrollItem = c.String(),
                        QuickBooksServiceItem = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Jobs", t => t.JobId, cascadeDelete: false)
                .Index(t => t.JobId);
            
            CreateTable(
                "dbo.QuickBooksOnlineExports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AccessToken = c.String(),
                        AccessTokenExpiresAt = c.String(),
                        CommitId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        InAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        OutAt = c.DateTime(precision: 7, storeType: "datetime2"),
                        RefreshToken = c.String(),
                        RefreshTokenExpiresAt = c.String(),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.TaskTemplates",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        OrganizationId = c.Int(nullable: false),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organizations", t => t.OrganizationId, cascadeDelete: false)
                .Index(t => t.OrganizationId);
            
            CreateTable(
                "dbo.TimesheetEntries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        EnteredAt = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        Minutes = c.Int(nullable: false),
                        Notes = c.String(),
                        TaskId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Tasks", t => t.TaskId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.TaskId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TimesheetEntries", "UserId", "dbo.Users");
            DropForeignKey("dbo.TimesheetEntries", "TaskId", "dbo.Tasks");
            DropForeignKey("dbo.TaskTemplates", "OrganizationId", "dbo.Organizations");
            DropForeignKey("dbo.QuickBooksOnlineExports", "UserId", "dbo.Users");
            DropForeignKey("dbo.Punches", "UserId", "dbo.Users");
            DropForeignKey("dbo.Punches", "TaskId", "dbo.Tasks");
            DropForeignKey("dbo.Tasks", "JobId", "dbo.Jobs");
            DropForeignKey("dbo.Punches", "CommitId", "dbo.Commits");
            DropForeignKey("dbo.Jobs", "CustomerId", "dbo.Customers");
            DropForeignKey("dbo.Customers", "OrganizationId", "dbo.Organizations");
            DropForeignKey("dbo.Commits", "UserId", "dbo.Users");
            DropForeignKey("dbo.Users", "OrganizationId", "dbo.Organizations");
            DropForeignKey("dbo.Commits", "OrganizationId", "dbo.Organizations");
            DropIndex("dbo.TimesheetEntries", new[] { "UserId" });
            DropIndex("dbo.TimesheetEntries", new[] { "TaskId" });
            DropIndex("dbo.TaskTemplates", new[] { "OrganizationId" });
            DropIndex("dbo.QuickBooksOnlineExports", new[] { "UserId" });
            DropIndex("dbo.Tasks", new[] { "JobId" });
            DropIndex("dbo.Punches", new[] { "UserId" });
            DropIndex("dbo.Punches", new[] { "TaskId" });
            DropIndex("dbo.Punches", new[] { "CommitId" });
            DropIndex("dbo.Jobs", new[] { "CustomerId" });
            DropIndex("dbo.Customers", new[] { "OrganizationId" });
            DropIndex("dbo.Users", new[] { "OrganizationId" });
            DropIndex("dbo.Organizations", new[] { "Code" });
            DropIndex("dbo.Commits", new[] { "UserId" });
            DropIndex("dbo.Commits", new[] { "OrganizationId" });
            DropTable("dbo.TimesheetEntries");
            DropTable("dbo.TaskTemplates");
            DropTable("dbo.QuickBooksOnlineExports");
            DropTable("dbo.Tasks");
            DropTable("dbo.Punches");
            DropTable("dbo.Jobs");
            DropTable("dbo.Customers");
            DropTable("dbo.Users");
            DropTable("dbo.Organizations");
            DropTable("dbo.Commits");
        }
    }
}
