namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingUniqueIndexToPunches : DbMigration
    {
        private const string IndexNameUnique = "IX_PunchesInAtOutAtTaskIdUserId";

        public override void Up()
        {
            Sql(String.Format(@"CREATE UNIQUE INDEX [{0}]
                                ON [dbo].[Punches] ([InAt], [OutAt], [TaskId], [UserId]);", IndexNameUnique));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Punches", IndexNameUnique);
        }
    }
}
