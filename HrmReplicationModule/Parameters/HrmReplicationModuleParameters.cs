using Sentez.Parameters;
using Sentez.Common.ModuleBase;

namespace Sentez.HrmReplicationModule.Parameters
{
    public class HrmReplicationModuleParameters : ParameterBase
    {
        string _bookingServiceCode, _bearerCode, _hotelsboomerangGetUrl;
        int _bookingCheckPeriod;

        [ParameterId(1), DefaultValue("AHZ0001")]
        public string BookingServiceCode { get { return _bookingServiceCode; } set { _bookingServiceCode = value; OnPropertyChanged("BookingServiceCode"); } }

        [ParameterId(2), DefaultValue("DEmBWFzkvlWqjRiDjvOfu8lt4Dr81ICFdLqQW7ASkxJ0f5hwXPWWM31EEiAax0yV")]
        public string BearerCode { get { return _bearerCode; } set { _bearerCode = value; OnPropertyChanged("BearerCode"); } }

        [ParameterId(3), DefaultValue("https://hotelsboomerang.com/api/sentez/waiting")]
        public string HotelsboomerangGetUrl { get { return _hotelsboomerangGetUrl; } set { _hotelsboomerangGetUrl = value; OnPropertyChanged("HotelsboomerangGetUrl"); } }

        [ParameterId(4), DefaultValue(15)]
        public int BookingCheckPeriod { get { return _bookingCheckPeriod; } set { _bookingCheckPeriod = value; OnPropertyChanged("BookingCheckPeriod"); } }

        
        public HrmReplicationModuleParameters()
            : base()
        {
            ParameterType = ParameterType.User;
            ModuleId = (short)Modules.ExternalModule22;
        }


        public override IParameter GetNewInstance()
        {
            return new HrmReplicationModuleParameters();
        }
    }
}
