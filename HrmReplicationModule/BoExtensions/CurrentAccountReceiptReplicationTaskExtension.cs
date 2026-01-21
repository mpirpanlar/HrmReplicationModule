using DevExpress.Xpf.Core;

using Prism.Ioc;

using Sentez.Common;
using Sentez.Common.Commands;
using Sentez.Common.SBase;
using Sentez.Common.SystemServices;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Tools;
using Sentez.Localization;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace Sentez.HrmReplicationModule.BoExtensions
{
    public class CurrentAccountReceiptReplicationTaskExtension : BoExtensionBase
    {
        ISystemService _createBulkUpdateTaskService;
        public CurrentAccountReceiptReplicationTaskExtension(BusinessObjectBase bo)
            : base(bo)
        {
        }

        protected override void SetBusinessObject(BusinessObjectBase businessObject)
        {
            base.SetBusinessObject(businessObject);
            if (BusinessObject == null)
                return;
        }

        protected override void OnColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            base.OnColumnChanged(sender, e);
            if (!Enabled || _suppressEvents)
                return;

            // Detay tablosunda (Erp_CurrentAccountReceiptItem) CurrentAccountId değiştiğinde
            if (e.Column != null && e.Column.ColumnName == "CurrentAccountId")
            {
                // Değişen satırın hangi tabloda olduğunu kontrol et
                if (e.Row != null && e.Row.Table != null && e.Row.Table.TableName == "Erp_CurrentAccountReceiptItem")
                {
                    // Cari kartından detay satırına kopyala
                    CopyReplicationFieldsFromCardToDetailItem(e.Row);
                    // Detay satırından başlığa kopyala
                    CopyReplicationFieldsFromDetailToHeader();
                }
            }
        }

        protected override void OnBeforePost(object sender, CancelEventArgs e)
        {
            if (!Enabled || _suppressEvents)
                return;
            base.OnBeforePost(sender, e);

            // Birden fazla farklı cari seçilmemesi kontrolü
            if (!ValidateSingleCurrentAccount(e))
                return;

            // Kayıt öncesi detay satırlarından başlığa kopyalama yap
            if (BusinessObject.CurrentRow?.Row != null)
            {
                CopyReplicationFieldsFromDetailToHeader();
            }
        }

        protected override void OnAfterSucceededPost(object sender, EventArgs e)
        {
            base.OnAfterSucceededPost(sender, e);

            bool isOk = true;

            // Evrak (cari hesap fişi) tablosundaki alanlardan kontrol yap
            DataRow currentAccountReceiptRow = BusinessObject.CurrentRow?.Row;
            if (currentAccountReceiptRow == null)
                return;

            DateTime receiptDate;
            if (!DateTime.TryParse(currentAccountReceiptRow["ReceiptDate"]?.ToString(), out receiptDate))
                isOk = false;

            if (isOk)
            {
                // Replikasyon aktif mi kontrolü (evrak tablosundan)
                bool isReplicable = false;
                if (currentAccountReceiptRow.Table.Columns.Contains("UD_IsTransactionReplicable") &&
                    currentAccountReceiptRow["UD_IsTransactionReplicable"] != DBNull.Value)
                {
                    isReplicable = Convert.ToBoolean(currentAccountReceiptRow["UD_IsTransactionReplicable"]);
                }

                if (!isReplicable)
                    isOk = false;
            }

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (isOk && currentAccountReceiptRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                startDate = currentAccountReceiptRow["UD_ReplicationStartDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(currentAccountReceiptRow["UD_ReplicationStartDate"]);

                if (startDate.HasValue && receiptDate < startDate.Value)
                    isOk = false;
            }

            if (isOk && currentAccountReceiptRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                endDate = currentAccountReceiptRow["UD_ReplicationEndDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(currentAccountReceiptRow["UD_ReplicationEndDate"]);

                if (endDate.HasValue && receiptDate > endDate.Value)
                    isOk = false;
            }

            if (isOk)
            {
                System.Collections.ArrayList prm = new System.Collections.ArrayList
                {
                    Convert.ToInt64(currentAccountReceiptRow["RecId"].ToString())
                };
                CreateBulkUpdateTask(BusinessObject.Name, prm.ToArray(), true);
            }
        }

        /// <summary>
        /// Birden fazla farklı cari seçilmemesi kontrolü yapar
        /// Sadece 3-Gelen Havaleler ve 4-Gönderilen Havaleler fişleri için geçerlidir
        /// </summary>
        private bool ValidateSingleCurrentAccount(CancelEventArgs e)
        {
            DataRow headerRow = BusinessObject.CurrentRow?.Row;
            if (headerRow == null)
                return true;

            // Fiş tipi kontrolü - sadece 3 (Gelen Havaleler) ve 4 (Gönderilen Havaleler) için kontrol yap
            if (headerRow.Table.Columns.Contains("ReceiptType"))
            {
                object receiptTypeObj = headerRow["ReceiptType"];
                if (receiptTypeObj != DBNull.Value && receiptTypeObj != null)
                {
                    int receiptType;
                    if (int.TryParse(receiptTypeObj.ToString(), out receiptType))
                    {
                        // Sadece 3 ve 4 tipindeki fişler için kontrol yap
                        if (receiptType != 3 && receiptType != 4)
                            return true; // Diğer fiş tipleri için kontrol yapma
                    }
                }
            }

            DataTable detailTable = BusinessObject.Data?.Tables["Erp_CurrentAccountReceiptItem"];
            if (detailTable == null)
                return true;

            // Silinmemiş ve CurrentAccountId dolu olan satırları filtrele
            var distinctCurrentAccountIds = detailTable.Rows.Cast<DataRow>()
                .Where(row => row.RowState != DataRowState.Deleted && 
                             row["CurrentAccountId"] != DBNull.Value && 
                             !string.IsNullOrWhiteSpace(row["CurrentAccountId"]?.ToString()))
                .Select(row => row["CurrentAccountId"].ToString())
                .Distinct()
                .ToList();

            if (distinctCurrentAccountIds.Count > 1)
            {
                e.Cancel = true;
                SysMng.Instance.ActWndMng.ShowMsg(
                    SLanguage.GetString("Cari hesap fişinde birden fazla farklı cari hesap seçilemez. Lütfen tüm satırlarda aynı cari hesabı kullanın."),
                    ConstantStr.Warning,
                    Common.InformationMessages.MessageBoxButton.OK,
                    Common.InformationMessages.MessageBoxImage.Warning
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cari hesap kartındaki replikasyon alanlarını detay satırına (Erp_CurrentAccountReceiptItem) kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromCardToDetailItem(DataRow detailRow)
        {
            if (detailRow == null)
                return;

            // CurrentAccountId kontrolü
            long currAccId;
            if (!long.TryParse(detailRow["CurrentAccountId"]?.ToString(), out currAccId))
                return;

            // Detay tablosunda replikasyon alanları var mı kontrol et
            if (!detailRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                return;

            // Cari hesap kartındaki replikasyon değerlerini sorgula
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("select");
            sb.AppendLine(" UD_IsTransactionReplicable,");
            sb.AppendLine(" UD_ReplicationStartDate,");
            sb.AppendLine(" UD_ReplicationEndDate");
            sb.AppendLine(" from Erp_CurrentAccount with (nolock)");
            sb.AppendFormat(" where RecId = {0}", currAccId);

            DataTable dt = UtilityFunctions.GetDataTableList(
                BusinessObject.ActiveSession.dbInfo.DBProvider,
                BusinessObject.ActiveSession.dbInfo.Connection,
                null,
                "Erp_CurrentAccount",
                sb.ToString()
            );

            if (dt == null || dt.Rows.Count == 0)
                return;

            DataRow cardRow = dt.Rows[0];

            // Kart değerlerini detay satırına kopyala
            if (detailRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
            {
                detailRow["UD_IsTransactionReplicable"] = 
                    cardRow["UD_IsTransactionReplicable"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_IsTransactionReplicable"];
            }

            if (detailRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                detailRow["UD_ReplicationStartDate"] = 
                    cardRow["UD_ReplicationStartDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationStartDate"];
            }

            if (detailRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                detailRow["UD_ReplicationEndDate"] = 
                    cardRow["UD_ReplicationEndDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationEndDate"];
            }
        }

        /// <summary>
        /// Detay satırlarındaki replikasyon alanlarını başlığa (Erp_CurrentAccountReceipt) kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromDetailToHeader()
        {
            DataRow headerRow = BusinessObject.CurrentRow?.Row;
            if (headerRow == null)
                return;

            DataTable detailTable = BusinessObject.Data?.Tables["Erp_CurrentAccountReceiptItem"];
            if (detailTable == null)
                return;

            // Başlık tablosunda replikasyon alanları var mı kontrol et
            if (!headerRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                return;

            // İlk silinmemiş detay satırındaki replikasyon değerlerini al
            DataRow firstDetailRow = detailTable.Rows.Cast<DataRow>()
                .FirstOrDefault(row => row.RowState != DataRowState.Deleted && 
                                      row["CurrentAccountId"] != DBNull.Value &&
                                      !string.IsNullOrWhiteSpace(row["CurrentAccountId"]?.ToString()));

            if (firstDetailRow == null)
                return;

            // Detay satırındaki değerleri başlığa kopyala
            if (firstDetailRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
            {
                headerRow["UD_IsTransactionReplicable"] = 
                    firstDetailRow["UD_IsTransactionReplicable"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : firstDetailRow["UD_IsTransactionReplicable"];
            }

            if (firstDetailRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                headerRow["UD_ReplicationStartDate"] = 
                    firstDetailRow["UD_ReplicationStartDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : firstDetailRow["UD_ReplicationStartDate"];
            }

            if (firstDetailRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                headerRow["UD_ReplicationEndDate"] = 
                    firstDetailRow["UD_ReplicationEndDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : firstDetailRow["UD_ReplicationEndDate"];
            }
        }

        void CreateBulkUpdateTask(string boName, object[] ids, bool bulkSend)
        {
            _createBulkUpdateTaskService = BusinessObject.ActiveSession.Container.Resolve<ISystemService>("CreateBulkUpdateTaskService");
            if (_createBulkUpdateTaskService != null)
            {
                int i = (int)_createBulkUpdateTaskService.Execute(boName, ids, bulkSend);
                //if (i > 0)
                //    DXMessageBox.Show(SLanguage.GetString($"Uzak noktalara gönderme işlemi sıraya alındı. Kayıt Sayısı {i}"));
                //else
                //    DXMessageBox.Show(SLanguage.GetString($"İşlem iptal Edildi."));
            }
        }
    }
}
