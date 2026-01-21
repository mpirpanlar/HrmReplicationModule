using DevExpress.Xpf.Core;

using Prism.Ioc;

using Sentez.Common;
using Sentez.Common.Commands;
using Sentez.Common.SBase;
using Sentez.Common.SystemServices;
using Sentez.Data.BusinessObjects;
using Sentez.Data.Tools;
using Sentez.HrmReplicationModule.Parameters;
using Sentez.Localization;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace Sentez.HrmReplicationModule.BoExtensions
{
    public class GLReceiptReplicationTaskExtension : BoExtensionBase
    {
        ISystemService _createBulkUpdateTaskService;
        public GLReceiptReplicationTaskExtension(BusinessObjectBase bo)
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

            // AccountCode değiştiğinde - Erp_GLReceiptItem tablosunda
            if (e.Column != null && e.Column.ColumnName == "AccountCode")
            {
                if (e.Row != null && e.Row.Table != null && e.Row.Table.TableName == "Erp_GLReceiptItem")
                {
                    // GL hesap kartından Item satırına ön değer olarak kopyala
                    CopyReplicationFieldsFromGLAccountToItem(e.Row);
                }
            }
        }

        protected override void OnBeforePost(object sender, CancelEventArgs e)
        {
            if (!Enabled || _suppressEvents)
                return;
            base.OnBeforePost(sender, e);

            // Kayıt öncesi kontrol ve uyarı mesajları
            ValidateAndWarnReplicationStatus(e);

            // AccountCode değişen tüm Item satırları için GL hesap kartından değerleri kopyala
            if (BusinessObject.Data?.Tables["Erp_GLReceiptItem"] != null)
            {
                DataTable itemTable = BusinessObject.Data.Tables["Erp_GLReceiptItem"];
                foreach (DataRow itemRow in itemTable.Rows)
                {
                    if (itemRow.RowState != DataRowState.Deleted && 
                        itemRow["AccountCode"] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(itemRow["AccountCode"]?.ToString()))
                    {
                        CopyReplicationFieldsFromGLAccountToItem(itemRow);
                    }
                }
            }
        }

        protected override void OnAfterSucceededPost(object sender, EventArgs e)
        {
            base.OnAfterSucceededPost(sender, e);

            DataRow glReceiptRow = BusinessObject.CurrentRow?.Row;
            if (glReceiptRow == null)
                return;

            // SourceModule kontrolü - sadece elle girilen mahsup fişleri (SourceModule = 0) replike edilir
            if (glReceiptRow.Table.Columns.Contains("SourceModule"))
            {
                object sourceModuleObj = glReceiptRow["SourceModule"];
                if (sourceModuleObj != DBNull.Value && sourceModuleObj != null)
                {
                    int sourceModule;
                    if (int.TryParse(sourceModuleObj.ToString(), out sourceModule))
                    {
                        // SourceModule != 0 ise başka modüllerden oluşan fiş, replike edilmez
                        if (sourceModule != 0)
                            return;
                    }
                }
            }

            // Fiş tarihini al
            DateTime receiptDate;
            if (!DateTime.TryParse(glReceiptRow["ReceiptDate"]?.ToString(), out receiptDate))
                return;

            // Item tablosunda en az bir replike edilecek hesap var mı kontrol et
            DataTable itemTable = BusinessObject.Data?.Tables["Erp_GLReceiptItem"];
            if (itemTable == null)
                return;

            bool hasReplicableAccount = false;

            // Parametrelerden hesap kodlarını al - parametre tanımlı değilse işlem yapılmaz
            var parameters = BusinessObject.ActiveSession.ParamService.GetParameterClass<HrmReplicationModuleParameters>();
            if (string.IsNullOrWhiteSpace(parameters?.GLAccountCodes))
                return;

            List<string> accountCodePrefixes = parameters.GLAccountCodes.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            foreach (DataRow itemRow in itemTable.Rows)
            {
                if (itemRow.RowState == DataRowState.Deleted)
                    continue;

                // AccountCode al
                string accountCode = itemRow["AccountCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(accountCode))
                    continue;

                // Parametre kontrolü - belirtilen prefix'lerle başlayan hesaplar kontrol edilir
                bool matchesPrefix = accountCodePrefixes.Any(prefix => accountCode.StartsWith(prefix));
                if (!matchesPrefix)
                    continue; // Bu hesap replike edilmeyecek

                // Item satırındaki replikasyon alanlarını kontrol et
                if (itemRow.Table.Columns.Contains("UD_IsTransactionReplicable") &&
                    itemRow["UD_IsTransactionReplicable"] != DBNull.Value)
                {
                    bool isReplicable = Convert.ToBoolean(itemRow["UD_IsTransactionReplicable"]);
                    if (!isReplicable)
                        continue;

                    // Tarih kontrolü
                    DateTime? startDate = null;
                    DateTime? endDate = null;

                    if (itemRow.Table.Columns.Contains("UD_ReplicationStartDate") &&
                        itemRow["UD_ReplicationStartDate"] != DBNull.Value)
                    {
                        startDate = Convert.ToDateTime(itemRow["UD_ReplicationStartDate"]);
                    }

                    if (itemRow.Table.Columns.Contains("UD_ReplicationEndDate") &&
                        itemRow["UD_ReplicationEndDate"] != DBNull.Value)
                    {
                        endDate = Convert.ToDateTime(itemRow["UD_ReplicationEndDate"]);
                    }

                    // Tarih aralığı kontrolü
                    if (startDate.HasValue && receiptDate < startDate.Value)
                        continue;

                    if (endDate.HasValue && receiptDate > endDate.Value)
                        continue;

                    // Bu satır replike edilebilir
                    hasReplicableAccount = true;
                    break;
                }
            }

            // En az bir replike edilecek hesap varsa fişi replike et
            if (hasReplicableAccount)
            {
                System.Collections.ArrayList prm = new System.Collections.ArrayList
                {
                    Convert.ToInt64(glReceiptRow["RecId"].ToString())
                };
                CreateBulkUpdateTask(BusinessObject.Name, prm.ToArray(), true);
            }
        }

        /// <summary>
        /// Fiş girişinde replikasyon durumunu kontrol eder ve gerekirse uyarı mesajı gösterir
        /// </summary>
        private void ValidateAndWarnReplicationStatus(CancelEventArgs e)
        {
            DataRow glReceiptRow = BusinessObject.CurrentRow?.Row;
            if (glReceiptRow == null)
                return;

            // SourceModule kontrolü - sadece elle girilen mahsup fişleri (SourceModule = 0) için kontrol yapılır
            if (glReceiptRow.Table.Columns.Contains("SourceModule"))
            {
                object sourceModuleObj = glReceiptRow["SourceModule"];
                if (sourceModuleObj != DBNull.Value && sourceModuleObj != null)
                {
                    int sourceModule;
                    if (int.TryParse(sourceModuleObj.ToString(), out sourceModule))
                    {
                        // SourceModule != 0 ise başka modüllerden oluşan fiş, kontrol yapılmaz
                        if (sourceModule != 0)
                            return;
                    }
                }
            }

            // Fiş tarihini al
            DateTime receiptDate;
            if (!DateTime.TryParse(glReceiptRow["ReceiptDate"]?.ToString(), out receiptDate))
                return;

            DataTable itemTable = BusinessObject.Data?.Tables["Erp_GLReceiptItem"];
            if (itemTable == null)
                return;

            // Parametrelerden hesap kodlarını al - parametre tanımlı değilse işlem yapılmaz
            var parameters = BusinessObject.ActiveSession.ParamService.GetParameterClass<HrmReplicationModuleParameters>();
            if (string.IsNullOrWhiteSpace(parameters?.GLAccountCodes))
                return;

            List<string> accountCodePrefixes = parameters.GLAccountCodes.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // Parametrelerde belirtilen prefix'lerle başlayan satırları kontrol et
            List<DataRow> relevantRows = new List<DataRow>();
            
            foreach (DataRow itemRow in itemTable.Rows)
            {
                if (itemRow.RowState == DataRowState.Deleted)
                    continue;

                string accountCode = itemRow["AccountCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(accountCode))
                    continue;

                // Parametre kontrolü - belirtilen prefix'lerle başlayan hesaplar kontrol edilir
                bool matchesPrefix = accountCodePrefixes.Any(prefix => accountCode.StartsWith(prefix));
                if (matchesPrefix)
                {
                    relevantRows.Add(itemRow);
                }
            }

            // Parametrelerde belirtilen prefix'lerle başlayan hiç satır yoksa işlem yapılmaz
            if (!relevantRows.Any())
                return;

            // Replikasyon durumlarını kontrol et
            bool hasReplicableAccount = false;
            bool hasNonReplicableAccount = false;
            bool hasReplicationEnabledButDateNotValid = false;

            foreach (DataRow itemRow in relevantRows)
            {
                bool isReplicable = false;
                bool dateValid = true;

                // Item satırındaki replikasyon alanlarını kontrol et
                if (itemRow.Table.Columns.Contains("UD_IsTransactionReplicable") &&
                    itemRow["UD_IsTransactionReplicable"] != DBNull.Value)
                {
                    isReplicable = Convert.ToBoolean(itemRow["UD_IsTransactionReplicable"]);
                }

                // Replikasyon aktif ise tarih kontrolü yap
                if (isReplicable)
                {
                    DateTime? startDate = null;
                    DateTime? endDate = null;

                    if (itemRow.Table.Columns.Contains("UD_ReplicationStartDate") &&
                        itemRow["UD_ReplicationStartDate"] != DBNull.Value)
                    {
                        startDate = Convert.ToDateTime(itemRow["UD_ReplicationStartDate"]);
                    }

                    if (itemRow.Table.Columns.Contains("UD_ReplicationEndDate") &&
                        itemRow["UD_ReplicationEndDate"] != DBNull.Value)
                    {
                        endDate = Convert.ToDateTime(itemRow["UD_ReplicationEndDate"]);
                    }

                    // Tarih aralığı kontrolü
                    if (startDate.HasValue && receiptDate < startDate.Value)
                        dateValid = false;

                    if (endDate.HasValue && receiptDate > endDate.Value)
                        dateValid = false;

                    if (dateValid)
                    {
                        hasReplicableAccount = true;
                    }
                    else
                    {
                        hasReplicationEnabledButDateNotValid = true;
                        hasNonReplicableAccount = true; // Tarih uygun değil, replike edilmeyecek
                    }
                }
                else
                {
                    // Replikasyon aktif değil
                    hasNonReplicableAccount = true;
                }
            }

            // Aynı fiş içinde parametrelerde belirtilen kod ile başlayan satırlardan
            // bazıları replike edilecek, bazıları edilmeyecek durumdaysa kayıt engelle
            if (hasReplicableAccount && hasNonReplicableAccount)
            {
                e.Cancel = true;
                SysMng.Instance.ActWndMng.ShowMsg(
                    SLanguage.GetString("Aynı fiş içerisinde parametrelerde belirtilen kod ile başlayan hesaplardan bazıları replike edilecek, bazıları edilmeyecek durumdadır. Tüm hesapların replikasyon durumu aynı olmalıdır. Kayıt işlemi iptal edilmiştir."),
                    ConstantStr.Warning,
                    Common.InformationMessages.MessageBoxButton.OK,
                    Common.InformationMessages.MessageBoxImage.Warning
                );
                return;
            }

            // Hiç replike edilecek hesap yoksa ama replikasyon özelliği seçili ve tarih uygun değilse uyarı (bilgi amaçlı)
            if (!hasReplicableAccount && hasReplicationEnabledButDateNotValid)
            {
                SysMng.Instance.ActWndMng.ShowMsg(
                    SLanguage.GetString("Bu fiş içerisinde replikasyon aktif olan hesaplar bulunmakta ancak fiş tarihi replikasyon tarih aralığında değildir. Bu fiş replike edilmeyecektir."),
                    ConstantStr.Information,
                    Common.InformationMessages.MessageBoxButton.OK,
                    Common.InformationMessages.MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// GL hesap kartındaki (Erp_GLAccount) replikasyon alanlarını Item satırına (Erp_GLReceiptItem) ön değer olarak kopyalar
        /// </summary>
        private void CopyReplicationFieldsFromGLAccountToItem(DataRow itemRow)
        {
            if (itemRow == null)
                return;

            // AccountCode kontrolü
            string accountCode = itemRow["AccountCode"]?.ToString();
            if (string.IsNullOrWhiteSpace(accountCode))
                return;

            // Item tablosunda replikasyon alanları var mı kontrol et
            if (!itemRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
                return;

            // GL hesap kartındaki replikasyon değerlerini sorgula
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("select");
            sb.AppendLine(" UD_IsTransactionReplicable,");
            sb.AppendLine(" UD_ReplicationStartDate,");
            sb.AppendLine(" UD_ReplicationEndDate");
            sb.AppendLine(" from Erp_GLAccount with (nolock)");
            sb.AppendFormat(" where AccountCode = '{0}'", accountCode.Replace("'", "''"));

            DataTable dt = UtilityFunctions.GetDataTableList(
                BusinessObject.ActiveSession.dbInfo.DBProvider,
                BusinessObject.ActiveSession.dbInfo.Connection,
                null,
                "Erp_GLAccount",
                sb.ToString()
            );

            if (dt == null || dt.Rows.Count == 0)
                return;

            DataRow glAccountRow = dt.Rows[0];

            // Kart değerlerini Item satırına kopyala (sadece değer yoksa)
            if (itemRow.Table.Columns.Contains("UD_IsTransactionReplicable"))
            {
                if (itemRow["UD_IsTransactionReplicable"] == DBNull.Value || itemRow["UD_IsTransactionReplicable"] == null)
                {
                    itemRow["UD_IsTransactionReplicable"] = 
                        glAccountRow["UD_IsTransactionReplicable"] == DBNull.Value 
                        ? (object)DBNull.Value 
                        : glAccountRow["UD_IsTransactionReplicable"];
                }
            }

            if (itemRow.Table.Columns.Contains("UD_ReplicationStartDate"))
            {
                if (itemRow["UD_ReplicationStartDate"] == DBNull.Value || itemRow["UD_ReplicationStartDate"] == null)
                {
                    itemRow["UD_ReplicationStartDate"] = 
                        glAccountRow["UD_ReplicationStartDate"] == DBNull.Value 
                        ? (object)DBNull.Value 
                        : glAccountRow["UD_ReplicationStartDate"];
                }
            }

            if (itemRow.Table.Columns.Contains("UD_ReplicationEndDate"))
            {
                if (itemRow["UD_ReplicationEndDate"] == DBNull.Value || itemRow["UD_ReplicationEndDate"] == null)
                {
                    itemRow["UD_ReplicationEndDate"] = 
                        glAccountRow["UD_ReplicationEndDate"] == DBNull.Value 
                        ? (object)DBNull.Value 
                        : glAccountRow["UD_ReplicationEndDate"];
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