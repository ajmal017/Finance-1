namespace Finance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomTags2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Securities", "CustomTags", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Securities", "CustomTags");
        }
    }
}
