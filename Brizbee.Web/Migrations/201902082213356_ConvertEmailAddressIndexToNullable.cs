namespace Brizbee.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConvertEmailAddressIndexToNullable : DbMigration
    {
        private const string IndexNameUnique = "IX_UsersEmailAddress";

        public override void Up()
        {
            DropIndex("dbo.Users", new[] { "EmailAddress" });
            AlterColumn("dbo.Users", "EmailAddress", c => c.String(nullable: true, maxLength: 254));
            Sql(String.Format(@"CREATE UNIQUE NONCLUSTERED INDEX [{0}]
                                ON [dbo].[Users] ([EmailAddress])
                                WHERE [EmailAddress] IS NOT NULL;", IndexNameUnique));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", IndexNameUnique);
            AlterColumn("dbo.Users", "EmailAddress", c => c.String(nullable: false, maxLength: 254));
            CreateIndex("dbo.Users", "EmailAddress", unique: true);
        }
    }
}
