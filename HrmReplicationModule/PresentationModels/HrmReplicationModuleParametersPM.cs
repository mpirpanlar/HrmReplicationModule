using Sentez.Common.PresentationModels;
using Prism.Ioc;
using Sentez.Common.Commands;
using Sentez.Localization;
using LiveCore.Desktop.UI.Controls;
using Sentez.Common.Utilities;
using Sentez.Data.Tools;
using System.Data;
using Sentez.Data.MetaData;
using System.IO;
using System.Windows;
using Sentez.Core.ParameterClasses;
using DevExpress.Xpf.Grid;
using Sentez.HrmReplicationModule.Parameters;
using Sentez.Common.SBase;
using System.Data.SqlClient;
using System.Text;
using DevExpress.Xpf.Core;
using System;
using Sentez.Common;

namespace Sentez.HrmReplicationModule.PresentationModels
{
    public class HrmReplicationModuleParametersPM : PMDesktop
    {
        public ProgramParameters ProgramParam { get; set; }

        Visibility companyBasedParameterVisibility = Visibility.Collapsed;
        public Visibility CompanyBasedParameterVisibility
        {
            get
            {
                return companyBasedParameterVisibility;
            }
            set
            {
                companyBasedParameterVisibility = value;
                OnPropertyChanged("CompanyBasedParameterVisibility");
            }
        }

        Visibility workplaceBasedParameterVisibility = Visibility.Collapsed;
        public Visibility WorkplaceBasedParameterVisibility
        {
            get
            {
                return workplaceBasedParameterVisibility;
            }
            set
            {
                workplaceBasedParameterVisibility = value;
                OnPropertyChanged("WorkplaceBasedParameterVisibility");
            }
        }


        int? parameterCompanyId = null;
        public int? ParameterCompanyId
        {
            get
            {
                return parameterCompanyId;
            }
            set
            {
                if (parameterCompanyId == value) return;
                parameterCompanyId = value;
                WorkPlaceDataView = null;
                if (ProgramParam.CanWorkplaceBased == 1 && value != null)
                    using (var workplace = UtilityFunctions.GetDataTableList(ActiveSession.dbInfo.DBProvider, ActiveSession.dbInfo.Connection, null, "Erp_Workplace", $"SELECT NULL RecId, NULL WorkplaceCodeName UNION ALL select RecId, WorkplaceCode+' - '+ WorkplaceName WorkplaceCodeName from Erp_Workplace where InUse=1 and CompanyId={value}  order by WorkplaceCodeName"))
                        WorkPlaceDataView = workplace?.AsDataView();

                OnPropertyChanged("ParameterCompanyId");
                LoadParameters();
            }
        }

        int? parameterWorkplaceId = null;
        public int? ParameterWorkplaceId
        {
            get
            {
                return parameterWorkplaceId;
            }
            set
            {
                if (parameterWorkplaceId == value) return;
                parameterWorkplaceId = value;
                OnPropertyChanged("ParameterWorkplaceId");
                LoadParameters();
            }
        }

        private Visibility canWorkplaceBasedVisibility;
        public Visibility CanWorkplaceBasedVisibility
        {
            get { return canWorkplaceBasedVisibility; }
            set { canWorkplaceBasedVisibility = value; OnPropertyChanged("CanWorkplaceBasedVisibility"); }
        }

        public byte CompanyBasedParam
        {
            get
            {
                if (ProgramParam.CanCompanyBased == 1)
                {
                    CanWorkplaceBasedVisibility = Visibility.Visible;
                }
                else
                {
                    CanWorkplaceBasedVisibility = Visibility.Collapsed;
                    ProgramParam.CanWorkplaceBased = 0;
                }
                return ProgramParam.CanCompanyBased;
            }
            set
            {
                ProgramParam.CanCompanyBased = value;
                OnPropertyChanged("CompanyBasedParam");
            }
        }


        private DataView workplaceDataView;
        public DataView WorkPlaceDataView
        {
            get { return workplaceDataView; }
            set { workplaceDataView = value; OnPropertyChanged("WorkPlaceDataView"); }
        }




        public LookupList Lists { get; set; }

        HrmReplicationModuleParameters parameters;
        public HrmReplicationModuleParameters Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                OnPropertyChanged("Parameters");
            }
        }

        public HrmReplicationModuleParametersPM(IContainerExtension container)
            : base(container)
        {
            Parameters = ActiveSession.ParamService.GetParameterClass<HrmReplicationModuleParameters>();
        }

        public override void Init()
        {
            base.Init();
            Lists = ActiveSession.LookupList.GetChild(UtilityFunctions.GetConnection(ActiveSession.dbInfo.DBProvider, ActiveSession.dbInfo.ConnectionString));


            InsertContextMenu(AddToMenu(new MenuItemPM(SLanguage.GetString("Kaydet"), "SaveParametersCommand") { ShortcutKey = System.Windows.Input.Key.F5, ShortcutKeyModifier = System.Windows.Input.ModifierKeys.None }, null));
            InsertContextMenu(AddToMenu(new MenuItemPM(SLanguage.GetString("Parametreleri Hedef Veritabanına Kopyala"), "CopyParametersToTargetDatabaseCommand"), null));

            if (ProgramParam == null)
                ProgramParam = ActiveSession.ParamService.GetParameterClass<ProgramParameters>();
            if (ProgramParam.CanCompanyBased == 1)
            {
                CompanyBasedParameterVisibility = Visibility.Visible;
                if (ProgramParam.CanWorkplaceBased == 1)
                    WorkplaceBasedParameterVisibility = Visibility.Visible;
            }
            LoadParameters();
        }

        public override void LoadCommands()
        {
            base.LoadCommands();
            CmdList.AddCmd(901, "SaveParametersCommand", SLanguage.GetString("Parametreleri Kaydet"), OnSaveParametersCommand, null);
            CmdList.AddCmd(902, "CopyParametersToTargetDatabaseCommand", SLanguage.GetString("Parametreleri Hedef Veritabanına Kopyala"), OnCopyParametersToTargetDatabaseCommand, CanCopyParametersToTargetDatabaseCommand);
        }

        void OnSaveParametersCommand(ISysCommandParam arg)
        {
            int? prmCompanyId = ParameterCompanyId;
            int? prmWorkplaceId = ParameterWorkplaceId;

            Parameters?.Save(ParameterCompanyId, ParameterWorkplaceId, null, ActiveSession.dbInfo.Connection);
        }

        void LoadParameters(bool closedPM = false)
        {
            Parameters?.Clear();
            int? prmCompanyId = ParameterCompanyId;
            int? prmWorkplaceId = ParameterWorkplaceId;
            if (closedPM)
            {
                prmCompanyId = ActiveSession.ActiveCompany.RecId;
                if (ActiveSession?.Workplace != null)
                    prmWorkplaceId = ActiveSession.Workplace.RecId;
                Parameters.Load(prmCompanyId, prmWorkplaceId, null, ActiveSession.dbInfo.Connection);
            }
            else
            {
                Parameters.Load(prmCompanyId, prmWorkplaceId, null, ActiveSession.dbInfo.Connection);
            }
        }

        public override bool Closed(object param)
        {
            LoadParameters(true);
            return false;
        }

        bool CanCopyParametersToTargetDatabaseCommand(ISysCommandParam obj)
        {
            if (Parameters == null)
                return false;

            return !string.IsNullOrWhiteSpace(Parameters.TargetServer) &&
                   !string.IsNullOrWhiteSpace(Parameters.TargetDatabase) &&
                   !string.IsNullOrWhiteSpace(Parameters.TargetUserName) &&
                   !string.IsNullOrWhiteSpace(Parameters.TargetPassword);
        }

        void OnCopyParametersToTargetDatabaseCommand(ISysCommandParam arg)
        {
            try
            {
                // Parametre kontrolü
                if (string.IsNullOrWhiteSpace(Parameters.TargetServer) ||
                    string.IsNullOrWhiteSpace(Parameters.TargetDatabase) ||
                    string.IsNullOrWhiteSpace(Parameters.TargetUserName) ||
                    string.IsNullOrWhiteSpace(Parameters.TargetPassword))
                {
                    SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString("Lütfen tüm SQL bağlantı bilgilerini doldurun."), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
                    return;
                }

                // Hedef veritabanına bağlantı testi
                string targetConnectionString = BuildConnectionString();
                try
                {
                    using (SqlConnection testConn = new SqlConnection(targetConnectionString))
                    {
                        testConn.Open();
                    }
                }
                catch (Exception testEx)
                {
                    SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString($"Hedef veritabanına bağlanılamadı: {testEx.Message}"), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
                    return;
                }

                // Mevcut veritabanından PrmGroup = 32 olan parametreleri oku
                int? companyId = ParameterCompanyId ?? ActiveSession.ActiveCompany?.RecId;
                int? workplaceId = ParameterWorkplaceId ?? ActiveSession.Workplace?.RecId;

                StringBuilder selectQuery = new StringBuilder();
                selectQuery.AppendLine("SELECT RecId, CompanyId, WorkplaceId, PrmGroup, PrmId, PrmValue, Condition");
                selectQuery.AppendLine("FROM Erp_Parameter");
                selectQuery.AppendLine("WHERE PrmGroup = 32 AND (IsDeleted IS NULL OR IsDeleted = 0)");
                
                if (companyId.HasValue)
                    selectQuery.AppendLine($"AND (CompanyId = {companyId.Value} OR CompanyId IS NULL)");
                else
                    selectQuery.AppendLine("AND CompanyId IS NULL");
                
                if (workplaceId.HasValue)
                    selectQuery.AppendLine($"AND (WorkplaceId = {workplaceId.Value} OR WorkplaceId IS NULL)");
                else
                    selectQuery.AppendLine("AND WorkplaceId IS NULL");

                DataTable sourceParams = null;
                using (var dt = UtilityFunctions.GetDataTableList(
                    ActiveSession.dbInfo.DBProvider,
                    ActiveSession.dbInfo.Connection,
                    null,
                    "Erp_Parameter",
                    selectQuery.ToString()))
                {
                    sourceParams = dt;
                }

                if (sourceParams == null || sourceParams.Rows.Count == 0)
                {
                    SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString("Kaynak veritabanında PrmGroup=32 parametre kaydı bulunamadı."), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
                    return;
                }

                // Hedef veritabanında şirket ve iş yeri RecId'lerini bul
                int? targetCompanyId = null;
                int? targetWorkplaceId = null;
                int? targetUserId = null;

                using (SqlConnection targetConn = new SqlConnection(targetConnectionString))
                {
                    targetConn.Open();

                    // Hedef veritabanında kullanıcı RecId'sini bul (Meta_User tablosundan)
                    string currentUserCode = ActiveSession.ActiveUser?.UserCode;
                    if (!string.IsNullOrWhiteSpace(currentUserCode))
                    {
                        // Önce UserCode ile kullanıcıyı bul (IsDeleted kontrolü ile)
                        string userQuery = "SELECT RecId FROM Meta_User WHERE UserCode = @UserCode AND (IsDeleted IS NULL OR IsDeleted = 0)";
                        using (SqlCommand cmd = new SqlCommand(userQuery, targetConn))
                        {
                            cmd.Parameters.AddWithValue("@UserCode", currentUserCode);
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                targetUserId = Convert.ToInt32(result);
                            }
                        }

                        // Eğer UserCode ile bulunamazsa, ilk kullanılabilir kullanıcıyı al
                        if (!targetUserId.HasValue)
                        {
                            string firstUserQuery = "SELECT TOP 1 RecId FROM Meta_User WHERE (IsDeleted IS NULL OR IsDeleted = 0) ORDER BY RecId";
                            using (SqlCommand cmd = new SqlCommand(firstUserQuery, targetConn))
                            {
                                object result = cmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    targetUserId = Convert.ToInt32(result);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Eğer UserCode yoksa, ilk kullanılabilir kullanıcıyı al
                        string firstUserQuery = "SELECT TOP 1 RecId FROM Meta_User WHERE (IsDeleted IS NULL OR IsDeleted = 0) ORDER BY RecId";
                        using (SqlCommand cmd = new SqlCommand(firstUserQuery, targetConn))
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                targetUserId = Convert.ToInt32(result);
                            }
                        }
                    }

                    // Hedef şirket RecId'sini bul
                    if (!string.IsNullOrWhiteSpace(Parameters.TargetCompanyCode))
                    {
                        string companyQuery = "SELECT RecId FROM Erp_Company WHERE CompanyCode = @CompanyCode AND InUse = 1";
                        using (SqlCommand cmd = new SqlCommand(companyQuery, targetConn))
                        {
                            cmd.Parameters.AddWithValue("@CompanyCode", Parameters.TargetCompanyCode);
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                targetCompanyId = Convert.ToInt32(result);
                            }
                            else
                            {
                                SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString($"Hedef veritabanında şirket kodu '{Parameters.TargetCompanyCode}' bulunamadı."), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // Hedef iş yeri RecId'sini bul
                    if (!string.IsNullOrWhiteSpace(Parameters.TargetWorkplaceCode))
                    {
                        string workplaceQuery = "SELECT RecId FROM Erp_Workplace WHERE WorkplaceCode = @WorkplaceCode AND InUse = 1";
                        if (targetCompanyId.HasValue)
                        {
                            workplaceQuery += " AND CompanyId = @CompanyId";
                        }
                        
                        using (SqlCommand cmd = new SqlCommand(workplaceQuery, targetConn))
                        {
                            cmd.Parameters.AddWithValue("@WorkplaceCode", Parameters.TargetWorkplaceCode);
                            if (targetCompanyId.HasValue)
                            {
                                cmd.Parameters.AddWithValue("@CompanyId", targetCompanyId.Value);
                            }
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                targetWorkplaceId = Convert.ToInt32(result);
                            }
                            else
                            {
                                SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString($"Hedef veritabanında iş yeri kodu '{Parameters.TargetWorkplaceCode}' bulunamadı."), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // Hedef veritabanına parametreleri yaz
                    foreach (DataRow row in sourceParams.Rows)
                    {
                        // Hedef taraftaki CompanyId ve WorkplaceId değerlerini belirle
                        object targetCompanyIdValue = DBNull.Value;
                        object targetWorkplaceIdValue = DBNull.Value;

                        // Eğer parametrelerde şirket kodu tanımlıysa, hedef taraftaki RecId'yi kullan
                        // Değilse kaynak taraftaki CompanyId değerini kullan
                        if (!string.IsNullOrWhiteSpace(Parameters.TargetCompanyCode))
                        {
                            targetCompanyIdValue = targetCompanyId.Value;
                        }
                        else
                        {
                            // Kaynak taraftaki CompanyId değerini al
                            if (row["CompanyId"] != DBNull.Value)
                            {
                                targetCompanyIdValue = row["CompanyId"];
                            }
                        }

                        // Eğer parametrelerde iş yeri kodu tanımlıysa, hedef taraftaki RecId'yi kullan
                        // Değilse kaynak taraftaki WorkplaceId değerini kullan
                        if (!string.IsNullOrWhiteSpace(Parameters.TargetWorkplaceCode))
                        {
                            targetWorkplaceIdValue = targetWorkplaceId.Value;
                        }
                        else
                        {
                            // Kaynak taraftaki WorkplaceId değerini al
                            if (row["WorkplaceId"] != DBNull.Value)
                            {
                                targetWorkplaceIdValue = row["WorkplaceId"];
                            }
                        }

                        string upsertQuery = @"
                            IF EXISTS (SELECT 1 FROM Erp_Parameter 
                                       WHERE PrmGroup = @PrmGroup
                                       AND PrmId = @PrmId
                                       AND ((CompanyId = @CompanyId) OR (CompanyId IS NULL AND @CompanyId IS NULL))
                                       AND ((WorkplaceId = @WorkplaceId) OR (WorkplaceId IS NULL AND @WorkplaceId IS NULL))
                                       AND (IsDeleted IS NULL OR IsDeleted = 0))
                            BEGIN
                                UPDATE Erp_Parameter 
                                SET PrmValue = @PrmValue,
                                    /*Condition = @Condition,*/
                                    UpdatedAt = GETDATE(),
                                    UpdatedBy = @UpdatedBy
                                WHERE PrmGroup = @PrmGroup
                                AND PrmId = @PrmId
                                AND ((CompanyId = @CompanyId) OR (CompanyId IS NULL AND @CompanyId IS NULL))
                                AND ((WorkplaceId = @WorkplaceId) OR (WorkplaceId IS NULL AND @WorkplaceId IS NULL))
                                AND (IsDeleted IS NULL OR IsDeleted = 0)
                            END
                            ELSE
                            BEGIN
                                INSERT INTO Erp_Parameter (PrmGroup, PrmId, PrmValue, /*Condition,*/ CompanyId, WorkplaceId, InsertedAt, InsertedBy, IsDeleted)
                                VALUES (@PrmGroup, @PrmId, @PrmValue/*, @Condition*/, @CompanyId, @WorkplaceId, GETDATE(), @InsertedBy, 0)
                            END";

                        using (SqlCommand cmd = new SqlCommand(upsertQuery, targetConn))
                        {
                            cmd.Parameters.AddWithValue("@PrmGroup", 32);
                            cmd.Parameters.AddWithValue("@PrmId", row["PrmId"] == DBNull.Value ? (object)DBNull.Value : row["PrmId"]);
                            
                            // PrmValue: nvarchar(max)
                            object prmValue = row["PrmValue"] == DBNull.Value ? (object)DBNull.Value : row["PrmValue"];
                            cmd.Parameters.AddWithValue("@PrmValue", prmValue);
                            
                            //// Condition: nvarchar(max) - string olarak gönderilebilir
                            //object conditionValue = row["Condition"] == DBNull.Value ? (object)DBNull.Value : row["Condition"];
                            //cmd.Parameters.AddWithValue("@Condition", conditionValue);
                            
                            cmd.Parameters.AddWithValue("@CompanyId", targetCompanyIdValue);
                            cmd.Parameters.AddWithValue("@WorkplaceId", targetWorkplaceIdValue);
                            
                            // Audit alanları: InsertedBy ve UpdatedBy - Hedef DB'deki Meta_User RecId'sini kullan
                            if (targetUserId.HasValue)
                            {
                                cmd.Parameters.AddWithValue("@InsertedBy", targetUserId.Value);
                                cmd.Parameters.AddWithValue("@UpdatedBy", targetUserId.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@InsertedBy", DBNull.Value);
                                cmd.Parameters.AddWithValue("@UpdatedBy", DBNull.Value);
                            }
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString("Parametreler başarıyla hedef veritabanına kopyalandı."), ConstantStr.Information, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Information);
            }
            catch (SqlException sqlEx)
            {
                SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString($"SQL Hatası: {sqlEx.Message}"), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SysMng.Instance.ActWndMng.ShowMsg(SLanguage.GetString($"Hata: {ex.Message}"), ConstantStr.Warning, Common.InformationMessages.MessageBoxButton.OK, Common.InformationMessages.MessageBoxImage.Warning);
            }
        }

        private string BuildConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = Parameters.TargetServer,
                InitialCatalog = Parameters.TargetDatabase,
                UserID = Parameters.TargetUserName,
                Password = Parameters.TargetPassword,
                IntegratedSecurity = false
            };

            return builder.ConnectionString;
        }
    }
}