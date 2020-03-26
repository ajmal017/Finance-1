namespace Finance.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Finance.Data.PriceDatabaseContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            // New timeout in seconds
            this.CommandTimeout = 60 * 30;
        }

        protected override void Seed(Finance.Data.PriceDatabaseContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
