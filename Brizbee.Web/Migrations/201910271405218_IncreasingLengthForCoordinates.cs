namespace Brizbee.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IncreasingLengthForCoordinates : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Punches", "LatitudeForInAt", c => c.String(maxLength: 20));
            AlterColumn("dbo.Punches", "LongitudeForInAt", c => c.String(maxLength: 20));
            AlterColumn("dbo.Punches", "LatitudeForOutAt", c => c.String(maxLength: 20));
            AlterColumn("dbo.Punches", "LongitudeForOutAt", c => c.String(maxLength: 20));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Punches", "LongitudeForOutAt", c => c.String(maxLength: 10));
            AlterColumn("dbo.Punches", "LatitudeForOutAt", c => c.String(maxLength: 10));
            AlterColumn("dbo.Punches", "LongitudeForInAt", c => c.String(maxLength: 10));
            AlterColumn("dbo.Punches", "LatitudeForInAt", c => c.String(maxLength: 10));
        }
    }
}
