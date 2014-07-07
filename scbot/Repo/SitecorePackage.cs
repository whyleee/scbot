namespace scbot.Repo
{
    public class SitecorePackage
    {
        public string Version { get; set; }

        public SitecorePackageType Type { get; set; }

        public string DownloadUrl { get; set; }

        public SitecorePackagePaths LocalPaths { get; set; }
    }

    public class SitecorePackagePaths
    {
        public string MsiPath { get; set; }

        public string WizardPath { get; set; }
    }
}