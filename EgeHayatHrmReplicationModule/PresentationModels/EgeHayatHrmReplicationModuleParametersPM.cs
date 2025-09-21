using Sentez.Common.PresentationModels;
using Prism.Ioc;
using Sentez.Common.Commands;
using Sentez.Localization;
using LiveCore.Desktop.UI.Controls;
using Sentez.Common.Utilities;
using Sentez.Data.Tools;
using System.Data;
using Sentez.Data.MetaData;
using System.IO;
using System.Windows;
using Sentez.Core.ParameterClasses;
using DevExpress.Xpf.Grid;
using Sentez.EgeHayatHrmReplicationModule.Parameters;

namespace Sentez.EgeHayatHrmReplicationModule.PresentationModels
{
    public class EgeHayatHrmReplicationModuleParametersPM : PMDesktop
    {
        public ProgramParameters ProgramParam { get; set; }
        bool parameterPM = false;

        Visibility companyBasedParameterVisibility = Visibility.Collapsed;
        public Visibility CompanyBasedParameterVisibility
        {
            get
            {
                return companyBasedParameterVisibility;
            }
            set
            {
                companyBasedParameterVisibility = value;
                OnPropertyChanged("CompanyBasedParameterVisibility");
            }
        }

        Visibility workplaceBasedParameterVisibility = Visibility.Collapsed;
        public Visibility WorkplaceBasedParameterVisibility
        {
            get
            {
                return workplaceBasedParameterVisibility;
            }
            set
            {
                workplaceBasedParameterVisibility = value;
                OnPropertyChanged("WorkplaceBasedParameterVisibility");
            }
        }


        int? parameterCompanyId = null;
        public int? ParameterCompanyId
        {
            get
            {
                return parameterCompanyId;
            }
            set
            {
                if (parameterCompanyId == value) return;
                parameterCompanyId = value;
                WorkPlaceDataView = null;
                if (ProgramParam.CanWorkplaceBased == 1 && value != null)
                    using (var workplace = UtilityFunctions.GetDataTableList(ActiveSession.dbInfo.DBProvider, ActiveSession.dbInfo.Connection, null, "Erp_Workplace", $"SELECT NULL RecId, NULL WorkplaceCodeName UNION ALL select RecId, WorkplaceCode+' - '+ WorkplaceName WorkplaceCodeName from Erp_Workplace where InUse=1 and CompanyId={value}  order by WorkplaceCodeName"))
                        WorkPlaceDataView = workplace?.AsDataView();

                OnPropertyChanged("ParameterCompanyId");
                LoadParameters();
            }
        }

        int? parameterWorkplaceId = null;
        public int? ParameterWorkplaceId
        {
            get
            {
                return parameterWorkplaceId;
            }
            set
            {
                if (parameterWorkplaceId == value) return;
                parameterWorkplaceId = value;
                OnPropertyChanged("ParameterWorkplaceId");
                LoadParameters();
            }
        }

        private Visibility canWorkplaceBasedVisibility;
        public Visibility CanWorkplaceBasedVisibility
        {
            get { return canWorkplaceBasedVisibility; }
            set { canWorkplaceBasedVisibility = value; OnPropertyChanged("CanWorkplaceBasedVisibility"); }
        }

        public byte CompanyBasedParam
        {
            get
            {
                if (ProgramParam.CanCompanyBased == 1)
                {
                    CanWorkplaceBasedVisibility = Visibility.Visible;
                }
                else
                {
                    CanWorkplaceBasedVisibility = Visibility.Collapsed;
                    ProgramParam.CanWorkplaceBased = 0;
                }
                return ProgramParam.CanCompanyBased;
            }
            set
            {
                ProgramParam.CanCompanyBased = value;
                OnPropertyChanged("CompanyBasedParam");
            }
        }


        private DataView workplaceDataView;
        public DataView WorkPlaceDataView
        {
            get { return workplaceDataView; }
            set { workplaceDataView = value; OnPropertyChanged("WorkPlaceDataView"); }
        }




        public LookupList Lists { get; set; }

        EgeHayatHrmReplicationModuleParameters parameters;
        public EgeHayatHrmReplicationModuleParameters Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                OnPropertyChanged("Parameters");
            }
        }

        LiveRichEdit lereQuotationTemplate;
        LiveGridControl gridControl;

        public EgeHayatHrmReplicationModuleParametersPM(IContainerExtension container)
            : base(container)
        {
            Parameters = ActiveSession.ParamService.GetParameterClass<EgeHayatHrmReplicationModuleParameters>();
        }

        public override void Init()
        {
            base.Init();
            Lists = ActiveSession.LookupList.GetChild(UtilityFunctions.GetConnection(ActiveSession.dbInfo.DBProvider, ActiveSession.dbInfo.ConnectionString));


            InsertContextMenu(AddToMenu(new MenuItemPM(SLanguage.GetString("Kaydet"), "SaveParametersCommand") { ShortcutKey = System.Windows.Input.Key.F5, ShortcutKeyModifier = System.Windows.Input.ModifierKeys.None }, null));

            if (ProgramParam == null)
                ProgramParam = ActiveSession.ParamService.GetParameterClass<ProgramParameters>();
            if (ProgramParam.CanCompanyBased == 1)
            {
                CompanyBasedParameterVisibility = Visibility.Visible;
                if (ProgramParam.CanWorkplaceBased == 1)
                    WorkplaceBasedParameterVisibility = Visibility.Visible;
            }
            LoadParameters();
        }

        public override void LoadCommands()
        {
            base.LoadCommands();
            CmdList.AddCmd(901, "SaveParametersCommand", SLanguage.GetString("Parametreleri Kaydet"), OnSaveParametersCommand, null);
        }

        void OnSaveParametersCommand(ISysCommandParam arg)
        {
            int? prmCompanyId = ParameterCompanyId;
            int? prmWorkplaceId = ParameterWorkplaceId;

            Parameters?.Save(ParameterCompanyId, ParameterWorkplaceId, null, ActiveSession.dbInfo.Connection);
        }

        void LoadParameters(bool closedPM = false)
        {
            Parameters?.Clear();
            int? prmCompanyId = ParameterCompanyId;
            int? prmWorkplaceId = ParameterWorkplaceId;
            if (closedPM)
            {
                prmCompanyId = ActiveSession.ActiveCompany.RecId;
                if (ActiveSession?.Workplace != null)
                    prmWorkplaceId = ActiveSession.Workplace.RecId;
                Parameters.Load(prmCompanyId, prmWorkplaceId, null, ActiveSession.dbInfo.Connection);
            }
            else
            {
                Parameters.Load(prmCompanyId, prmWorkplaceId, null, ActiveSession.dbInfo.Connection);
            }
        }

        public override bool Closed(object param)
        {
            LoadParameters(true);
            return false;
        }
    }
}