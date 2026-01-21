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
    public class ChequeReceiptReplicationTaskExtension : BoExtensionBase
    {
        ISystemService _createBulkUpdateTaskService;
        public ChequeReceiptReplicationTaskExtension(BusinessObjectBase bo)
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

            // CurrentAccountId değiştiğinde
            if (e.Column != null && e.Column.ColumnName == "CurrentAccountId")
            {
                // Başlık tablosunda (Erp_ChequeReceipt) CurrentAccountId değiştiğinde
                if (e.Row != null && e.Row.Table != null && e.Row.Table.TableName == "Erp_ChequeReceipt")
                {
                    // Cari kartından doğrudan başlığa kopyala
                    CopyReplicationFieldsFromCardToHeader(e.Row);
                    // Başlıktan Item ve Erp_Cheque tablolarına kopyala
                    CopyReplicationFieldsFromHeaderToDetailAndCheque();
                }
                // Detay tablosunda (Erp_ChequeReceiptItem) CurrentAccountId değiştiğinde
                else if (e.Row != null && e.Row.Table != null && e.Row.Table.TableName == "Erp_ChequeReceiptItem")
                {
                    // Cari kartından doğrudan başlığa kopyala
                    CopyReplicationFieldsFromCardToHeader();
                    // Başlıktan Item ve Erp_Cheque tablolarına kopyala
                    CopyReplicationFieldsFromHeaderToDetailAndCheque();
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

            // Kayıt öncesi cari kartından başlığa kopyalama yap
            if (BusinessObject.CurrentRow?.Row != null)
            {
                CopyReplicationFieldsFromCardToHeader();
                // Başlıktan Item ve Erp_Cheque tablolarına kopyala
                CopyReplicationFieldsFromHeaderToDetailAndCheque();
            }
        }

        protected override void OnAfterSucceededPost(object sender, EventArgs e)
        {
            base.OnAfterSucceededPost(sender, e);

            bool isOk = true;

            // Evrak (çek fişi) tablosundaki alanlardan kontrol yap
            DataRow chequeReceiptRow = BusinessObject.CurrentRow?.Row;
            if (chequeReceiptRow == null)
                return;

            DateTime receiptDate;
            if (!DateTime.TryParse(chequeReceiptRow["ReceiptDate"]?.ToString(), out receiptDate))
                isOk = false;

            if (isOk)
            {
                // Replikasyon aktif mi kontrolü (evrak tablosundan)
                bool isReplicable = false;
                if (chequeReceiptRow.Table.Columns.Contains("UD_IsTransactionReplicable") &&
                    chequeReceiptRow["UD_IsTransactionReplicable"] != DBNull.Value)
                {
                    isReplicable = Convert.ToBoolean(chequeReceiptRow["UD_IsTransactionReplicable"]);
                }

                if (!isReplicable)
                    isOk = false;
            }

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (isOk && chequeReceiptRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                startDate = chequeReceiptRow["UD_ReplicationStartDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(chequeReceiptRow["UD_ReplicationStartDate"]);

                if (startDate.HasValue && receiptDate < startDate.Value)
                    isOk = false;
            }

            if (isOk && chequeReceiptRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                endDate = chequeReceiptRow["UD_ReplicationEndDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(chequeReceiptRow["UD_ReplicationEndDate"]);

                if (endDate.HasValue && receiptDate > endDate.Value)
                    isOk = false;
            }

            if (isOk)
            {
                System.Collections.ArrayList prm = new System.Collections.ArrayList
                {
                    Convert.ToInt64(chequeReceiptRow["RecId"].ToString())
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

            DataTable detailTable = BusinessObject.Data?.Tables["Erp_ChequeReceiptItem"];
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
                    SLanguage.GetString("Çek fişinde birden fazla farklı cari hesap seçilemez. Lütfen tüm satırlarda aynı cari hesabı kullanın."),
                    ConstantStr.Warning,
                    Common.InformationMessages.MessageBoxButton.OK,
                    Common.InformationMessages.MessageBoxImage.Warning
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cari hesap kartındaki replikasyon alanlarını doğrudan başlığa (Erp_ChequeReceipt) kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromCardToHeader(DataRow headerRow = null)
        {
            if (headerRow == null)
                headerRow = BusinessObject.CurrentRow?.Row;
            
            if (headerRow == null)
                return;

            // Başlık tablosunda replikasyon alanları var mı kontrol et
            if (!headerRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                return;

            // CurrentAccountId kontrolü - önce başlık tablosunda kontrol et
            long currAccId = 0;
            bool hasCurrentAccountId = false;

            if (headerRow.Table.Columns.Contains("CurrentAccountId") && 
                headerRow["CurrentAccountId"] != DBNull.Value &&
                !string.IsNullOrWhiteSpace(headerRow["CurrentAccountId"]?.ToString()))
            {
                if (long.TryParse(headerRow["CurrentAccountId"]?.ToString(), out currAccId))
                    hasCurrentAccountId = true;
            }

            // Başlıkta yoksa detay tablosundan al
            if (!hasCurrentAccountId)
            {
                DataTable detailTable = BusinessObject.Data?.Tables["Erp_ChequeReceiptItem"];
                if (detailTable != null)
                {
                    DataRow firstDetailRow = detailTable.Rows.Cast<DataRow>()
                        .FirstOrDefault(row => row.RowState != DataRowState.Deleted && 
                                              row["CurrentAccountId"] != DBNull.Value &&
                                              !string.IsNullOrWhiteSpace(row["CurrentAccountId"]?.ToString()));
                    
                    if (firstDetailRow != null && 
                        long.TryParse(firstDetailRow["CurrentAccountId"]?.ToString(), out currAccId))
                    {
                        hasCurrentAccountId = true;
                    }
                }
            }

            if (!hasCurrentAccountId)
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

            // Kart değerlerini başlığa kopyala
            if (headerRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
            {
                headerRow["UD_IsTransactionReplicable"] = 
                    cardRow["UD_IsTransactionReplicable"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_IsTransactionReplicable"];
            }

            if (headerRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                headerRow["UD_ReplicationStartDate"] = 
                    cardRow["UD_ReplicationStartDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationStartDate"];
            }

            if (headerRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                headerRow["UD_ReplicationEndDate"] = 
                    cardRow["UD_ReplicationEndDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationEndDate"];
            }
        }

        /// <summary>
        /// Başlıktaki (Erp_ChequeReceipt) replikasyon alanlarını Item (Erp_ChequeReceiptItem) ve Erp_Cheque tablolarına kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromHeaderToDetailAndCheque()
        {
            DataRow headerRow = BusinessObject.CurrentRow?.Row;
            if (headerRow == null)
                return;

            // Başlık tablosunda replikasyon alanları var mı kontrol et
            if (!headerRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                return;

            // Başlıktan değerleri al
            object isReplicable = headerRow["UD_IsTransactionReplicable"] == DBNull.Value 
                ? (object)DBNull.Value 
                : headerRow["UD_IsTransactionReplicable"];
            
            object startDate = headerRow["UD_ReplicationStartDate"] == DBNull.Value 
                ? (object)DBNull.Value 
                : headerRow["UD_ReplicationStartDate"];
            
            object endDate = headerRow["UD_ReplicationEndDate"] == DBNull.Value 
                ? (object)DBNull.Value 
                : headerRow["UD_ReplicationEndDate"];

            // Erp_ChequeReceiptItem tablosuna kopyala (eğer alanlar varsa)
            DataTable detailTable = BusinessObject.Data?.Tables["Erp_ChequeReceiptItem"];
            if (detailTable != null && detailTable.Columns.Contains("UD_IsTransactionReplicable"))
            {
                foreach (DataRow detailRow in detailTable.Rows)
                {
                    if (detailRow.RowState == DataRowState.Deleted)
                        continue;

                    if (detailRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                        detailRow["UD_IsTransactionReplicable"] = isReplicable;

                    if (detailRow.Table.Columns.Contains("UD_ReplicationStartDate"))
                        detailRow["UD_ReplicationStartDate"] = startDate;

                    if (detailRow.Table.Columns.Contains("UD_ReplicationEndDate"))
                        detailRow["UD_ReplicationEndDate"] = endDate;
                }
            }

            // Erp_Cheque tablosuna kopyala (eğer alanlar varsa)
            // Önce ChequeId'leri bulmalıyız
            if (detailTable != null)
            {
                var chequeIds = detailTable.Rows.Cast<DataRow>()
                    .Where(row => row.RowState != DataRowState.Deleted && 
                                 row["ChequeId"] != DBNull.Value &&
                                 !string.IsNullOrWhiteSpace(row["ChequeId"]?.ToString()))
                    .Select(row => row["ChequeId"].ToString())
                    .Distinct()
                    .ToList();

                if (chequeIds.Any())
                {
                    DataTable chequeTable = BusinessObject.Data?.Tables["Erp_Cheque"];
                    if (chequeTable != null && chequeTable.Columns.Contains("UD_IsTransactionReplicable"))
                    {
                        foreach (string chequeIdStr in chequeIds)
                        {
                            long chequeId;
                            if (!long.TryParse(chequeIdStr, out chequeId))
                                continue;

                            DataRow chequeRow = chequeTable.Rows.Cast<DataRow>()
                                .FirstOrDefault(row => row.RowState != DataRowState.Deleted &&
                                                      row["RecId"] != DBNull.Value &&
                                                      row["RecId"].ToString() == chequeIdStr);

                            if (chequeRow != null)
                            {
                                if (chequeRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                                    chequeRow["UD_IsTransactionReplicable"] = isReplicable;

                                if (chequeRow.Table.Columns.Contains("UD_ReplicationStartDate"))
                                    chequeRow["UD_ReplicationStartDate"] = startDate;

                                if (chequeRow.Table.Columns.Contains("UD_ReplicationEndDate"))
                                    chequeRow["UD_ReplicationEndDate"] = endDate;
                            }
                        }
                    }
                }
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
