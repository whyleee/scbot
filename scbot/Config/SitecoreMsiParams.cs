using System;
using System.Collections.Generic;
using System.Linq;

namespace scbot.Config
{
    // Copy of SitecoreInstallWizardCore.Constants.PublicConstants,
    // but using 'const' instead of 'readonly' fields
    public class SitecoreMsiParams
    {
        public const string FullMode = "SC_FULL";
        public const string ClientOnlyMode = "SC_CLIENTONLY";
        public const string DatabaseOnlyMode = "SC_DBONLY";
        public const string SkipConfigureIis = "SKIPCONFIGUREIIS";
        public const string SkipInstallSqlData = "SKIPINSTALLSQLDATA";
        public const string SkipUninstallSqlData = "SKIPUNINSTALLSQLDATA";
        public const string LicensePath = "SC_LICENSE_PATH";
        public const string InstanceName = "SC_INSTANCENAME";
        public const string DatabaseType = "SC_DBTYPE";
        public const string OracleClient = "SC_ORACLE_CLIENT";
        public const string OracleTablespace = "SC_ORACLE_TABLESPACENAME";
        public const string OracleInstance = "SC_ORACLE_INSTANCE";
        public const string OracleSystemPassword = "SC_ORACLE_SYSTEMPASSWORD";
        public const string OraclePrefix = "SC_ORACLE_PREFIX";
        public const string OracleDataAccessVersion = "SC_ORACLE_DATAACCESS_VERSION";
        public const string OracleVersion = "SC_ORACLE_VERSION";
        public const string OracleTablespaceSpace = "SC_ORACLETABLESPACE_SPACE";
        public const string OracleDllPath = "SC_ORACLEDLL_PATH";
        public const string SqlServer = "SC_SQL_SERVER";
        public const string SqlServerUser = "SC_SQL_SERVER_USER";
        public const string SqlServerPassword = "SC_SQL_SERVER_PASSWORD";
        public const string SqlDbPrefix = "SC_DBPREFIX";
        public const string SqlServerConfigUser = "SC_SQL_SERVER_CONFIG_USER";
        public const string SqlServerConfigPassword = "SC_SQL_SERVER_CONFIG_PASSWORD";
        public const string SqlPrefixPhysicalFiles = "SC_PREFIX_PHYSICAL_FILES";
        public const string Installlocation = "INSTALLLOCATION";
        public const string DataFolder = "SC_DATA_FOLDER";
        public const string DatabaseFolder = "SC_DB_FOLDER";
        public const string MdfFilesFolder = "SC_MDF_FOLDER";
        public const string LdfFilesFolder = "SC_LDF_FOLDER";
        public const string IisSiteName = "SC_IISSITE_NAME";
        public const string IisAppPoolName = "SC_IISAPPPOOL_NAME";
        public const string IisSitePort = "SC_IISSITE_PORT";
        public const string IisSiteHeader = "SC_IISSITE_HEADER";
        public const string IisSiteID = " SC_IISSITE_ID";
        public const string NetVersion = "SC_NET_VERSION";
        public const string IntegratedPipelineMode = "SC_INTEGRATED_PIPELINE_MODE";

        // Extra params (not in original SitecoreInstallWizardCore.Constants.PublicConstants)
        public const string Language = "SC_LANG";

        // Common MSI params
        public const string MsiTransforms = "TRANSFORMS";
        public const string MsiNewInstance = "MSINEWINSTANCE";
        public const string MsiLogVerbose = "LOGVERBOSE";

        // Custom params
        public const string SitecoreVersion = "SC_VERSION";
    }
}
