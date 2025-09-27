using LiveCore.Desktop.Common;
using LiveCore.Desktop.UI.Controls;
using Sentez.Common.Commands;
using Sentez.Common.PresentationModels;

using Sentez.Common.Utilities;
using Sentez.Data.Tools;

using Sentez.Localization;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Sentez.HrmReplicationModule
{
    public partial class HrmReplicationModule : LiveModule
    {
        DataTable InventoryTemplateTable = null;
        private void InventoryParams_Init(PMBase pm, PmParam parameter)
        {
            cardPm = pm as CardPM;
            if (cardPm == null)
                return;
            cardPm.PropertyChanged += CardPm_PropertyChanged;
            cardPm.CmdList.AddCmd(317, "CardPmSaveCommand", SLanguage.GetString("Ege Hayat Parametre Kaydet"), OnCardPmSaveCommand, null);
            if (cardPm.ActiveView._view != null)
            {
                (cardPm.ActiveView._view as UserControl).PreviewKeyDown += HrmReplicationModule_CardPm_PreviewKeyDown;
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event for the inventory parameters view.
        /// </summary>
        /// <remarks>This method initializes and configures the UI elements for the "Kod Üretici" tab,
        /// including adding a new row to the main grid, creating a <see cref="GroupBox"/> with a <see
        /// cref="LiveGridControl"/> for displaying inventory template data, and binding the data to a <see
        /// cref="DataTable"/>. It also updates the context menu commands for the card parameter manager.</remarks>
        /// <param name="sender">The source of the event, typically the view being loaded.</param>
        /// <param name="e">The event data associated with the <see cref="FrameworkElement.Loaded"/> event.</param>
        private void InventoryParams_ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (cardPm == null)
                return;
            LiveTabControl tabControl = cardPm.FCtrl("TabGenel") as LiveTabControl;
            foreach (var item in tabControl.Items)
            {
                if ((item as LiveTabItem).Header.ToString() == SLanguage.GetString("Kod Üretici"))
                {
                    // Ana Grid'e erişelim (örnek olarak "MainGrid" adını kullanıyorum)
                    Grid[] grids = FrameworkTreeHelper.FindLogicalChilds<Grid>((item as LiveTabItem));
                    if (grids?.Length > 0)
                    {
                        Grid mainGrid = grids[0]; // Veya uygun referans

                        // "*" satırından önce yeni bir satır ekle (örneğin 3. sıraya)
                        int insertIndex = 3;

                        // Yeni satır tanımı
                        RowDefinition newRow = new RowDefinition { Height = GridLength.Auto };
                        mainGrid.RowDefinitions.Insert(insertIndex, newRow);

                        // Yeni GroupBox oluştur
                        GroupBox groupBox = new GroupBox
                        {
                            Header = SLanguage.GetString("Kod Üretici Şablon Tanımları"),
                            Margin = new Thickness(5)
                        };
                        InventoryTemplateTable = new DataTable();
                        InventoryTemplateTable.TableName = "InventoryTemplateTable";
                        InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "TemplateExplanation", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
                        InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "TemplateDetail", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
                        InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "IsServiceTemplate", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtBool), DefaultValue = false });
                        if (!string.IsNullOrEmpty(cardPm.InventoryParam.ExternalParameters[5001].ToString()))
                        {
                            try
                            {
                                if (InventoryTemplateTable != null)
                                    InventoryTemplateTable.Rows.Clear();
                                DataSet DsInventoryPriceTable = new DataSet();
                                StringReader sr = new StringReader(cardPm.InventoryParam.ExternalParameters[5001].ToString());
                                DsInventoryPriceTable.ReadXml(sr);
                                DataTable dt = DsInventoryPriceTable.Tables["InventoryTemplateTable"];
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        DataRow newTemplateRow = InventoryTemplateTable.NewRow();
                                        foreach (DataColumn dc in dr.Table.Columns)
                                        {
                                            if (InventoryTemplateTable.Columns.Contains(dc.ColumnName))
                                            {
                                                newTemplateRow[dc.ColumnName] = dr[dc.ColumnName];
                                            }
                                        }
                                        InventoryTemplateTable.Rows.Add(newTemplateRow);
                                    }
                                }
                            }
                            catch
                            {
                                InventoryTemplateTable.Rows.Clear();
                            }
                        }
                        else
                        {
                            if (InventoryTemplateTable != null)
                                InventoryTemplateTable.Rows.Clear();
                        }
                        // LiveGridControl oluştur
                        var liveGrid = new LiveGridControl
                        {
                            Height = 300,
                            ItemsSource = InventoryTemplateTable.DefaultView, // DataTable objenizle bağlayın
                            Lookups = this.Lists, // Lookup varsa
                            EnableSaveLayout = true
                        };
                        liveGrid.ColumnDefinitions.Add(new ReceiptColumn() { Caption = SLanguage.GetString("Açıklama"), ColumnName = "TemplateExplanation", Width = 300, DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
                        liveGrid.ColumnDefinitions.Add(new ReceiptColumn() { Caption = SLanguage.GetString("Şablon Tanımı"), ColumnName = "TemplateDetail", Width = 300, DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
                        liveGrid.ColumnDefinitions.Add(new ReceiptColumn() { Caption = SLanguage.GetString("Hizmet Şablonu"), ColumnName = "IsServiceTemplate", Width = 80, DataType = UdtTypes.GetUdtSystemType(UdtType.UdtBool), EditorType = Data.MetaData.EditorType.CheckBox });
                        // Gerekirse özel görünüm ayarlayın
                        liveGrid.View = new ReceiptView();

                        // GroupBox içine ekle
                        groupBox.Content = liveGrid;

                        // GroupBox'ı Gridden doğru satıra yerleştir
                        Grid.SetRow(groupBox, insertIndex);
                        mainGrid.Children.Add(groupBox);
                    }
                }
            }
            foreach (System.Windows.Controls.MenuItem itm in cardPm.contextMenu.Items)
            {
                if (itm.Command != null)
                {
                    if (itm.Command is ISysCommand)
                    {
                        if ((itm.Command as ISysCommand).Name == "ParameterPostCommand")
                        {
                            itm.Command = cardPm.CmdList["CardPmSaveCommand"];
                            break;
                        }
                    }
                }
            }
        }

        private void OnCardPmSaveCommand(ISysCommandParam param)
        {
            SetInventoryParamsValue();
            cardPm.OnParameterPostCommand(null);
        }

        private void CardPm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InventoryParams")
            {

            }
        }

        private void HrmReplicationModule_CardPm_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                SetInventoryParamsValue();
            }
        }

        /// <summary>
        /// Inventory Parametrelerini ExternalParameters içine atar
        /// </summary>
        private void SetInventoryParamsValue()
        {
            DataSet ds = new DataSet();  // eski kayıtlar dataset olarak tutulduğu için 
            ds.Tables.Add(InventoryTemplateTable);
            StringWriter sw = new StringWriter();
            ds.WriteXml(sw);
            cardPm.InventoryParam.ExternalParameters[5001] = sw.ToString();
            ds.Tables.Remove(InventoryTemplateTable);
            ds.Dispose();
        }

        private void InventoryParams_Dispose(PMBase pm, PmParam parameter)
        {
            cardPm = pm as CardPM;
            if (cardPm == null)
                return;
            cardPm.PropertyChanged -= CardPm_PropertyChanged;
            if (cardPm.ActiveView._view != null)
            {
                (cardPm.ActiveView._view as UserControl).PreviewKeyDown -= HrmReplicationModule_CardPm_PreviewKeyDown;
            }
        }
    }
}
