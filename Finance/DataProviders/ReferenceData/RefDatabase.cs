using Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{
    public class PriceDatabase
    {
        private static PriceDatabase _Instance { get; set; }
        public static PriceDatabase Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new PriceDatabase(Settings.Instance.DatabaseConnectionString);

                return _Instance;
            }
        }

        public string ConnectionString { get; }

        public PriceDatabase(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        //public bool LoadPriceBarData(Security security)
        //{
        //    using (var db = new PriceDatabaseContext())
        //    {
        //        if (db.Entry(security).Collection(s => s.DailyPriceBarData) != null)
        //        {
        //            var ent = db.Entry(security);
        //            ent.State = EntityState.Unchanged;
        //            ent.Collection(x => x.DailyPriceBarData).Load();

        //            return true;
        //        }
        //        return false;
        //    }
        //}

        public Security GetSecurity(string ticker, bool create = true, bool track = false)
        {
            using (var db = new PriceDatabaseContext(ConnectionString))
            {

                ticker = ticker.Trim();

                var ret = (from sec in (track ? db.Securities
                           .Include(x => x.DailyPriceBarData) :
                           db.Securities.AsNoTracking()
                           .Include(x => x.DailyPriceBarData))
                           where sec.Ticker == ticker
                           select sec).SingleOrDefault();


                if (ret != null || !create)
                {
                    return ret;
                }

                ret = db.Securities.AddAndReturn(new Security(ticker));
                db.SaveChanges();

                return ret;
            }
        }
        public bool SetSecurity(Security security, bool Overwrite)
        {

            if (security.DailyPriceBarData.Any(x => x.BarSize != PriceBarSize.Daily))
                throw new UnknownErrorException() { message = "Price Bar data invalid" };

            try
            {
                using (var db = new PriceDatabaseContext(ConnectionString))
                {
                    //// Get the Security entity stored in the database
                    var dbSecurity = (from sec in db.Securities where sec.Ticker == security.Ticker select sec).Include(x => x.DailyPriceBarData).FirstOrDefault();

                    if (dbSecurity == null)
                    {
                        throw new SecurityNotFoundException() { message = "Attempted to reattach non-existent security to database" };
                    }

                    db.Entry(dbSecurity).CurrentValues.SetValues(security);

                    foreach (var newBar in security.DailyPriceBarData)
                    {
                        try
                        {
                            // Skip bars not marked for update
                            if (!newBar.ToUpdate && !Overwrite)
                                continue;

                            // Find existing bar if one exists for this date
                            var currentBar = dbSecurity.DailyPriceBarData.Where(currBar => currBar == newBar);

                            if (currentBar?.Count() > 1)
                            {
                                // More than one bar got saved... delete both and save the new one
                                currentBar.ToList().ForEach(b => db.Entry(b).State = EntityState.Deleted);

                                //// Insert new bar
                                newBar.Security = dbSecurity;
                                dbSecurity.DailyPriceBarData.Add(newBar);
                                newBar.ToUpdate = false;

                                db.SaveChanges();
                            }
                            else if (currentBar.SingleOrDefault() != null)
                            {
                                if (!Overwrite)
                                    continue;
                                // Update bars that are in the new bar collection
                                db.Entry(currentBar).CurrentValues.SetValues(newBar);
                                newBar.ToUpdate = false;
                            }
                            else
                            {
                                //// Insert new bars
                                newBar.Security = dbSecurity;
                                newBar.ToUpdate = false;
                                dbSecurity.DailyPriceBarData.Add(newBar);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }

                    db.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + ".SetSecurity()", $"Could not set data for {security.Ticker}; {ex.Message}", LogMessageType.SystemError));
                return false;
            }
        }
        public void RemoveSecurity(Security security)
        {
            using (var db = new PriceDatabaseContext(ConnectionString))
            {
                try
                {
                    //var sec = GetSecurity(security.Ticker, false, true);
                    //db.Securities.Remove(sec);
                    //db.SaveChanges();
                    db.Securities.Attach(security);
                    db.Entry(security).State = EntityState.Deleted;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Log(new LogMessage(ToString() + ".RemoveSecurity()", ex.Message, LogMessageType.SystemError));
                    //throw ex;
                }
            }

        }

        public List<string> AllTickers()
        {
            using (var db = new PriceDatabaseContext(ConnectionString))
            {
                if (Settings.Instance.Debug)
                    return (from sec in db.Securities select sec.Ticker).Take(Settings.Instance.TestingModeSecurityCount).ToList();
                else
                    return (from sec in db.Securities select sec.Ticker).ToList();
            }
        }
        public List<Security> AllSecurities()
        {
            Log(new LogMessage(ToString(), "Refreshing Securities list...", LogMessageType.Production));

            List<Security> ret;

            using (var db = new PriceDatabaseContext(ConnectionString))
            {
                if (Settings.Instance.TestingModeSecurityEnabled)
                    ret = db.Securities.AsNoTracking().Include(x => x.DailyPriceBarData)
                        .Take(Settings.Instance.TestingModeSecurityCount).ToList();
                else
                    ret = db.Securities.AsNoTracking().Include(x => x.DailyPriceBarData).ToList();

                foreach (Security sec in ret)
                {
                    var maxBar = (from pb in db.PriceBars where pb.Security.Ticker == sec.Ticker select pb.BarDateTime).ToList();

                    sec.LastUpdate = maxBar.Count() == 0 ? DateTime.MinValue : maxBar.Max();
                }

                return ret;
            }
        }
        public int PriceBarCount(string ticker = "")
        {
            using (var db = new PriceDatabaseContext(ConnectionString))
            {
                if (ticker == "")
                    return (from bar in db.PriceBars select bar).Count();
                else
                    return (from bar in db.PriceBars where bar.Security.Ticker == ticker select bar).Count();
            }
        }

    }
    public class PriceDatabaseContext : DbContext
    {
        public DbSet<Security> Securities { get; set; }
        public DbSet<PriceBar> PriceBars { get; set; }

        public PriceDatabaseContext()
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<PriceDatabaseContext>());

            this.Database.CommandTimeout = 180;
        }
        public PriceDatabaseContext(string connectionString)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<PriceDatabaseContext>());

            this.Database.CommandTimeout = 180;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PriceBar>().ToTable("PriceBars");

            modelBuilder.Entity<Security>()
                .HasMany(x => x.DailyPriceBarData)
                .WithOptional(x => x.Security)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }

    }

    public class IndexDatabase
    {
        private static IndexDatabase _Instance { get; set; }
        public static IndexDatabase Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new IndexDatabase();

                return _Instance;
            }
        }

        public string ConnectionString { get; }

        public IndexDatabase() { }

        public List<TrendIndex> GetAllTrendIndices()
        {
            using (var db = new IndexDatabaseContext())
            {
                return db.TrendIndices.AsNoTracking().Include(x => x.IndexEntries).ToList();
            }
        }
        public TrendIndex GetTrendIndex(string sector, PriceBarSize priceBarSize, int swingPointBarCount, bool create = true)
        {
            using (var db = new IndexDatabaseContext())
            {
                var ret = (from index in db.TrendIndices.AsNoTracking().Include(x => x.IndexEntries)
                           where index.IndexName == sector
                           where index.IndexSwingpointBarCount == swingPointBarCount
                           where index.TrendPriceBarSize == priceBarSize
                           select index).FirstOrDefault();

                if (ret != null || !create)
                    return ret;

                db.TrendIndices.Add(new TrendIndex(sector, swingPointBarCount, priceBarSize));
                db.SaveChanges();

                ret = (from index in db.TrendIndices.AsNoTracking().Include(x => x.IndexEntries)
                       where index.IndexName == sector
                       where index.IndexSwingpointBarCount == swingPointBarCount
                       where index.TrendPriceBarSize == priceBarSize
                       select index).FirstOrDefault();
                if (ret == null)
                    Console.WriteLine();

                return ret;
            }
        }
        public void SetTrendIndex(TrendIndex trendIndex)
        {
            if (trendIndex == null)
                return;

            using (var db = new IndexDatabaseContext())
            {
                // Get the index entity stored in the database
                var dbIndex = (from index in db.TrendIndices
                               where index.IndexName == trendIndex.IndexName
                               //where index.IndexSwingpointBarCount == trendIndex.IndexSwingpointBarCount
                               //where index.TrendPriceBarSize == trendIndex.TrendPriceBarSize
                               select index).FirstOrDefault();

                if (dbIndex == null)
                {
                    throw new SecurityNotFoundException() { message = "Attempted to reattach non-existent index to database" };
                }

                // Update scalar properties of Security
                db.Entry(dbIndex).CurrentValues.SetValues(trendIndex);

                foreach (var newIndexDay in trendIndex.IndexEntries)
                {
                    // Find existing bar if one exists for this date
                    var currentIndexDay = dbIndex.IndexEntries.SingleOrDefault(x => x == newIndexDay);
                    if (currentIndexDay != null)
                    {
                        // Update bars that are in the new bar collection
                        db.Entry(currentIndexDay).CurrentValues.SetValues(newIndexDay);
                    }
                    else
                    {
                        // Insert new bars
                        newIndexDay.Parent = dbIndex;
                        dbIndex.IndexEntries.Add(newIndexDay);
                    }
                }

                db.SaveChanges();
            }


        }

    }
    public class IndexDatabaseContext : DbContext
    {

        public DbSet<TrendIndex> TrendIndices { get; set; }
        public DbSet<TrendIndexDay> TrendIndexDays { get; set; }

        public IndexDatabaseContext()
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<IndexDatabaseContext>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrendIndexDay>().ToTable("TrendIndexDays");

            modelBuilder.Entity<TrendIndex>()
                .HasMany(x => x.IndexEntries)
                .WithOptional(x => x.Parent)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class LiveTradeDatabase
    {



    }
    public class LiveTradeDatabaseContext : DbContext
    {

        public DbSet<Trade> ExecutedTrades { get; set; }

    }

}