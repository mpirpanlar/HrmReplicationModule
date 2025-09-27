using Prism.Ioc;

using Sentez.Common.ModuleBase;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Query;
using Sentez.Data.Tools;

using System.ComponentModel;

namespace Sentez.HrmReplicationModule.Models
{
    [BusinessObjectExplanation("Araç Görev Atamaları")]
    public class VehicleAssignmentBO : BusinessObjectBase
    {
        public VehicleAssignmentBO(IContainerExtension container)
            : base(container, 0, "", "", new string[] { "Erp_VehicleAssignment" })
        {
            Lookups.AddLookUp("Erp_VehicleAssignment", "VehicleId", true, "Erp_Vehicle", "VehicleCode", "VehicleCode", "VehicleName", "VehicleName");
            Lookups.AddLookUp("Erp_VehicleAssignment", "CurrentAccountId", true, "Erp_CurrentAccount", "CurrentAccountCode", "CurrentAccountCode", new string[] { "TradeName", "CurrentAccountName", "InUse", "RiskLimit", "IsPotential", "TaxOfficeId", "TaxNo", "ForexRateId", "IsEInvoice", "IsBlackList", "EArchivesShippingType", "IsEDespatch", "GroupId", "EInvoiceXsltName", "EArchiveXsltName", "EProducerReceiptXsltName", "EGuestCheckXsltName", "EDespatchXsltName" }, new string[] { "CurrentAccountTradeName", "CurrentAccountName", "CurrentAccountInUse", "CurrentAccountRiskLimit", "CurrentAccountIsPotential", "CurrentAccountTaxOfficeId", "CurrentAccountTaxNo", "CurrentAccountForexRateId", "CurrentAccountIsEInvoice", "CurrentAccountIsBlackList", "CurrentAccountEArchivesShippingType", "CurrentAccountIsEDespatch", "CurrentAccountGroupId", "CurrentAccountEInvoiceXsltName", "CurrentAccountEArchiveXsltName", "CurrentAccountEProducerReceiptXsltName", "CurrentAccountEGuestCheckXsltName", "CurrentAccountEDespatchXsltName" });
            Lookups.AddLookUp("Erp_VehicleAssignment", "EmployeeId", true, "Erp_Employee", "EmployeeCode", "EmployeeCode", new string[] { "EmployeeName", "InUse" }, new string[] { "EmployeeName", "EmployeeInUse" });

            ValueFiller.AddRule("Erp_VehicleAssignment", "InUse", 1);  //öndeger gelmesi için

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