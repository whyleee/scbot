namespace scbot.Config
{
    public interface IConfigWriter
    {
        void WriteConfig(string configPath, SitecoreInstallParameters config);
    }
}