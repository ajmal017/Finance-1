using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Contains values fed to a portfolio constructor to initialize various fields
    /// </summary>
    public class PortfolioSetup
    {

        public PortfolioDirection PortfolioDirection { get; set; } = PortfolioDirection.LongShort;
        public PortfolioMarginType PortfolioMarginType { get; set; } = PortfolioMarginType.RegTMargin;

        public decimal InitialCashBalance { get; set; }

        public bool APIused { get; set; } = true;

        public DateTime InceptionDate { get; set; }

        public PortfolioSetup(PortfolioDirection portfolioDirection, PortfolioMarginType portfolioMarginType, decimal initialCashBalance, bool isAPIused, DateTime inceptionDate)
        {
            PortfolioDirection = portfolioDirection;
            PortfolioMarginType = portfolioMarginType;
            InitialCashBalance = initialCashBalance;
            APIused = isAPIused;
            InceptionDate = inceptionDate;
        }

        /// <summary>
        /// Returns a new PortfolioSetup with default parameters
        /// </summary>
        /// <returns></returns>
        public static PortfolioSetup Default()
        {
            return new PortfolioSetup(PortfolioDirection.LongOnly,
                   PortfolioMarginType.RegTMargin,
                   10000.0m,
                   true,
                   DateTime.Today);
        }

        /// <summary>
        /// Return a deep copy of this object
        /// </summary>
        /// <returns></returns>
        public PortfolioSetup Copy()
        {
            return new PortfolioSetup(PortfolioDirection, PortfolioMarginType, InitialCashBalance, APIused, InceptionDate);
        }
    }

}
