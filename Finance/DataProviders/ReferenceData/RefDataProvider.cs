using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;

namespace Finance.Data
{
    public class RefDataProviderRequest : IEquatable<RefDataProviderRequest>
    {
        private static int _NextRequestId = 0;
        private static int NextRequestId => ++_NextRequestId;
        public int RequestID { get; private set; }
        public Security Security { get; private set; }

        public DataProviderRequestType RequestType { get; private set; }
        public DataProviderRequestStatus RequestStatus { get; private set; }
        public DataProviderErrorType DataProviderErrorType { get; private set; }
        public string ErrorMessage { get; private set; }

        public DateTime SubmittedDateTime { get; private set; }
        public (DateTime start, DateTime end) PriceDataRequestRange { get; set; }
        public TimeSpan PriceDataRequestSpan => (PriceDataRequestRange.end - PriceDataRequestRange.start);

        public static RefDataProviderRequest GetContractDataRequest(Security security)
        {
            return new RefDataProviderRequest()
            {
                Security = security,
                RequestType = DataProviderRequestType.SecurityContractData,
                RequestStatus = DataProviderRequestStatus.Pending
            };
        }
        public static RefDataProviderRequest GetPriceDataRequest(Security security, DateTime start, DateTime end)
        {
            // TODO: Weekend values
            if (start == DateTime.MinValue)
                start = Settings.Instance.DataRequestStartLimit;

            if (!Calendar.IsTradingDay(start))
                start = Calendar.NextTradingDay(start);
            if (!Calendar.IsTradingDay(end))
                end = Calendar.PriorTradingDay(end);

            if (start > end)
                throw new InvalidDateOrderException() { message = "Start date must be before or equal to end date" };

            return new RefDataProviderRequest()
            {
                Security = security,
                RequestType = DataProviderRequestType.SecurityPriceData,
                PriceDataRequestRange = AdjustedRequestPeriod((start, end)),
                RequestStatus = DataProviderRequestStatus.Pending
            };
        }
        public static RefDataProviderRequest GetPriceDataRequest(Security security, DateTime end)
        {
            // Update from last bar to end, or earliest possible value if not populated

            if (security.DailyPriceBarData.Count == 0)
                return GetPriceDataRequest(security, DateTime.MinValue, end);

            DateTime start = Calendar.NextTradingDay(security.GetLastBar(PriceBarSize.Daily).BarDateTime);
            return GetPriceDataRequest(security, start, end);
        }
        public static RefDataProviderRequest GetVolumeDataRequest(Security security, DateTime start, DateTime end)
        {
            if (start > end)
                throw new InvalidDateOrderException() { message = "Start date must be before or equal to end date" };

            return new RefDataProviderRequest()
            {
                Security = security,
                RequestType = DataProviderRequestType.SecurityVolumeData,
                PriceDataRequestRange = AdjustedRequestPeriod((start, end)),
                RequestStatus = DataProviderRequestStatus.Pending
            };
        }
        public static RefDataProviderRequest GetCompanyInfoRequest(Security security)
        {
            return new RefDataProviderRequest()
            {
                Security = security,
                RequestType = DataProviderRequestType.SecurityCompanyInfo,
                RequestStatus = DataProviderRequestStatus.Pending
            };
        }

        public int SetNextRequestId()
        {
            RequestID = NextRequestId;
            return RequestID;
        }
        public void MarkPending()
        {
            this.RequestStatus = DataProviderRequestStatus.Pending;
        }
        public void MarkSubmitted(DateTime submittedTime)
        {
            RequestStatus = DataProviderRequestStatus.Submitted;
            SubmittedDateTime = submittedTime;
        }
        public void MarkPartialResponse()
        {
            RequestStatus = DataProviderRequestStatus.PartialResponse;
        }
        public void MarkComplete()
        {
            RequestStatus = DataProviderRequestStatus.CompleteResponse;
            DataProviderErrorType = DataProviderErrorType.NoError;
        }
        public void MarkError(DataProviderErrorType errorType, string errorMessage)
        {
            RequestStatus = DataProviderRequestStatus.ErrorResponse;
            DataProviderErrorType = errorType;
            ErrorMessage = errorMessage;
        }
        public void MarkCancelled(string cancelMessage)
        {
            RequestStatus = DataProviderRequestStatus.Cancelled;
            DataProviderErrorType = DataProviderErrorType.Cancelled;
            ErrorMessage = cancelMessage;
        }
        public void MarkWorking()
        {
            this.RequestStatus = DataProviderRequestStatus.Processing;
        }
        private static (DateTime start, DateTime adjustedEnd) AdjustedRequestPeriod((DateTime start, DateTime end) period)
        {


            if (period.end.Date == DateTime.Today.Date && !Helpers.AfterHours(DateTime.Now))
            {
                // Do not adjust end period to EOD if the market has not closed, otherwise we will get a partial data bar
                return (period.start.Date, period.end.Date); /// Midnight to midnight, does not request data for end date
            }
            else
                return (period.start.Date, period.end.Date.Add(new TimeSpan(23, 59, 0)));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RefDataProviderRequest);
        }
        public bool Equals(RefDataProviderRequest other)
        {
            return other != null &&
                   RequestID == other.RequestID;
        }
        public override int GetHashCode()
        {
            return -1211968265 + RequestID.GetHashCode();
        }
        public static bool operator ==(RefDataProviderRequest left, RefDataProviderRequest right)
        {
            return EqualityComparer<RefDataProviderRequest>.Default.Equals(left, right);
        }
        public static bool operator !=(RefDataProviderRequest left, RefDataProviderRequest right)
        {
            return !(left == right);
        }

        public RefDataProviderRequest Copy()
        {
            RefDataProviderRequest ret = this.MemberwiseClone() as RefDataProviderRequest;
            ret.PriceDataRequestRange = (PriceDataRequestRange.start, PriceDataRequestRange.end);
            return ret;
        }
        public void CopyTo(RefDataProviderRequest copyToRequest)
        {
            // Reflect all public fields to the child
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.SetMethod == null)
                    continue;
                property.SetValue(copyToRequest, property.GetValue(this));
            }
        }
    }
    public class RefDataProviderSupportedSymbolsResponse
    {
        public string Ticker { get; set; }
        public string Exchange { get; set; }
        public string LongName { get; set; }
    }
    public class RefDataProviderSectorListResponse
    {
        public List<string> SectorNames { get; set; } = new List<string>();
    }

    public abstract class RefDataProvider : IProviderStatus
    {
        public static bool NeedsInitialize { get; set; } = false;
        private static DataProviderType CurrentDataProviderType { get; set; }

        private static RefDataProvider _Instance { get; set; }
        public static RefDataProvider Instance(object caller)
        {
            if (caller is RefDataManager dm) { }
            else
                throw new AccessViolationException("Cannot access DataProvider outside of DataManager");

            if (_Instance == null || (Settings.Instance.RefDataProvider != CurrentDataProviderType))
            {
                switch (Settings.Instance.RefDataProvider)
                {
                    case DataProviderType.InteractiveBrokers:
                        CurrentDataProviderType = DataProviderType.InteractiveBrokers;
                        _Instance = new IbkrDataProvider(Settings.Instance.DataProviderPort, 1);
                        NeedsInitialize = true;
                        break;
                    case DataProviderType.IEXCloud:
                        CurrentDataProviderType = DataProviderType.IEXCloud;
                        _Instance = new IexDataProvider();
                        NeedsInitialize = true;
                        break;
                    default:
                        return null;
                }
            }

            return _Instance;
        }

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event DataProviderResponseEventHandler DataProviderRequestResponse;
        protected void OnDataProviderResponse(RefDataProviderRequest request)
        {
            DataProviderRequestResponse?.Invoke(this, new DataProviderResponseEventArgs()
            {
                Request = request
            });
        }

        public event DataProviderSymbolsResponseEventHandler DataProviderSupportedSymbolsResponse;
        protected void OnDataProviderSupportedSymbolsResponse(List<RefDataProviderSupportedSymbolsResponse> symbols)
        {
            DataProviderSupportedSymbolsResponse?.Invoke(this, new DataProviderSupportedSymbolsEventArgs(symbols));
        }

        public event DataProviderSectorsResponseEventHandler DataProviderSectorsResponse;
        protected void OnDataProviderSectorsResponse(RefDataProviderSectorListResponse response)
        {
            DataProviderSectorsResponse?.Invoke(this, new DataProviderSectorsEventArgs(response));
        }

        #endregion
        #region Status Indicator Control

        private ControlStatus _Status { get; set; } = ControlStatus.Offline;
        public ControlStatus Status
        {
            get => _Status;
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        private string _StatusMessage { get; set; } = "";
        public string StatusMessage
        {
            get
            {
                return _StatusMessage;
            }
            set
            {
                if (_StatusMessage != value)
                {
                    _StatusMessage = value;
                    OnPropertyChanged("StatusMessage");
                }
            }
        }

        private string _StatusMessage2 { get; set; } = "";
        public string StatusMessage2
        {
            get
            {
                return _StatusMessage2;
            }
            set
            {
                if (_StatusMessage2 != value)
                {
                    _StatusMessage2 = value;
                    OnPropertyChanged("StatusMessage");
                }
            }
        }

        #endregion

        public abstract string Name { get; }
        private bool _Connected { get; set; }
        public bool Connected
        {
            get
            {
                return _Connected;
            }
            protected set
            {
                if (_Connected != value)
                {
                    _Connected = value;
                    Status = (_Connected ? ControlStatus.Ready : ControlStatus.Offline);
                    OnPropertyChanged("Connected");
                }
            }
        }
        public bool Busy => Status == ControlStatus.Working;

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract void SubmitRequest(RefDataProviderRequest request);
        public abstract void SubmitBatchRequest(List<RefDataProviderRequest> requests);
        public abstract void GetProviderSupportedSymbols();
        public abstract void GetProviderSectors();
        public abstract void CancelAllRequests(string cancelMessage);
    }
}
