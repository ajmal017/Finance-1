using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Finance.Calendar;

namespace Finance
{
    public class TrendInfo
    {
        public SwingPointType SwingPointType { get; set; } = SwingPointType.None;
        public TrendQualification TrendType { get; set; } = TrendQualification.AmbivalentSideways;
        public SwingPointTest SwingPointTestType { get; set; } = SwingPointTest.None;
        public SwingPointTestPriceResult SwingPointTestPriceResult { get; set; }
        public SwingPointTestVolumeResult SwingPointTestVolumeResult { get; set; }
    }
}
