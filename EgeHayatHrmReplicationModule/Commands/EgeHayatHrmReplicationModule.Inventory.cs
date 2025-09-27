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
        private void InventoryPm_Init(PMBase pm, PmParam parameter)
        {
            inventoryPm = pm as InventoryPM;
            if (inventoryPm == null)
                return;
            if (inventoryPm.ActiveView._view != null)
            {
                //(inventoryPm.ActiveView._view as UserControl).PreviewKeyDown += EgeHayatHrmReplicationModule_CardPm_PreviewKeyDown;
                //inventoryPm.ActiveBO.AfterSucceededPost += ActiveBO_AfterSucceededPost;
            }
        }

        private void InventoryPm_ViewLoaded(object sender, RoutedEventArgs e)
        {
            if (inventoryPm == null)
                return;

            DataTable InventoryTemplateTable = new DataTable();
            InventoryTemplateTable.TableName = "InventoryTemplateTable";
            InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "TemplateExplanation", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
            InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "TemplateDetail", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtText) });
            InventoryTemplateTable.Columns.Add(new DataColumn() { ColumnName = "IsServiceTemplate", DataType = UdtTypes.GetUdtSystemType(UdtType.UdtBool), DefaultValue = false });
            if (!string.IsNullOrEmpty(inventoryPm.ActiveSession.ParamService.GetParameterClass<InventoryParameters>().ExternalParameters[5001].ToString()))
            {
                try
                {
                    if (InventoryTemplateTable != null)
                        InventoryTemplateTable.Rows.Clear();
                    DataSet DsInventoryPriceTable = new DataSet();
                    StringReader sr = new StringReader(inventoryPm.ActiveSession.ParamService.GetParameterClass<InventoryParameters>().ExternalParameters[5001].ToString());
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
            inventoryPm.Lists.AddLookupList("InventoryTemplateList", "Display", typeof(string), new object[] { SLanguage.GetString("Devre Dışı"), SLanguage.GetString("Ekle") }, "Value", typeof(byte), new object[] { 1, 2 });
            LiveLayoutGroup ldpInventoryGeneral = inventoryPm.FCtrl("LlgMain") as LiveLayoutGroup;
            if (ldpInventoryGeneral != null)
            {
                // Grid'e erişim (ScrollViewer içindeki Grid olduğunu varsayıyoruz)
                LiveLayoutPanel[] liveLayoutPanels = FrameworkTreeHelper.FindLogicalChilds<LiveLayoutPanel>((ldpInventoryGeneral));
                if (liveLayoutPanels?.Length > 0)
                {
                    LiveLayoutPanel liveLayoutPanel = liveLayoutPanels.Where(x => x.Name == "KeyFieldPanel").FirstOrDefault();
                    if (liveLayoutPanel != null)
                    {
                        Grid[] grids = FrameworkTreeHelper.FindLogicalChilds<Grid>((liveLayoutPanel));
                        if (grids?.Length > 0)
                        {
                            Grid grid = grids[0];
                            // "*" satırının öncesine eklenecek sıra (yani son satırdan önce)
                            int insertIndex = grid.RowDefinitions.Count - 1;

                            // Yeni satır tanımı ekle
                            RowDefinition newRow = new RowDefinition { Height = GridLength.Auto };
                            grid.RowDefinitions.Insert(insertIndex, newRow);

                            // Label oluştur
                            var label = new LiveLabel
                            {
                                Content = SLanguage.GetString("Otomatik Hizmet Kartı"), // runtime'da binding gerekiyorsa Translate çağrısı kullanılmalı
                                HorizontalContentAlignment = HorizontalAlignment.Right,
                                FontWeight = FontWeights.Bold
                            };
                            Grid.SetRow(label, insertIndex);
                            Grid.SetColumn(label, 0);
                            grid.Children.Add(label);

                            // LiveCheckedComboBoxEdit oluştur
                            liveCheckedComboBoxEditInventoryTemplate = new LiveCheckedComboBoxEdit
                            {
                                Name = "lcbeInventoryTemplateList",
                                Margin = new Thickness(2, 1, 1, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                //ItemsSource = inventoryPm.Lists["InventoryTemplateList"],
                                ItemsSource = InventoryTemplateTable.DefaultView,
                                DisplayMember = "TemplateExplanation",
                                ValueMember = "TemplateDetail",
                                Width = 200
                                //EditValue = this.DataContext is YourViewModel vm
                                //    ? vm.mrpOptions.SelectedInventoryTypes
                                //    : null // veya başka binding yöntemi
                            };
                            liveCheckedComboBoxEditInventoryTemplate.EditValueChanging += LiveCheckedComboBoxEditInventoryTemplate_EditValueChanging;
                            Grid.SetRow(liveCheckedComboBoxEditInventoryTemplate, insertIndex);
                            Grid.SetColumn(liveCheckedComboBoxEditInventoryTemplate, 1);
                            Grid.SetColumnSpan(liveCheckedComboBoxEditInventoryTemplate, 2);
                            grid.Children.Add(liveCheckedComboBoxEditInventoryTemplate);
                        }
                    }
                }
            }
        }

        private void LiveCheckedComboBoxEditInventoryTemplate_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            if (e.NewValue != null)
            {
                if (!inventoryPm.ActiveBO.IsNewRecord && !inventoryPm.ActiveBO.HasDataChanges)
                    inventoryPm.ActiveBO.CurrentRow["RecId"] = inventoryPm.ActiveBO.CurrentRow["RecId"];
            }
        }

        private void InventoryPm_Dispose(PMBase pm, PmParam parameter)
        {
            if (inventoryPm != null)
            {
                //inventoryPm.ActiveBO.AfterSucceededPost -= ActiveBO_AfterSucceededPost;
                LiveLayoutGroup ldpInventoryGeneral = inventoryPm.FCtrl("LlgMain") as LiveLayoutGroup;
                if (ldpInventoryGeneral != null)
                {
                    LiveCheckedComboBoxEdit[] liveCheckedComboBoxEdits = FrameworkTreeHelper.FindLogicalChilds<LiveCheckedComboBoxEdit>(ldpInventoryGeneral);
                    if (liveCheckedComboBoxEdits != null && liveCheckedComboBoxEdits.Length > 0)
                    {
                        foreach (LiveCheckedComboBoxEdit checkedComboBoxEdit in liveCheckedComboBoxEdits)
                        {
                            if (checkedComboBoxEdit.Name == "lcbeInventoryTemplateList")
                            {
                                checkedComboBoxEdit.EditValueChanging -= LiveCheckedComboBoxEditInventoryTemplate_EditValueChanging;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
