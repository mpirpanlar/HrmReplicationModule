using Prism.Ioc;
using Sentez.Common.ModuleBase;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Query;
using Sentez.Data.Tools;
using System.ComponentModel;

namespace Sentez.HrmReplicationModule.Models
{
    [BusinessObjectExplanation("Araç Muayene Kayıtları")]
    public class VehicleInspectionBO : BusinessObjectBase
    {
        public VehicleInspectionBO(IContainerExtension container)
            : base(container, 0, "", "", new string[] { "Erp_VehicleInspection" })
        {
            Lookups.AddLookUp("Erp_VehicleInspection", "VehicleId", true, "Erp_Vehicle", "VehicleCode", "VehicleCode", "VehicleName", "VehicleName");

            ValueFiller.AddRule("Erp_VehicleInspection", "InUse", 1);  //öndeger gelmesi için

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