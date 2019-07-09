namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConvertingToDateTime2AndRemoveUserTimeZone : DbMigration
    {
        private const string IndexNameUnique = "IX_PunchesInAtOutAtTaskIdUserId";

        public override void Up()
        {
            DropIndex("dbo.Punches", IndexNameUnique);
            AlterColumn("dbo.Commits", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Commits", "QuickBooksExportedAt", c => c.DateTime(precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Commits", "InAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Commits", "OutAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Organizations", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Users", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Customers", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Jobs", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Punches", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Punches", "InAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTime(precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Tasks", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.TaskTemplates", "CreatedAt", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            DropColumn("dbo.Users", "TimeZone");
            Sql(String.Format(@"CREATE UNIQUE INDEX [{0}]
                                ON [dbo].[Punches] ([InAt], [OutAt], [TaskId], [UserId]);", IndexNameUnique));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Punches", IndexNameUnique);
            AddColumn("dbo.Users", "TimeZone", c => c.String());
            AlterColumn("dbo.TaskTemplates", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Tasks", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTimeOffset(precision: 7));
            AlterColumn("dbo.Punches", "InAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Punches", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Jobs", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Customers", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Users", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Organizations", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Commits", "OutAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Commits", "InAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Commits", "QuickBooksExportedAt", c => c.DateTimeOffset(precision: 7));
            AlterColumn("dbo.Commits", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            Sql(String.Format(@"CREATE UNIQUE INDEX [{0}]
                                ON [dbo].[Punches] ([InAt], [OutAt], [TaskId], [UserId]);", IndexNameUnique));
        }
    }
}
