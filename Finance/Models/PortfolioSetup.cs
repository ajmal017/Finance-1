using System;
using System.Collections.Generic;
using System.Configuration;
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
        [SettingsCategory(SettingsType.PortfolioParameters, typeof(PortfolioDirection))]
        [SettingsDescription("Portfolio Allowed Trading")]
        public PortfolioDirection PortfolioDirection { get; set; }
        [SettingsCategory(SettingsType.PortfolioParameters, typeof(PortfolioMarginType))]
        [SettingsDescription("Portfolio margin type")]
        public PortfolioMarginType PortfolioMarginType { get; set; }

        [SettingsCategory(SettingsType.PortfolioParameters, typeof(decimal))]
        [SettingsDescription("Initial Cash Balance")]
        public decimal InitialCashBalance { get; set; }

        public DateTime InceptionDate { get; set; }

        public PortfolioSetup(PortfolioDirection portfolioDirection, PortfolioMarginType portfolioMarginType, decimal initialCashBalance, DateTime inceptionDate)
        {
            PortfolioDirection = portfolioDirection;
            PortfolioMarginType = portfolioMarginType;
            InitialCashBalance = initialCashBalance;
            InceptionDate = inceptionDate;
        }

        /// <summary>
        /// Returns a new PortfolioSetup with default parameters
        /// </summary>
        /// <returns></returns>
        public static PortfolioSetup Default()
        {
            return new PortfolioSetup(
                Settings.Instance.PortfolioDirection,
                Settings.Instance.PortfolioMarginType,
                Settings.Instance.PortfolioStartingBalance,
                DateTime.Today);
        }

        /// <summary>
        /// Return a deep copy of this object
        /// </summary>
        /// <returns></returns>
        public PortfolioSetup Copy()
        {
            return new PortfolioSetup(PortfolioDirection, PortfolioMarginType, InitialCashBalance, InceptionDate);
        }
    }

}
