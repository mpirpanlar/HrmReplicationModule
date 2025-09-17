using LiveCore.Desktop.SBase.MenuManager;
using Sentez.Common;
using Sentez.Common.Commands;
using Sentez.Common.ModuleBase;
using Sentez.Common.ResourceManager;
using Sentez.Common.SystemServices;
using Sentez.Data.MetaData.DatabaseControl;
using System;
using System.IO;
using System.Reflection;
using LiveCore.Desktop.Common;
using Prism.Ioc;
using Sentez.Common.SBase;
using Sentez.Data.BusinessObjects;
using Sentez.Common.PresentationModels;
using Sentez.Common.Report;
using EgeHayatPublicModule.Services;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using Sentez.Core.ParameterClasses;
using System.Windows.Threading;
using System.Windows;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Sentez.EgeHayatPublicModule.PresentationModels;
using Sentez.Parameters;
using Sentez.EgeHayatPublicModule.Parameters;
using Sentez.Data.Tools;
using Sentez.Data.MetaData;
using Sentez.Localization;
using Sentez.EgeHayatPublicModule.Models;
using Sentez.Common.Utilities;
using Sentez.MetaPosModule.ParameterClasses;
using System.Collections.Generic;
using Sentez.PosModule.PresentationModels;
using LiveCore.Desktop.UI.Controls;
using System.Windows.Controls;

namespace Sentez.EgeHayatPublicModule
{
    public partial class EgeHayatPublicModule : LiveModule
    {
        SysMng _sysMng;
        LiveSession liveSession = null;
        EgeHayatPublicModuleParameters EgeHayatPublicModuleParameters;
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

        public override short moduleID { get { return (short)Modules.ExternalModule21; } }

        public EgeHayatPublicModule(IContainerExtension container)
        {
            _container = container;
            _sysMng = _container.Resolve<SysMng>();
            if (_sysMng != null)
            {
                _sysMng.AfterDesktopLogin += _sysMng_AfterDesktopLogin;
                _sysMng.BeforeLogout += _sysMng_BeforeLogout;
            }
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
            EgeHayatPublicModuleSecurity.RegisterSecurityDefinitions();

            //MenuManager.Instance.RegisterMenu("EgeHayatPublicModule", "EgeHayatPublicModuleMenu", moduleID, true);
            ParameterBase.AddInitExternalParameter("InventoryParameters", InitExternalParameter);

            PMBase.AddCustomInit("InventoryParams", InventoryParams_Init);
            PMBase.AddCustomViewLoaded("InventoryParams", InventoryParams_ViewLoaded);
            PMBase.AddCustomDispose("InventoryParams", InventoryParams_Dispose);

            PMBase.AddCustomInit("InventoryPM", InventoryPm_Init);
            PMBase.AddCustomViewLoaded("InventoryPM", InventoryPm_ViewLoaded);
            PMBase.AddCustomDispose("InventoryPM", InventoryPm_Dispose);
        }

        private void InitExternalParameter(Dictionary<int, object> externalParamerters)
        {
            externalParamerters.Add((int)InventoryParameterType.INV_CodeTemplates, "");
            //externalParamerters.Add((int)TseParameterType.TSE_FiskalyIntegrationTypeForTableReceipt, (byte)0);
        }

        public override void OnInitialize(IContainerProvider containerProvider)
        {
            _sysMng.AddApplication("EgeHayatPublicModule");
        }

        public override void RegisterModuleCommands()
        {
        }

        public void Initialize()
        {
        }

        private void RegisterBO()
        {
            _container.Register<IBusinessObject, VehicleAssignmentBO>("VehicleAssignmentBO");
            _container.Register<IBusinessObject, VehicleInspectionBO>("VehicleInspectionBO");
            _container.Register<IBusinessObject, VehicleMaintenanceBO>("VehicleMaintenanceBO");
        }

        private void RegisterServices()
        {
            ParameterFactory.StaticFactory.RegisterParameterClass(typeof(EgeHayatPublicModuleParameters), (int)Modules.ExternalModule21);
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
            ResMng.AddRes("EgeHayatPublicModuleMenu", "EgeHayatPublicModule;component/ModuleMenu.xml", ResSource.Resource, ResourceType.MenuXml, Modules.ExternalModule21, 0, 0);
        }

        private void RegisterList()
        {
            //_container.Register<IReport, CrmActivityTypeList>("Crm_ActivityTypeTypeNameList");
            //_container.Register<IReport, CrmActivityChecklistItemList>("Crm_ActivityChecklistItemChecklistTitleList");
        }

        private void RegisterViews()
        {
            ResMng.AddRes("EgeHayatPublicModuleParametersView", "EgeHayatPublicModule;component/Views/EgeHayatPublicModuleParameters.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule21, 0, 0);
            ResMng.AddRes("VehicleAssignmentView", "EgeHayatPublicModule;component/Views/VehicleAssignment.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule21, 0, 0);
            ResMng.AddRes("VehicleInspectionView", "EgeHayatPublicModule;component/Views/VehicleInspection.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule21, 0, 0);
            ResMng.AddRes("VehicleMaintenanceView", "EgeHayatPublicModule;component/Views/VehicleMaintenance.xaml", ResSource.Resource, ResourceType.View, Modules.ExternalModule21, 0, 0);
        }

        private void RegisterPM()
        {
            _container.Register<IPMBase, EgeHayatPublicModuleParametersPM>("EgeHayatPublicModuleParametersPM");
        }

        private void RegisterRpr()
        {
            //_container.Register<IReport, SalesShipmentComparePolicy>("SalesShipmentComparePolicy");
        }

        public void RegisterCoreDocuments()
        {
            //Schema.ReadXml(Assembly.GetAssembly(typeof(EgeHayatPublicModule)).GetManifestResourceStream("EgeHayatPublicModule.EgeHayatPublicModuleDataSchema.xml"));
            //DbCreator.AddRegistration(3014, EgeHayatPublicModuleDbUpdateScript);
        }

        DbScripts EgeHayatPublicModuleDbUpdateScript(DbCreator instance)
        {
            return DbScripts.LoadFromAssembly(Assembly.GetAssembly(typeof(EgeHayatPublicModule)), "EgeHayatPublicModule.EgeHayatPublicModuleDbUpdateScripts.xml");
        }

        private CancellationTokenSource bilgeceBoomerangCts;
        private static readonly object bilgeceBoomerangLockKey = new object();

        private void _sysMng_AfterDesktopLogin(object sender, EventArgs e)
        {
            liveSession = _sysMng.getSession();

            if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_SubGroup"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_SubGroup", SLanguage.GetString("Alt Grup"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_Property"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_Property", SLanguage.GetString("Özellik"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            if (!Schema.Tables["Erp_Inventory"].Fields.Contains("UD_Weight"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Inventory", "UD_Weight", SLanguage.GetString("Ağırlık"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);

            if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_SubGroup"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_SubGroup", SLanguage.GetString("Alt Grup"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_Property"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_Property", SLanguage.GetString("Özellik"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_Weight"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_Weight", SLanguage.GetString("Ağırlık"), (byte)UdtType.UdtCode, (byte)FieldUsage.Code, (byte)EditorType.ListSelector, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);
            if (!Schema.Tables["Erp_Service"].Fields.Contains("UD_InventoryId"))
                CreatMetaDataFieldsService.CreatMetaDataFields("Erp_Service", "UD_InventoryId", SLanguage.GetString("Malzeme ID"), (byte)UdtType.UdtInt64, (byte)FieldUsage.None, (byte)EditorType.ReadOnlyTextEditor, (byte)ValueInputMethod.FreeTypeAndAddToList, 0);

            EgeHayatPublicModuleParameters = liveSession.ParamService.GetParameterClass<EgeHayatPublicModuleParameters>();
        }

        private void _sysMng_BeforeLogout(object sender, EventArgs e)
        {
        }
    }
}
