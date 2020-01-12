using Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Helpers;
using static Finance.Logger;

namespace Finance.Data
{
    public class PriceDatabase
    {
        
        /// <summary>
        /// Retrieves a security from the database, by default creates a new security if none exists
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        public Security GetSecurity(string ticker, bool create = true, bool track = false)
        {
            using (var db = new PriceDatabaseContext())
            {
                var ret = (from sec in (track ? db.Securities
                           .Include(x => x.PriceBarData) : db.Securities.AsNoTracking()
                           .Include(x => x.PriceBarData))
                           where sec.Ticker == ticker
                           select sec).FirstOrDefault();

                if (ret != null || !create)
                    return ret;

                ret = new Security(ticker);
                db.Securities.Add(ret);
                db.SaveChanges();

                return ret;
            }
        }

        /// <summary>
        /// Reattaches and saves an existing security to the database
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public void SetSecurity(Security security, bool OverwriteAll)
        {
            try
            {
                using (var db = new PriceDatabaseContext())
                {
                    // Get the Security entity stored in the database
                    var dbSecurity = (from sec in db.Securities where sec.Ticker == security.Ticker select sec).FirstOrDefault();

                    if (dbSecurity == null)
                    {
                        throw new SecurityNotFoundException() { message = "Attempted to reattach non-existent security to database" };
                    }

                    // Update scalar properties of Security
                    db.Entry(dbSecurity).CurrentValues.SetValues(security);

                    foreach (var newBar in security.PriceBarData)
                    {
                        // Skip bars not marked for update
                        if (!newBar.ToUpdate && !OverwriteAll)
                            continue;

                        // Find existing bar if one exists for this date
                        var currentBar = dbSecurity.PriceBarData.SingleOrDefault(currBar => currBar == newBar);
                        if (currentBar != null)
                        {
                            if (!OverwriteAll)
                                continue;
                            // Update bars that are in the new bar collection
                            db.Entry(currentBar).CurrentValues.SetValues(newBar);
                        }
                        else
                        {
                            // Insert new bars
                            newBar.Security = dbSecurity;
                            dbSecurity.PriceBarData.Add(newBar);
                        }
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log(new LogMessage(ToString() + ".SetSecurity()", ex.Message, LogMessageType.Error));
                throw ex;
            }
        }

        /// <summary>
        /// Deletes a security and all data from the database
        /// </summary>
        /// <param name="security"></param>
        public void RemoveSecurity(Security security)
        {
            using (var db = new PriceDatabaseContext())
            {
                try
                {
                    db.Entry(security).State = EntityState.Deleted;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Log(new LogMessage(ToString() + ".RemoveSecurity()", ex.Message, LogMessageType.Error));
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Gets a list of all saved tickers
        /// </summary>
        public List<string> AllTickers
        {
            get
            {
                using (var db = new PriceDatabaseContext())
                {
                    var a = (from sec in db.Securities select sec.Ticker).ToList();
                    return a;
                }
            }
        }

        /// <summary>
        /// Gets a list of all securities
        /// </summary>
        /// <returns></returns>
        public List<Security> AllSecurities()
        {
            var sw = new Stopwatch();
            Log(new LogMessage(ToString(), "Request from database for AllSecurity refresh...", LogMessageType.Production));

            using (var db = new PriceDatabaseContext())
            {
                sw.Start();
                var ret = db.Securities.AsNoTracking().Include(x => x.PriceBarData).ToList();
                sw.Stop();
                Log(new LogMessage(ToString(), $" !!! Database load took {sw.ElapsedMilliseconds} ms to load {ret.Count} securities", LogMessageType.Production));
                return ret;
            }

        }

        /// <summary>
        /// Returns the count of current price bars
        /// </summary>
        /// <returns></returns>
        public int PriceBarCount(string ticker = "")
        {
            using (var db = new PriceDatabaseContext())
            {
                if (ticker == "")
                    return (from bar in db.PriceBars select bar).Count();
                else
                    return (from bar in db.PriceBars where bar.Security.Ticker == ticker select bar).Count();
            }
        }

    }

    /// <summary>
    /// Database context implementation
    /// </summary>
    public class PriceDatabaseContext : DbContext
    {

        public DbSet<Security> Securities { get; set; }
        public DbSet<PriceBar> PriceBars { get; set; }

        public PriceDatabaseContext()
        {
            Database.SetInitializer<PriceDatabaseContext>(new DropCreateDatabaseIfModelChanges<PriceDatabaseContext>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PriceBar>().ToTable("PriceBars");

            modelBuilder.Entity<Security>()
                .HasMany(x => x.PriceBarData)
                .WithOptional(x => x.Security)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }

    }

}
