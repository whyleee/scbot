using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace scbot.Repo
{
    public class CookieAwareWebClient : WebClient
    {
        private readonly CookieContainer _cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = _cookies;
            }

            return request;
        }
    }
}
