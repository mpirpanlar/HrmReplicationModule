using DevExpress.Xpf.Core;

using Prism.Ioc;

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
    public class CurrentAccountReplicationTaskExtension : BoExtensionBase
    {
        ISystemService _createBulkUpdateTaskService;
        public CurrentAccountReplicationTaskExtension(BusinessObjectBase bo)
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

            // CurrentAccountId değiştiğinde kart değerlerini fatura tablosuna kopyala
            if (e.Column != null && e.Column.ColumnName == "CurrentAccountId" && BusinessObject.CurrentRow?.Row != null)
            {
                CopyReplicationFieldsFromCardToInvoice();
            }
        }

        protected override void OnBeforePost(object sender, CancelEventArgs e)
        {
            if (!Enabled || _suppressEvents)
                return;
            base.OnBeforePost(sender, e);

            // Kayıt öncesi kart değerlerini fatura tablosuna kopyala (eğer henüz kopyalanmadıysa)
            if (BusinessObject.CurrentRow?.Row != null)
            {
                CopyReplicationFieldsFromCardToInvoice();
            }
        }

        protected override void OnAfterSucceededPost(object sender, EventArgs e)
        {
            base.OnAfterSucceededPost(sender, e);

            bool isOk = true;

            // Evrak (fatura) tablosundaki alanlardan kontrol yap
            DataRow invoiceRow = BusinessObject.CurrentRow?.Row;
            if (invoiceRow == null)
                return;

            DateTime receiptDate;
            if (!DateTime.TryParse(invoiceRow["ReceiptDate"]?.ToString(), out receiptDate))
                isOk = false;

            if (isOk)
            {
                // Replikasyon aktif mi kontrolü (evrak tablosundan)
                bool isReplicable = false;
                if (invoiceRow.Table.Columns.Contains("UD_IsTransactionReplicable") &&
                    invoiceRow["UD_IsTransactionReplicable"] != DBNull.Value)
                {
                    isReplicable = Convert.ToBoolean(invoiceRow["UD_IsTransactionReplicable"]);
                }

                if (!isReplicable)
                    isOk = false;
            }

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (isOk && invoiceRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                startDate = invoiceRow["UD_ReplicationStartDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(invoiceRow["UD_ReplicationStartDate"]);

                if (startDate.HasValue && receiptDate < startDate.Value)
                    isOk = false;
            }

            if (isOk && invoiceRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                endDate = invoiceRow["UD_ReplicationEndDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(invoiceRow["UD_ReplicationEndDate"]);

                if (endDate.HasValue && receiptDate > endDate.Value)
                    isOk = false;
            }

            if (isOk)
            {
                System.Collections.ArrayList prm = new System.Collections.ArrayList
                {
                    Convert.ToInt64(invoiceRow["RecId"].ToString())
                };
                CreateBulkUpdateTask(BusinessObject.Name, prm.ToArray(), true);
            }
        }

        /// <summary>
        /// Cari hesap kartındaki replikasyon alanlarını fatura tablosuna kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromCardToInvoice()
        {
            if (BusinessObject.CurrentRow?.Row == null)
                return;

            DataRow invoiceRow = BusinessObject.CurrentRow.Row;

            // CurrentAccountId kontrolü
            long currAccId;
            if (!long.TryParse(invoiceRow["CurrentAccountId"]?.ToString(), out currAccId))
                return;

            // Fatura tablosunda replikasyon alanları var mı kontrol et
            DataTable invoiceTable = BusinessObject.Data?.Tables["Erp_Invoice"];
            if (invoiceTable == null || !invoiceTable.Columns.Contains("UD_IsTransactionReplicable"))
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

            // Kart değerlerini fatura tablosuna kopyala
            if (invoiceRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
            {
                invoiceRow["UD_IsTransactionReplicable"] = 
                    cardRow["UD_IsTransactionReplicable"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_IsTransactionReplicable"];
            }

            if (invoiceRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                invoiceRow["UD_ReplicationStartDate"] = 
                    cardRow["UD_ReplicationStartDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationStartDate"];
            }

            if (invoiceRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                invoiceRow["UD_ReplicationEndDate"] = 
                    cardRow["UD_ReplicationEndDate"] == DBNull.Value 
                    ? (object)DBNull.Value 
                    : cardRow["UD_ReplicationEndDate"];
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
