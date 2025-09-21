using DevExpress.XtraRichEdit.Import.Html;

using LiveCore.Desktop.Common;
using LiveCore.Desktop.UI.Controls;

using Prism.Ioc;

using Sentez.Common.PresentationModels;
using Sentez.Common.Utilities;
using Sentez.Core.ParameterClasses;
using Sentez.Data.BusinessObjects;
using Sentez.Data.MetaData;
using Sentez.Data.Tools;
using Sentez.Finance.PresentationModels;
using Sentez.InventoryModule.PresentationModels;
using Sentez.Localization;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Sentez.EgeHayatHrmReplicationModule
{
    public partial class EgeHayatHrmReplicationModule : LiveModule
    {

        private void CurrentAccountBo_Init(BusinessObjectBase bo, BoParam parameter)
        {
            bo.Lookups.AddLookUp("Erp_Address", "AddressId", true, "Erp_Mark", "AddressCode", "AddressCode", "Explanation", "AddressExplanation");
        }

        /// <summary>
        /// Belirtilen parametrelerle geçerli cari hesap sunum modelini Initialize eder.
        /// </summary>
        /// <remarks>
        /// Sağlanan <paramref name="pm"/> nesnesi <see cref="CurrentAccountPM"/> türünde değilse,
        /// metot herhangi bir işlem yapmadan geri döner.
        /// </remarks>
        /// <param name="pm">
        /// Initialize edilecek temel sunum modeli. <see cref="CurrentAccountPM"/> türünde olmalıdır.
        /// </param>
        /// <param name="parameter">
        /// Initialize işlemi için kullanılan ek parametreler.
        /// </param>
        private void CurrentAccountPm_Init(PMBase pm, PmParam parameter)
        {
            currentAccountPm = pm as CurrentAccountPM;
            if (currentAccountPm == null)
                return;
            if (currentAccountPm.ActiveView._view != null)
            {
                LiveDocumentGroup liveDocumentGroup = currentAccountPm.FCtrl("GenelDocumentPanel") as LiveDocumentGroup;
                if (liveDocumentGroup != null)
                {
                    ldpCurrentAccountAttachmentAddress = new LiveDocumentPanel();
                    ldpCurrentAccountAttachmentAddress.Caption = SLanguage.GetString("Adres Ekleri");
                    liveDocumentGroup.Items.Add(ldpCurrentAccountAttachmentAddress);

                    PMDesktop pMDesktop = currentAccountPm.container.Resolve<PMDesktop>();
                    var tsePublicParametersView = pMDesktop.LoadXamlRes("CurrentAccountAttachmentAddressView");
                    (tsePublicParametersView._view as UserControl).DataContext = currentAccountPm;
                    ldpCurrentAccountAttachmentAddress.Content = tsePublicParametersView._view;
                }
                Set_CurrentAccountPm_AddressView_ItemsSource(pm);
                LiveGridControl[] attachmentGridControlGrids = FrameworkTreeHelper.FindLogicalChilds<LiveGridControl>((pm as PMDesktop).ActiveViewControl);
                if (attachmentGridControlGrids?.Length > 0)
                {
                    foreach (LiveGridControl gridControl in attachmentGridControlGrids)
                    {
                        if (gridControl.Name == "AddressView")
                            gridControl.CurrentItemChanged += CurrentAccountPm_AddressView_CurrentItemChanged;
                    }
                }
                //(inventoryPm.ActiveView._view as UserControl).PreviewKeyDown += EgeHayatHrmReplicationModule_CardPm_PreviewKeyDown;
                //currentAccountPm.ActiveBO.AfterSucceededPost += ActiveBO_AfterSucceededPost;
            }
        }

        private static void Set_CurrentAccountPm_AddressView_ItemsSource(PMBase pm)
        {
            LiveAttachment[] attachmentCurrAccArray = FrameworkTreeHelper.FindLogicalChilds<LiveAttachment>((pm as PMDesktop).ActiveViewControl);
            if (attachmentCurrAccArray?.Length > 0)
            {
                foreach (LiveAttachment liveAttachment in attachmentCurrAccArray)
                {
                    if (liveAttachment.Name == "CurrentAccountAttachmentAddressGrid")
                    {
                        LiveGridControl[] gridControls = FrameworkTreeHelper.FindLogicalChilds<LiveGridControl>(liveAttachment);
                        if (gridControls?.Length > 0)
                            foreach (LiveGridControl gridControl in gridControls)
                            {
                                if (gridControl.Name == "AttachmentGridControl")
                                    gridControl.RowFilter = "AddressId is null";
                                gridControl.CreatedNewRow += (s, e) =>
                                {
                                    var table = (pm as PMDesktop).ActiveBO.Data.Tables["Erp_CurrentAccountAttachment"];
                                    if (table == null) return;
                                    var drv = gridControl.CurrentItem as DataRowView;
                                    //if (drv == null || drv.Row.IsNull("RecId")) return;

                                    LiveGridControl[] attachmentGridControlGrids = FrameworkTreeHelper.FindLogicalChilds<LiveGridControl>((pm as PMDesktop).ActiveViewControl);
                                    if (attachmentGridControlGrids?.Length > 0)
                                    {
                                        foreach (LiveGridControl gridControlAddressView in attachmentGridControlGrids)
                                        {
                                            if (gridControlAddressView.Name == "AddressView")
                                            {
                                                if (gridControlAddressView.CurrentItem != null)
                                                {
                                                    var recId = Convert.ToInt64((gridControlAddressView.CurrentItem as DataRowView).Row["RecId"]);
                                                    e.View.Row["AddressId"] = recId;
                                                    var view = new DataView(table)
                                                    {
                                                        RowFilter = $"AddressId = {recId}",
                                                        RowStateFilter = DataViewRowState.CurrentRows
                                                    };
                                                    gridControl.RowFilter = view.RowFilter;
                                                    // Grid’e DataView bağla
                                                    gridControl.ItemsSource = view;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                };
                            }
                    }
                }
            }
        }

        private void CurrentAccountPm_AddressView_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            if (e.NewItem is DataRowView)
            {
                LiveAttachment[] attachmentCurrAccArray = FrameworkTreeHelper.FindLogicalChilds<LiveAttachment>(currentAccountPm.ActiveViewControl);
                if (attachmentCurrAccArray?.Length > 0)
                {
                    foreach (LiveAttachment liveAttachment in attachmentCurrAccArray)
                    {
                        if (liveAttachment.Name == "CurrentAccountAttachmentAddressGrid")
                        {
                            LiveGridControl[] gridControls = FrameworkTreeHelper.FindLogicalChilds<LiveGridControl>(liveAttachment);
                            if (gridControls?.Length > 0)
                                foreach (LiveGridControl gridControl in gridControls)
                                {
                                    if (gridControl.Name == "AttachmentGridControl")
                                    {
                                        var table = currentAccountPm.ActiveBO.Data.Tables["Erp_CurrentAccountAttachment"];
                                        if (table == null) return;

                                        var drv = e.NewItem as DataRowView;
                                        if (drv == null || drv.Row.IsNull("RecId")) return;

                                        var recId = Convert.ToInt64(drv.Row["RecId"]);

                                        // AddressId = seçilen kaydın RecId'si olacak şekilde filtre
                                        var view = new DataView(table)
                                        {
                                            RowFilter = $"AddressId = {recId}",
                                            RowStateFilter = DataViewRowState.CurrentRows
                                        };
                                        gridControl.RowFilter = view.RowFilter;
                                        // Grid’e DataView bağla
                                        gridControl.ItemsSource = view;
                                        break;
                                    }
                                }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event for the current account view.
        /// </summary>
        /// <param name="sender">The source of the event, typically the view that was loaded.</param>
        /// <param name="e">The event data associated with the <see cref="RoutedEventArgs"/>.</param>
        private void CurrentAccountPm_ViewLoaded(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Releases resources associated with the specified account presentation model.
        /// </summary>
        /// <remarks>This method is intended to clean up resources related to the specified presentation
        /// model.  Ensure that the <paramref name="pm"/> parameter is properly initialized before calling this
        /// method.</remarks>
        /// <param name="pm">The presentation model instance to be disposed. Cannot be null.</param>
        /// <param name="parameter">Additional parameters required for the disposal operation. May be null if no parameters are needed.</param>
        private void CurrentAccountPm_Dispose(PMBase pm, PmParam parameter)
        {
            LiveGridControl[] attachmentGridControlGrids = FrameworkTreeHelper.FindLogicalChilds<LiveGridControl>((pm as PMDesktop).ActiveViewControl);
            if (attachmentGridControlGrids?.Length > 0)
            {
                foreach (LiveGridControl gridControl in attachmentGridControlGrids)
                {
                    if (gridControl.Name == "AddressView")
                        gridControl.CurrentItemChanged -= CurrentAccountPm_AddressView_CurrentItemChanged;
                }
            }
        }
    }
}
