using Prism.Ioc;
using Sentez.Common.ModuleBase;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Query;
using Sentez.Data.Tools;
using System.ComponentModel;

namespace Sentez.HrmReplicationModule.Models
{
    [BusinessObjectExplanation("Araç Bakım Kayıtları")]
    public class VehicleMaintenanceBO : BusinessObjectBase
    {
        public VehicleMaintenanceBO(IContainerExtension container)
            : base(container, 0, "", "", new string[] { "Erp_VehicleMaintenance" })
        {
            Lookups.AddLookUp("Erp_VehicleMaintenance", "VehicleId", true, "Erp_Vehicle", "VehicleCode", "VehicleCode", "VehicleName", "VehicleName");

            ValueFiller.AddRule("Erp_VehicleMaintenance", "InUse", 1);  //öndeger gelmesi için

            //CodeGenerator.CodeField = "TestCode";
            //CodeGenerator.CodeTable = "Erp_Test";
            //CodeGenerator.TemplateString = string.Empty;
            //CodeGenerator.TemplateString = "########";
            CodeGenerator.Enabled = false;
            SecurityChecker.LogicalModuleID = (short)Modules.ExternalModule20;

        }
        public override void Init(BoParam boParam)
        {
            if (initialized) return;

            initialized = true;
            base.Init(boParam);
        }
    }
}