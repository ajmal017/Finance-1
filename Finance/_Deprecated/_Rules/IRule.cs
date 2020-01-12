using Finance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finance
{
    /// <summary>
    /// Structure for a generic Rule which can be implemented in a rule pipeline
    /// </summary>
    public interface IRule<T>
    {
        // Identifies the rule within sequence of pipeline
        int RuleId { get; set; }

        // User-defined rule name
        string RuleName { get; set; }

    }

    /// <summary>
    /// Generic rule base class
    /// </summary>
    public abstract class Rule
    {
        public int RuleId { get; set; }
        public string RuleName { get; set; }
    }

    /// <summary>
    /// Base class for a trade signal rule
    /// </summary>
    public abstract class TradeStrategyRule<Security> : Rule
    {
        public abstract Trade Run(Security sec, Portfolio port, DateTime AsOf);
    }

    /// <summary>
    /// Base class for a rule that approves pending/conditional/indicated trades
    /// </summary>
    public abstract class TradeApprovalRule<Portfolio> : Rule
    {
        public abstract void Run(Portfolio port, Trade trade);
    }

    /// <summary>
    /// Base class for a rule that manages existing positions
    /// </summary>
    public abstract class PositionManagementRule<Portfolio> : Rule
    {
        public abstract void Run(Portfolio port, DateTime AsOf, bool UseOpeningValues = false);
    }

    /// <summary>
    /// Apply to rules to indicate order in which rules must be logically executed within a single pipeline
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RuleExecutionOrderAttribute : Attribute, IEquatable<RuleExecutionOrderAttribute>
    {
        public int Order { get; }

        public RuleExecutionOrderAttribute(int order)
        {
            Order = order;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuleExecutionOrderAttribute);
        }

        public bool Equals(RuleExecutionOrderAttribute other)
        {
            return other != null &&
                   base.Equals(other) &&
                   Order == other.Order;
        }

        public override int GetHashCode()
        {
            var hashCode = 1041847501;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Order.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RuleExecutionOrderAttribute attribute1, RuleExecutionOrderAttribute attribute2)
        {
            return EqualityComparer<RuleExecutionOrderAttribute>.Default.Equals(attribute1, attribute2);
        }

        public static bool operator !=(RuleExecutionOrderAttribute attribute1, RuleExecutionOrderAttribute attribute2)
        {
            return !(attribute1 == attribute2);
        }
    }


}
