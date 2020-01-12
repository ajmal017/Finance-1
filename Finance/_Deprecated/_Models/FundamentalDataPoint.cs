using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Finance.Calendar;
using static Finance.Helpers;


namespace Finance
{
    public partial class FundamentalDataPoint : IEquatable<FundamentalDataPoint>
    {
        public int Id { get; set; }

        [Key]
        public virtual Security Security { get; set; }

        [Key]
        public string DataPointId { get; set; }

        public decimal DataPointValue { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as FundamentalDataPoint);
        }

        public bool Equals(FundamentalDataPoint other)
        {
            return other != null &&
                   EqualityComparer<Security>.Default.Equals(Security, other.Security) &&
                   DataPointId == other.DataPointId;
        }

        public override int GetHashCode()
        {
            var hashCode = -86004105;
            hashCode = hashCode * -1521134295 + EqualityComparer<Security>.Default.GetHashCode(Security);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataPointId);
            return hashCode;
        }

        public static bool operator ==(FundamentalDataPoint point1, FundamentalDataPoint point2)
        {
            return EqualityComparer<FundamentalDataPoint>.Default.Equals(point1, point2);
        }

        public static bool operator !=(FundamentalDataPoint point1, FundamentalDataPoint point2)
        {
            return !(point1 == point2);
        }
    }
}
