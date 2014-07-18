namespace scbot.Repo
{
    public class SitecorePackage
    {
        public SitecorePackage()
        {
            LocalPaths = new SitecorePackagePaths();
        }

        public string Version { get; set; }

        public SitecorePackageType Type { get; set; }

        public string DownloadUrl { get; set; }

        public SitecorePackagePaths LocalPaths { get; set; }

        public bool Corrupted { get; set; }
    }

    public class SitecorePackagePaths
    {
        public string PackageDir { get; set; }

        public string InstallerPath { get; set; }

        public string MsiPath { get; set; }

        public string WizardPath { get; set; }
    }
}