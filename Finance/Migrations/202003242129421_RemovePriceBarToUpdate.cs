namespace Finance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePriceBarToUpdate : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.PriceBars", "ToUpdate");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PriceBars", "ToUpdate", c => c.Boolean(nullable: false));
        }
    }
}
