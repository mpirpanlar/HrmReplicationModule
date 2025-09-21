using Sentez.Common.ModuleBase;
using Sentez.Common.Security;
using Sentez.Localization;

namespace Sentez.EgeHayatHrmReplicationModule
{
    class EgeHayatHrmReplicationModuleSecurity
    {
        public static void RegisterSecurityDefinitions()
        {
            short _moduleId = (short)Modules.ExternalModule15;

            SecurityDefinition mainSecurity = new SecurityDefinition(SLanguage.GetString("Maliyet Kontrol Modülü"), _moduleId, _moduleId, 0, 0, Privileges.Select);
            mainSecurity.AddChild(new SecurityDefinition(SLanguage.GetString("Satış-Sevkiyat Karşılaştırması"), _moduleId, _moduleId, (short)EgeHayatHrmReplicationModuleSecurityItems.VariantItemMark, (short)EgeHayatHrmReplicationModuleSecuritySubItems.None, Privileges.Select));
            mainSecurity.AddChild(new SecurityDefinition(SLanguage.GetString("Hata Kontrol Mekanizması"), _moduleId, _moduleId, (short)EgeHayatHrmReplicationModuleSecurityItems.InventoryMark, (short)EgeHayatHrmReplicationModuleSecuritySubItems.None, Privileges.Select));
            mainSecurity.AddChild(new SecurityDefinition(SLanguage.GetString("Hata Görev Kontrolü"), _moduleId, _moduleId, (short)EgeHayatHrmReplicationModuleSecurityItems.FaultTaskControl, (short)EgeHayatHrmReplicationModuleSecuritySubItems.None, Privileges.All));
            mainSecurity.AddChild(new SecurityDefinition(SLanguage.GetString("Aylık Gerçek Maliyet"), _moduleId, _moduleId, (short)EgeHayatHrmReplicationModuleSecurityItems.MonthlyActualCost, (short)EgeHayatHrmReplicationModuleSecuritySubItems.None, Privileges.All));
            mainSecurity.AddChild(new SecurityDefinition(SLanguage.GetString("Order Tarihçesi"), _moduleId, _moduleId, (short)EgeHayatHrmReplicationModuleSecurityItems.OrderAllHistory, (short)EgeHayatHrmReplicationModuleSecuritySubItems.None, Privileges.All));

            PrivilegeInfo.SecurityDefinitions.AddDefinition(mainSecurity);
        }
    }

}
