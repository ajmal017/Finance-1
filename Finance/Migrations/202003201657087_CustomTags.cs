namespace Finance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomTags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Securities", "Excluded", c => c.Boolean(nullable: false));
            DropColumn("dbo.PriceBars", "CustomTags");
            DropColumn("dbo.Securities", "CustomTags");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Securities", "CustomTags", c => c.Int(nullable: false));
            AddColumn("dbo.PriceBars", "CustomTags", c => c.Int(nullable: false));
            DropColumn("dbo.Securities", "Excluded");
        }
    }
}
