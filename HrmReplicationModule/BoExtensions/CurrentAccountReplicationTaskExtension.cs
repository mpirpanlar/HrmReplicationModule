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
        }

        protected override void OnBeforePost(object sender, CancelEventArgs e)
        {
            if (!Enabled || _suppressEvents)
                return;
            base.OnBeforePost(sender, e);
        }

        protected override void OnAfterSucceededPost(object sender, EventArgs e)
        {
            base.OnAfterSucceededPost(sender, e);

            bool isOk = true;

            long currAccId;
            if (!long.TryParse(BusinessObject.CurrentRow["CurrentAccountId"]?.ToString(), out currAccId))
                isOk = false;

            DateTime receiptDate;
            if (!DateTime.TryParse(BusinessObject.CurrentRow["ReceiptDate"]?.ToString(), out receiptDate))
                isOk = false;

            DataRow row = null;

            if (isOk)
            {
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
                    isOk = false;
                else
                    row = dt.Rows[0];
            }

            if (isOk)
            {
                bool isReplicable =
                    row["UD_IsTransactionReplicable"] != DBNull.Value &&
                    Convert.ToBoolean(row["UD_IsTransactionReplicable"]);

                if (!isReplicable)
                    isOk = false;
            }

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (isOk)
            {
                startDate = row["UD_ReplicationStartDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["UD_ReplicationStartDate"]);

                endDate = row["UD_ReplicationEndDate"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["UD_ReplicationEndDate"]);

                if (startDate.HasValue && receiptDate < startDate.Value)
                    isOk = false;

                if (endDate.HasValue && receiptDate > endDate.Value)
                    isOk = false;
            }

            if (isOk)
            {
                // ================================
                // Task oluşturma buraya yapılacak
                // ================================
                System.Collections.ArrayList prm = new System.Collections.ArrayList
                {
                    Convert.ToInt64(BusinessObject.CurrentRow["RecId"].ToString())
                };
                CreateBulkUpdateTask(BusinessObject.Name, prm.ToArray(), true);
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
