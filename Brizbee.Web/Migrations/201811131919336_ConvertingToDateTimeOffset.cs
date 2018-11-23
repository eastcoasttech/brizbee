namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConvertingToDateTimeOffset : DbMigration
    {
        private const string IndexNameUnique = "IX_PunchesInAtOutAtTaskIdUserId";

        public override void Up()
        {
            DropIndex("dbo.Punches", IndexNameUnique);
            AlterColumn("dbo.Commits", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Commits", "QuickBooksExportedAt", c => c.DateTimeOffset(precision: 7));
            AlterColumn("dbo.Commits", "InAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Commits", "OutAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Organizations", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Users", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Customers", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Jobs", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Punches", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Punches", "InAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTimeOffset(precision: 7));
            AlterColumn("dbo.Tasks", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.TaskTemplates", "CreatedAt", c => c.DateTimeOffset(nullable: false, precision: 7));
            Sql(String.Format(@"CREATE UNIQUE INDEX [{0}]
                                ON [dbo].[Punches] ([InAt], [OutAt], [TaskId], [UserId]);", IndexNameUnique));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Punches", IndexNameUnique);
            AlterColumn("dbo.TaskTemplates", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Tasks", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Punches", "OutAt", c => c.DateTime());
            AlterColumn("dbo.Punches", "InAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Punches", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Jobs", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Customers", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Users", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Organizations", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Commits", "OutAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Commits", "InAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Commits", "QuickBooksExportedAt", c => c.DateTime());
            AlterColumn("dbo.Commits", "CreatedAt", c => c.DateTime(nullable: false));
            Sql(String.Format(@"CREATE UNIQUE INDEX [{0}]
                                ON [dbo].[Punches] ([InAt], [OutAt], [TaskId], [UserId]);", IndexNameUnique));
        }
    }
}
