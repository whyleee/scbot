using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace scbot.Repo
{
    public class SitecoreSdnClient
    {
        private const string SDN_LOGIN_PAGE_URL = "http://sdn.sitecore.net/sdn5/misc/loginpage.aspx";

        private readonly WebClient _client = new CookieAwareWebClient();

        public void Login(string username, string password)
        {
            _client.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            //var postData = string.Format("__EVENTTARGET=ctl09%24loginButton&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUKLTQ5MTgxOTU2Mw9kFgQCAQ9kFgRmD2QWAmYPFQEpV2VsY29tZSB0byB0aGUgU2l0ZWNvcmUgRGV2ZWxvcGVyIE5ldHdvcmtkAgMPZBYCZg9kFgJmDxUDAAQyMDEyJntFRjBGQTFCOC1EOEY0LTQ1QUUtQUNDMi0yNzY0MkUwMDZBOTB9ZAIDD2QWAmYPZBYCZg9kFgQCAw9kFgZmDxUBEmN0bDA5X2VtYWlsVGV4dEJveGQCAg8VARVjdGwwOV9wYXNzd29yZFRleHRCb3hkAgUPEA9kFgIeBVN0eWxlBRBwYWRkaW5nLWxlZnQ6M3B4ZGRkAgUPZBYCZg8VAQlBbm9ueW1vdXNkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBRZjdGwwOSRyZW1lbWJlckNoZWNrQm94YmPxi3RYRUNBC%2Fb%2FUzEwJONqIMw%3D&__EVENTVALIDATION=%2FwEWBgLOl6mwBQKp6LiWDQLojvFeAqz1%2BvEIAqfsgJAJArO1k4gKEwLSnEjULwt66ogDP9QmZ7eeG4o%3D&SearchButton=&ctl09%24emailTextBox={0}&ctl09%24passwordTextBox={1}&ctl09%24rememberCheckBox=on", WebUtility.UrlEncode(username), WebUtility.UrlEncode(password));
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

            var login = _client.UploadString(SDN_LOGIN_PAGE_URL, postData);
        }

        public SitecorePackage GetLatestSitecorePackage(SitecorePackageType packageType = SitecorePackageType.ExeInstaller)
        {
            // Find a link for recommended version
            var overviewHtml = _client.DownloadString("http://sdn.sitecore.net/Resources/Sitecore%207/Sitecore%20CMS.aspx");

            var doc = new HtmlDocument();
            doc.LoadHtml(overviewHtml);

            var recommenedOverviewUrl = doc.DocumentNode
                .QuerySelectorAll(".centercontainer .maincontainer .maincolumnwide ul")
                .First()
                .QuerySelector("a")
                .Attributes["href"]
                .Value;

            var version = recommenedOverviewUrl.Substring(recommenedOverviewUrl.IndexOf("/Update/") + "/Update/".Length).Replace("_", "").Replace(".aspx", "");
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

            var downloadUrl = string.Format("http://sdn.sitecore.net/downloads/Sitecore{0}{1}.download", version, packageTypeSuffix);
            var displayVersion = version.Insert(1, ".").Replace("rev", " rev. ");

            return new SitecorePackage
            {
                Version = displayVersion,
                Type = packageType,
                DownloadUrl = downloadUrl
            };
        }

        public void DownloadFile(string url, string to)
        {
            _client.DownloadFile(url, to);
        }
    }
}
