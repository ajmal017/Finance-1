namespace Finance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomTags3 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Securities", "Excluded");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Securities", "Excluded", c => c.Boolean(nullable: false));
        }
    }
}
