namespace Finance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomTags31 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PriceBars", "CustomTags", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PriceBars", "CustomTags");
        }
    }
}
