using DevExpress.Xpf.Core;

using EgeHayatHrmReplicationModule.Services;

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
using Sentez.EgeHayatHrmReplicationModule.Models;
using Sentez.EgeHayatHrmReplicationModule.Parameters;
using Sentez.EgeHayatHrmReplicationModule.PresentationModels;
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

namespace Sentez.EgeHayatHrmReplicationModule
{
    public partial class EgeHayatHrmReplicationModule : LiveModule
    {
        SysMng _sysMng;
        LiveSession liveSession = null;
        ISystemService _createBulkUpdateTaskService;
        ISysCommand hrmTimeBulkUpdateCommand;

        EgeHayatHrmReplicationModuleParameters EgeHayatHrmReplicationModuleParameters;

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

        public EgeHayatHrmReplicationModule(IContainerExtension container)
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
            EgeHayatHrmReplicationModuleSecurity.RegisterSecurityDefinitions();

            //MenuManager.Instance.RegisterMenu("EgeHayatHrmReplicationModule", "EgeHayatHrmReplicationModuleMenu", moduleID, true);
            ParameterBase.AddInitExternalParameter("InventoryParameters", InitExternalParameter);

            PMBase.AddCustomInit("InventoryParams", InventoryParams_Init);
            PMBase.AddCustomViewLoaded("InventoryParams", InventoryParams_ViewLoaded);
            PMBase.AddCustomDispose("InventoryParams", InventoryParams_Dispose);

            PMBase.AddCustomInit("InventoryPM", InventoryPm_Init);
            PMBase.AddCustomViewLoaded("InventoryPM", InventoryPm_ViewLoaded);
            PMBase.AddCustomDispose("InventoryPM", InventoryPm_Dispose);

            BusinessObjectBase.AddCustomInit("CurrentAccountBO", CurrentAccountBo_Init);

            PMBase.AddCustomInit("CurrentAccountPM", CurrentAccountPm_Init);
            PMBase.AddCustomViewLoaded("CurrentAccountPM", CurrentAccountPm_ViewLoaded);
            PMBase.AddCustomDispose("CurrentAccountPM", CurrentAccountPm_Dispose);

            var ort = new ReceiptTypeDefinition
            {
                Type = 701,
                TypeName = SLanguage.GetString("IK-Mesai Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMTimeBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);

            ort = new ReceiptTypeDefinition
            {
                Type = 702,
                TypeName = SLanguage.GetString("IK-Ek Ödeme-Ek Kesinti Kartları"),
                Module = (short)Modules.HRMModule,
                BoName = "HRMAddPaymentDeductionBO"
            };
            WorkFlowModuleType.WorkFlowModuleTypes.Add(ort.Type, ort);
        }

        private void RegisterServiceCommands()
        {
            var prm = ActiveSession.ParamService.GetParameterClass("ReplicationParameters");
            if (prm.GetValue<int>("ReplicationEnabled") != 1)
                return;

            _createBulkUpdateTaskService = ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            hrmTimeBulkUpdateCommand = SysMng.Instance.RegisterCmd(moduleID, 144, "HrmTimeBulkUpdateCommand", SLanguage.GetString("Uzak Noktalara Gönder"), "HRMTimeBO", OnBulkUpdateCommand, CanBulkUpdateCommand);
            AddExtraContextItems("Hrm_TimeTimeCodeList", hrmTimeBulkUpdateCommand);
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
            _sysMng.AddApplication("EgeHayatHrmReplicationModule");
        }

        public override void RegisterModuleCommands()
        {
        }

        public void Initialize()
        {
        }

        private void RegisterBO()
        {
            //_container.Register<IBusinessObject, VehicleAssignmentBO>("VehicleAssignmentBO");
            //_container.Register<IBusinessObject, VehicleInspectionBO>("VehicleInspectionBO");
            //_container.Register<IBusinessObject, VehicleMaintenanceBO>("VehicleMaintenanceBO");
        }

        private void RegisterServices()
        {
            ParameterFactory.StaticFactory.RegisterParameterClass(typeof(EgeHayatHrmReplicationModuleParameters), (int)Modules.ExternalModule22);
            _container.Register<ISystemService, CreatMetaDataFieldsService>("CreatMetaDataFieldsService");
            //BusinessObjectBase.AddCustomExtension("OrderReceiptBO", typeof(OrderReceiptControlExtension));

            //BusinessObjectBase.AddCustomConstruction("CurrentAccountBO", CurrentAccountBoCustomCons);
            //BusinessObjectBase.AddCustomInit("CurrentAccountBO", CurrentAccountBo_Init);

            //BusinessObjectBase.AddCustomConstruction("CRMCustomerTransactionBO", CrmCustomerTransactionBoCustomCons);
            //BusinessObjectBase.AddCustomInit("CRMCustomerTransactionBO", CrmCustomerTransactionBo_Init);

            //PMBase.AddCustomInit("CurrentAccountPM", CurrentAccountPm_Init);
            //PMBase.AddCustomInit("CRMCustomerTransactionPM", CrmCustomerTransactionPm_Init);
        }

        private void RegisterRes()
        {
            ResMng.AddRes("EgeHayatHrmReplicationModuleMenu", "EgeHayatHrmReplicationModule;component/ModuleMenu.xml", ResSource.Resource, ResourceType.MenuXml, Modules.ExternalModule22, 0, 0);
        }

        private void RegisterList()
        {
            //_container.Register<IReport, CrmActivityTypeList>("Crm_ActivityTypeTypeNameList");
            //_container.Register<IReport, CrmActivityChecklistItemList>("Crm_ActivityChecklistItemChecklistTitleList");
        }

        private void RegisterViews()
        {
            ResMng.AddRes("EgeHayatHrmReplicationModuleParametersView", "EgeHayatHrmReplicationModule;component/Views/EgeHayatHrmReplicationModuleParameters.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
            //ResMng.AddRes("VehicleAssignmentView", "EgeHayatHrmReplicationModule;component/Views/VehicleAssignment.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
            //ResMng.AddRes("VehicleInspectionView", "EgeHayatHrmReplicationModule;component/Views/VehicleInspection.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
            //ResMng.AddRes("VehicleMaintenanceView", "EgeHayatHrmReplicationModule;component/Views/VehicleMaintenance.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
            //ResMng.AddRes("CurrentAccountAttachmentAddressView", "EgeHayatHrmReplicationModule;component/Views/CurrentAccountAttachmentAddress.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule22, 0, 0);
        }

        private void RegisterPM()
        {
            _container.Register<IPMBase, EgeHayatHrmReplicationModuleParametersPM>("EgeHayatHrmReplicationModuleParametersPM");
        }

        private void RegisterRpr()
        {
            //_container.Register<IReport, SalesShipmentComparePolicy>("SalesShipmentComparePolicy");
        }

        public void RegisterCoreDocuments()
        {
            Schema.ReadXml(Assembly.GetAssembly(typeof(EgeHayatHrmReplicationModule)).GetManifestResourceStream("EgeHayatHrmReplicationModule.EgeHayatHrmReplicationModuleDataSchema.xml"));
            DbCreator.AddRegistration(3021, EgeHayatHrmReplicationModuleDbUpdateScript);
        }

        DbScripts EgeHayatHrmReplicationModuleDbUpdateScript(DbCreator instance)
        {
            return DbScripts.LoadFromAssembly(Assembly.GetAssembly(typeof(EgeHayatHrmReplicationModule)), "EgeHayatHrmReplicationModule.EgeHayatHrmReplicationModuleDbUpdateScripts.xml");
        }

        private CancellationTokenSource bilgeceBoomerangCts;
        private static readonly object bilgeceBoomerangLockKey = new object();

        private void _sysMng_AfterDesktopLogin(object sender, EventArgs e)
        {
            liveSession = _sysMng.getSession();

            //if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_SubGroup"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_SubGroup", SLanguage.GetString("Alt Grup"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            //if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_Property"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_Property", SLanguage.GetString("Özellik"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            //if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_Weight"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_Weight", SLanguage.GetString("Ağırlık"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);

            //if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_SubGroup"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_SubGroup", SLanguage.GetString("Alt Grup"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            //if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_Property"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_Property", SLanguage.GetString("Özellik"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            //if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_Weight"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_Weight", SLanguage.GetString("Ağırlık"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            //if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_InventoryId"))
            //    CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_InventoryId", SLanguage.GetString("Malzeme ID"), (byte)UdtType.UdtInt64, (byte)FieldUsage.None, (byte)EditorType.ReadOnlyTextEditor, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);

            EgeHayatHrmReplicationModuleParameters = liveSession.ParamService.GetParameterClass<EgeHayatHrmReplicationModuleParameters>();
        }

        private void _sysMng_BeforeLogout(object sender, EventArgs e)
        {
        }
    }
}
