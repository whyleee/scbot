using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace scbot.Config
{
    [DataContract]
    public class SitecoreInstallParameters
    {
        // General settings

        [DataMember(Name = SitecoreMsiParams.InstanceName)]
        public string InstanceName { get; set; }

        [DataMember(Name = SitecoreMsiParams.SitecoreVersion)]
        public string SitecoreVersion { get; set; }

        [DataMember(Name = SitecoreMsiParams.Language)]
        public string Language { get; set; }

        [DataMember(Name = SitecoreMsiParams.LicensePath)]
        public string LicensePath { get; set; }


        // Install mode settings

        [DataMember(Name = SitecoreMsiParams.FullMode)]
        [DefaultValue(false)]
        public bool InstallFullMode { get; set; }

        [DataMember(Name = SitecoreMsiParams.DatabaseOnlyMode)]
        [DefaultValue(false)]
        public bool InstallDbMode { get; set; }

        [DataMember(Name = SitecoreMsiParams.ClientOnlyMode)]
        [DefaultValue(false)]
        public bool InstallClientMode { get; set; }

        [DataMember(Name = SitecoreMsiParams.SkipConfigureIis)]
        [DefaultValue(false)]
        public bool SkipConfigureIis { get; set; }

        [DataMember(Name = SitecoreMsiParams.SkipInstallSqlData)]
        [DefaultValue(false)]
        public bool SkipInstallSqlData { get; set; }

        [DataMember(Name = SitecoreMsiParams.SkipUninstallSqlData)]
        [DefaultValue(false)]
        public bool SkipUninstallSqlData { get; set; }

        public void SetInstallMode(InstallMode mode)
        {
            if (mode == InstallMode.Full)
            {
                InstallFullMode = true;
            }
            else if (mode == InstallMode.Db)
            {
                InstallDbMode = true;
                SkipConfigureIis = true;
            }
            else if (mode == InstallMode.Client)
            {
                InstallClientMode = true;
                SkipInstallSqlData = true;
                SkipUninstallSqlData = true;
            }
        }


        // Install paths

        [DataMember(Name = SitecoreMsiParams.Installlocation)]
        public string InstallFolder { get; set; }

        [DataMember(Name = SitecoreMsiParams.DataFolder)]
        public string DataFolder { get; set; }

        [DataMember(Name = SitecoreMsiParams.DatabaseFolder)]
        public string DbFolder { get; set; }

        [DataMember(Name = SitecoreMsiParams.MdfFilesFolder)]
        public string DbMdfFolder { get; set; }

        [DataMember(Name = SitecoreMsiParams.LdfFilesFolder)]
        public string DbLdfFolder { get; set; }


        // SQL settings

        [DataMember(Name = SitecoreMsiParams.DatabaseType)]
        public string DbType { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlServer)]
        public string SqlServer { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlServerUser)]
        public string SqlServerUser { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlServerPassword)]
        public string SqlServerPassword { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlDbPrefix)]
        public string SqlDbPrefix { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlPrefixPhysicalFiles)]
        [DefaultValue(false)]
        public bool SqlPrefixPhysicalFiles { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlServerConfigUser)]
        public string SqlServerConfigUser { get; set; }

        [DataMember(Name = SitecoreMsiParams.SqlServerConfigPassword)]
        public string SqlServerConfigPassword { get; set; }


        // IIS settings

        [DataMember(Name = SitecoreMsiParams.NetVersion)]
        public string NetVersion { get; set; }

        [DataMember(Name = SitecoreMsiParams.IisSiteName)]
        public string IisSiteName { get; set; }

        [DataMember(Name = SitecoreMsiParams.IisAppPoolName)]
        public string IisAppPoolName { get; set; }

        [DataMember(Name = SitecoreMsiParams.IntegratedPipelineMode)]
        [DefaultValue(false)]
        public bool IisIntegratedPipelineMode { get; set; }

        [DataMember(Name = SitecoreMsiParams.IisSiteHeader)]
        public string IisSiteHostname { get; set; }

        [DataMember(Name = SitecoreMsiParams.IisSitePort)]
        public string IisSitePort { get; set; }

        [DataMember(Name = SitecoreMsiParams.IisSiteID)]
        public string IisSiteId { get; set; }


        // Common MSI params

        [DataMember(Name = SitecoreMsiParams.MsiTransforms)]
        public string MsiTransforms { get; set; }

        [DataMember(Name = SitecoreMsiParams.MsiNewInstance)]
        [DefaultValue(false)]
        public bool MsiNewInstance { get; set; }

        [DataMember(Name = SitecoreMsiParams.MsiLogVerbose)]
        [DefaultValue(false)]
        public bool MsiLogVerbose { get; set; }
    }
}
