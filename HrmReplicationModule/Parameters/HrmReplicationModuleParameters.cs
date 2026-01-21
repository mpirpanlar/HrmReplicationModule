using Sentez.Parameters;
using Sentez.Common.ModuleBase;

namespace Sentez.HrmReplicationModule.Parameters
{
    public class HrmReplicationModuleParameters : ParameterBase
    {
        string _targetServer;
        string _targetDatabase;
        string _targetUserName;
        string _targetPassword;
        string _targetCompanyCode;
        string _targetWorkplaceCode;
        string _glAccountCodes;

        [ParameterId(1), DefaultValue("")]
        public string TargetServer 
        { 
            get { return _targetServer; } 
            set { _targetServer = value; OnPropertyChanged("TargetServer"); } 
        }

        [ParameterId(2), DefaultValue("")]
        public string TargetDatabase 
        { 
            get { return _targetDatabase; } 
            set { _targetDatabase = value; OnPropertyChanged("TargetDatabase"); } 
        }

        [ParameterId(3), DefaultValue("")]
        public string TargetUserName 
        { 
            get { return _targetUserName; } 
            set { _targetUserName = value; OnPropertyChanged("TargetUserName"); } 
        }

        [ParameterId(4), DefaultValue("")]
        public string TargetPassword 
        { 
            get { return _targetPassword; } 
            set { _targetPassword = value; OnPropertyChanged("TargetPassword"); } 
        }

        [ParameterId(5), DefaultValue("")]
        public string TargetCompanyCode 
        { 
            get { return _targetCompanyCode; } 
            set { _targetCompanyCode = value; OnPropertyChanged("TargetCompanyCode"); } 
        }

        [ParameterId(6), DefaultValue("")]
        public string TargetWorkplaceCode 
        { 
            get { return _targetWorkplaceCode; } 
            set { _targetWorkplaceCode = value; OnPropertyChanged("TargetWorkplaceCode"); } 
        }

        [ParameterId(7), DefaultValue("")]
        public string GLAccountCodes 
        { 
            get { return _glAccountCodes; } 
            set { _glAccountCodes = value; OnPropertyChanged("GLAccountCodes"); } 
        }

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
