using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace scbot.Repo
{
    public class SitecoreSdnClient
    {
        private const string SDN_LOGIN_PAGE_URL = "http://sdn.sitecore.net/sdn5/misc/loginpage.aspx";
        private const string SDN_DOWNLOAD_PAGE_URL = "http://sdn.sitecore.net/Resources/Sitecore%207/Sitecore%20CMS.aspx";

        private readonly WebClient _client = new CookieAwareWebClient();

        public bool Login(string username, string password)
        {
            _client.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");

            var postData = string.Format("__EVENTTARGET=ctl09%24loginButton" +
                "&__EVENTARGUMENT=" +
                "&__VIEWSTATE=%2FwEPDwUKLTQ5MTgxOTU2Mw9kFgQCAQ9kFgRmD2QWAmYPFQEpV2VsY29tZSB0byB0aGUgU2l0ZWNvcmUgRGV2ZWxvcGVyIE5ldHdvcmtkAgMPZBYCZg9kFgJmDxUDAAQyMDEyJntFRjBGQTFCOC1EOEY0LTQ1QUUtQUNDMi0yNzY0MkUwMDZBOTB9ZAIDD2QWAmYPZBYCZg9kFgQCAw9kFgZmDxUBEmN0bDA5X2VtYWlsVGV4dEJveGQCAg8VARVjdGwwOV9wYXNzd29yZFRleHRCb3hkAgUPEA9kFgIeBVN0eWxlBRBwYWRkaW5nLWxlZnQ6M3B4ZGRkAgUPZBYCZg8VAQlBbm9ueW1vdXNkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBRZjdGwwOSRyZW1lbWJlckNoZWNrQm94YmPxi3RYRUNBC%2Fb%2FUzEwJONqIMw%3D" +
                "&__EVENTVALIDATION=%2FwEWBgLOl6mwBQKp6LiWDQLojvFeAqz1%2BvEIAqfsgJAJArO1k4gKEwLSnEjULwt66ogDP9QmZ7eeG4o%3D" +
                "&SearchButton=" +
                "&ctl09%24emailTextBox={0}" +
                "&ctl09%24passwordTextBox={1}" +
                "&ctl09%24rememberCheckBox=on",
                WebUtility.UrlEncode(username), WebUtility.UrlEncode(password)
            );

            var resultHtml = _client.UploadString(SDN_LOGIN_PAGE_URL, postData);
            var doc = new HtmlDocument();
            doc.LoadHtml(resultHtml);

            var loggedIn = doc.DocumentNode.QuerySelector(".loggedin_area") != null;
            return loggedIn;
        }

        public SitecorePackage GetSitecorePackage(string version = null, SitecorePackageType packageType = SitecorePackageType.ExeInstaller)
        {
            // Find a link for the latest version
            var overviewHtml = _client.DownloadString(SDN_DOWNLOAD_PAGE_URL);

            var doc = new HtmlDocument();
            doc.LoadHtml(overviewHtml);

            var recommenedOverviewUrl = doc.DocumentNode
                .QuerySelectorAll(".centercontainer .maincontainer .maincolumnwide ul")
                .First()
                .QuerySelector("a")
                .Attributes["href"]
                .Value;

            string packageTypeSuffix = null;

            if (packageType == SitecorePackageType.ExeInstaller)
            {
                packageTypeSuffix = "exe";
            }
            else if (packageType == SitecorePackageType.ZippedExeInstaller)
            {
                packageTypeSuffix = "exe_zip";
            }
            else if (packageType == SitecorePackageType.ZippedWebsite)
            {
                packageTypeSuffix = "";
            }

            string urlVersion;

            if (version != null)
            {
                if (!version.Contains("rev"))
                {
                    var secondPointIndex = IndexOfNth(version, '.', 2);
                    if (secondPointIndex != -1)
                    {
                        version = version.Insert(secondPointIndex, " rev");
                    }
                }

                urlVersion = version.Replace(" ", "").Replace(".", "");
            }
            else
            {
                urlVersion = recommenedOverviewUrl
                    .Substring(recommenedOverviewUrl.IndexOf("/Update/") + "/Update/".Length)
                    .Replace("_", "").Replace(".aspx", "");
            }

            if (!Regex.IsMatch(urlVersion, @"^\d{2}rev\d{6}$"))
            {
                Console.WriteLine("ERROR: invalid Sitecore version '{0}'. Correct format: '{1}' or '{2}'",
                    version ?? urlVersion, "7.2 rev. 123456", "7.2.123456");
                Environment.Exit(-1);
            }

            var downloadUrl = string.Format("http://sdn.sitecore.net/downloads/Sitecore{0}{1}.download", urlVersion, packageTypeSuffix);
            var displayVersion = urlVersion.Insert(1, ".").Replace("rev", " rev. ");

            return new SitecorePackage
            {
                Version = displayVersion,
                Type = packageType,
                DownloadUrl = downloadUrl
            };
        }

        public void DownloadFile(string url, string to)
        {
            try
            {
                _client.DownloadFile(url, to);
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    if (((HttpWebResponse) ex.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("ERROR: " + ex.Message);
                        Environment.Exit(-1);
                    }
                }

                throw;
            }
            
        }

        private static int IndexOfNth(string str, char c, int n)
        {
            int s = -1;

            for (int i = 0; i < n; i++)
            {
                s = str.IndexOf(c, s + 1);

                if (s == -1) break;
            }

            return s;
        }
    }
}
