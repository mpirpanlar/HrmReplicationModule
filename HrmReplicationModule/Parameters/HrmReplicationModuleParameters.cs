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
