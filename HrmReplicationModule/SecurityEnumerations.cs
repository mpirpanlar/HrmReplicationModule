namespace Sentez.HrmReplicationModule
{
    public enum MenuSubRoots : short
    {
        Descriptions = 1000, 
        Transactions, 
        Operations, 
        Reports, 
        Settings 
    }
    public enum HrmReplicationModuleSecurityItems : short
    {
        None,
        VariantItemMark,
        InventoryMark,
        FaultTaskControl,
        MonthlyActualCost,
        OrderAllHistory
    }
    public enum HrmReplicationModuleSecuritySubItems : short
    {
        None
    }
}
