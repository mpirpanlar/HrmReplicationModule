using DevExpress.Xpf.Core;

using HrmReplicationModule.Services;

using LiveCore.Desktop.Common;
using LiveCore.Desktop.SBase.MenuManager;
using LiveCore.Desktop.UI.Controls;

using Newtonsoft.Json;

using Prism.Ioc;

using Sentez.Common;
using Sentez.Common.Commands;
using Sentez.Common.ModuleBase;
using Sentez.Common.PresentationModels;
using Sentez.Common.Replication;
using Sentez.Common.Report;
using Sentez.Common.ResourceManager;
using Sentez.Common.SBase;
using Sentez.Common.SystemServices;
using Sentez.Common.Utilities;
using Sentez.Core.ParameterClasses;
using Sentez.Data.BusinessObjects;
using Sentez.Data.MetaData;
using Sentez.Data.MetaData.DatabaseControl;
using Sentez.Data.Tools;
using Sentez.HrmReplicationModule.BoExtensions;
using Sentez.HrmReplicationModule.Parameters;
using Sentez.HrmReplicationModule.PresentationModels;
using Sentez.Localization;
using Sentez.MetaPosModule.ParameterClasses;
using Sentez.Parameters;
using Sentez.PosModule.PresentationModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Sentez.HrmReplicationModule
{
    public partial class HrmReplicationModule : LiveModule
    {
        SysMng _sysMng;
        LiveSession liveSession = null;
        ISystemService _createBulkUpdateTaskService;
        ISysCommand hrmTimeBulkUpdateCommand;

        HrmReplicationModuleParameters HrmReplicationModuleParameters;

        LiveSession ActiveSession
        {
            get
            {
                return SysMng.Instance.getSession();
            }
        }

        public Stream _MenuDefination = null;
        public override Stream MenuDefination
        {
            get
            {
                return _MenuDefination;
            }
        }

        public override short moduleID { get { return (short)Modules.ExternalModule22; } }

        public HrmReplicationModule(IContainerExtension container)
        {
            _container = container;
            _sysMng = _container.Resolve<SysMng>();
            if (_sysMng != null)
            {
                _sysMng.AfterLogin += _sysMng_AfterLogin;
                _sysMng.AfterDesktopLogin += _sysMng_AfterDesktopLogin;
                _sysMng.BeforeLogout += _sysMng_BeforeLogout;
            }
        }

        private void _sysMng_AfterLogin(object sender, EventArgs e)
        {
            RegisterServiceCommands();
        }

        public override void OnRegister(IContainerRegistry containerRegistry)
        {
            RegisterCoreDocuments();
            RegisterBO();
            RegisterViews();
            RegisterRes();
            RegisterRpr();
            RegisterPM();
            RegisterModuleCommands();
            RegisterServices();
            RegisterList();
            HrmReplicationModuleSecurity.RegisterSecurityDefinitions();

            int hrmTypeStart = 701;
            var ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Mesai Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMTimeBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Ek Ödeme-Ek Kesinti Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMAddPaymentDeductionBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-İzin Grup Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMLeaveGroupBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-İzin Tipleri Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMLeaveTypeBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Pozisyon Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "PositionBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Ayrılış Neden Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmQuitBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Meslek Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmProfessionBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Devamsızlık Nedeni Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmAbsenceBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Vergi Dilim Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmTaxSegmentBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-SGP Prim Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmSsiPrmBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Tehlike Oran Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HrmHazardSegmentBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Sicil Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMEmployeeBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = hrmTypeStart++,
                TypeName = SLanguage.GetString("IK-Sicil Kartı Puantaj Bilgileri"),
                Module = (short)Modules.HRMModule,
                BoName = "CheckingBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);
        }

        private void RegisterServiceCommands()
        {
            var prm = ActiveSession.ParamService.GetParameterClass("ReplicationParameters");
            if (prm.GetValue<int>("ReplicationEnabled") != 1)
                return;

            #region Mesai Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "HrmTimeBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HRMTimeBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_TimeTimeCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Ek Ödeme-Ek Kesinti Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HRMAddPaymentDeductionBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_AddPaymentDeductionAddPaymentCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region İzin Grup Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HRMLeaveGroupBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_LeaveGroupLeaveCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region İzin Tipleri Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HRMLeaveTypeBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_LeaveTypeLeaveTypeCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Pozisyon Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "PositionBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_PositionPositionCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Ayrılış Neden Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmQuitBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmQuitQuitCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Meslek Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmProfessionBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmProfessionProfessionCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Devamsızlık Nedeni Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmAbsenceBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmAbsenceAbsenceCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Vergi Dilim Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmTaxSegmentBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmTaxSegmentHrmTaxSegmentList", hrmTimeBulkUpdateCommand);
            #endregion

            #region SGP Prim Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmSsiPrmBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmSsiPrmHrmSsiPrmList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Tehlike Oran Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HrmHazardSegmentBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Meta_HrmHazardSegmentHrmHazardSegmentList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Sicil Kartları
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder-Sicil Kartları"), "HRMEmployeeBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Erp_EmployeeHRMEmployeeCodeList", hrmTimeBulkUpdateCommand);
            #endregion

            #region Sicil Kartı Puantaj Bilgileri
            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "AddPaymentDeductionBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder-Puantaj Bilgileri"), "CheckingBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Erp_EmployeeHRMEmployeeCodeList", hrmTimeBulkUpdateCommand);
            #endregion
        }

        private void OnBulkUpdateCommand(ISysCommandParam prm)
        {
            if (prm == null || prm.SelectedFields == null || prm.SelectedFields.Count == 0)
                return;
            CreateBulkUpdateTask(prm.BoName, prm.SelectedFields.ToArray());
        }

        void CreateBulkUpdateTask(string boName, object[] ids)
        {
            CreateBulkUpdateTask(boName, ids, true);
        }

        void CreateBulkUpdateTask(string boName, object[] ids, bool bulkSend)
        {
            int i = (int)_createBulkUpdateTaskService.Execute(boName, ids, bulkSend);
            if (i > 0)
                DXMessageBox.Show(SLanguage.GetString($"Uzak noktalara gönderme işlemi sıraya alındı. Kayıt Sayısı {i}"));
            else
                DXMessageBox.Show(SLanguage.GetString($"İşlem iptal Edildi."));
        }

        private bool CanBulkUpdateCommand(ISysCommandParam obj)
        {
            var prm = ActiveSession.ParamService.GetParameterClass<ReplicationParameters>();
            return prm.ReplicationEnabled == 1;
        }

        void AddExtraContextItems(string pmName, ISysCommand command)
        {
            var t = _sysMng.ExtraContextItems.GetItems(pmName);
            if (!t.Any(x => x.Caption == command.Caption))
            {
                _sysMng.ExtraContextItems.Add(pmName, new MenuItemPM("Separator_Replication", command.Name + "Sp"));
                _sysMng.ExtraContextItems.Add(pmName, new MenuItemPM()
                {
                    MenuItemCommand = command,
                    Caption = command.Caption,
                    Name = command.Name,
                    //MenuItemCommandParam = new SysCommandParam()
                });
            }
        }

        private void InitExternalParameter(Dictionary<int, object> externalParamerters)
        {
            externalParamerters.Add((int)InventoryParameterType.INV_CodeTemplates, "");
            //externalParamerters.Add((int)TseParameterType.TSE_FiskalyIntegrationTypeForTableReceipt, (byte)0);
        }

        public override void OnInitialize(IContainerProvider containerProvider)
        {
            _sysMng.AddApplication("HrmReplicationModule");
        }

        public override void RegisterModuleCommands()
        {
        }

        public void Initialize()
        {
        }

        private void RegisterBO()
        {
        }

        private void RegisterServices()
        {
            ParameterFactory.StaticFactory.RegisterParameterClass(typeof(HrmReplicationModuleParameters), (int)Modules.ExternalModule22);
            _container.Register<ISystemService, CreatMetaDataFieldsService>("CreatMetaDataFieldsService");
            BusinessObjectBase.AddCustomExtension("InvoiceBO", typeof(CurrentAccountReplicationTaskExtension));
        }

        private void RegisterRes()
        {
            ResMng.AddRes("HrmReplicationModuleMenu", "HrmReplicationModule;component/ModuleMenu.xml", ResSource.Resource, ResourceType.MenuXml, Modules.ExternalModule22, 0, 0);
        }

        private void RegisterList()
        {
        }

        private void RegisterViews()
        {
            ResMng.AddRes("HrmReplicationModuleParametersView", "HrmReplicationModule;component/Views/HrmReplicationModuleParameters.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
        }

        private void RegisterPM()
        {
            _container.Register<IPMBase, HrmReplicationModuleParametersPM>("HrmReplicationModuleParametersPM");
        }

        private void RegisterRpr()
        {
        }

        public void RegisterCoreDocuments()
        {
            Schema.ReadXml(Assembly.GetAssembly(typeof(HrmReplicationModule)).GetManifestResourceStream("HrmReplicationModule.HrmReplicationModuleDataSchema.xml"));
            DbCreator.AddRegistration(3021, HrmReplicationModuleDbUpdateScript);
        }

        DbScripts HrmReplicationModuleDbUpdateScript(DbCreator instance)
        {
            return DbScripts.LoadFromAssembly(Assembly.GetAssembly(typeof(HrmReplicationModule)), "HrmReplicationModule.HrmReplicationModuleDbUpdateScripts.xml");
        }

        private CancellationTokenSource bilgeceBoomerangCts;
        private static readonly object bilgeceBoomerangLockKey = new object();

        private void _sysMng_AfterDesktopLogin(object sender, EventArgs e)
        {
            liveSession = _sysMng.getSession();

            // 1. Bu cari karta ait hareketler replike edilecek mi?
            if (!Schema.Tables["Erp_CurrentAccount"].Fields.Contains("UD_IsTransactionReplicable"))
                CreatMetaDataFieldsService.CreatMetaDataFields(
                    "Erp_CurrentAccount",
                    "UD_IsTransactionReplicable",
                    SLanguage.GetString("Replikasyon Aktif"),
                    (byte)UdtType.UdtBool,
                    (byte)FieldUsage.Bool,
                    (byte)EditorType.CheckBox,
                    (byte)ValueInputMethod.FreeType,
                    0
                );

            // 2. Replikasyon başlangıç tarihi
            if (!Schema.Tables["Erp_CurrentAccount"].Fields.Contains("UD_ReplicationStartDate"))
                CreatMetaDataFieldsService.CreatMetaDataFields(
                "Erp_CurrentAccount",
                "UD_ReplicationStartDate",
                SLanguage.GetString("Replikasyon Başlangıç"),
                (byte)UdtType.UdtDate,
                (byte)FieldUsage.Date,
                (byte)EditorType.DateEditor,
                (byte)ValueInputMethod.FreeType,
                0
            );

            // 3. Replikasyon bitiş tarihi
            if (!Schema.Tables["Erp_CurrentAccount"].Fields.Contains("UD_ReplicationEndDate"))
                CreatMetaDataFieldsService.CreatMetaDataFields(
                "Erp_CurrentAccount",
                "UD_ReplicationEndDate",
                SLanguage.GetString("Replikasyon Bitiş"),
                (byte)UdtType.UdtDate,
                (byte)FieldUsage.Date,
                (byte)EditorType.DateEditor,
                (byte)ValueInputMethod.FreeType,
                0
            );

            HrmReplicationModuleParameters = liveSession.ParamService.GetParameterClass<HrmReplicationModuleParameters>();
        }

        private void _sysMng_BeforeLogout(object sender, EventArgs e)
        {
        }
    }
}
