using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CredentialManagement;

namespace scbot
{
    public class CredentialStorage
    {
        private readonly string _storageName;

        public CredentialStorage()
        {
            _storageName = Assembly.GetExecutingAssembly().GetName().Name;
        }

        public Credentials GetSavedCredentials()
        {
            var credentials = new Credential {Target = _storageName};

            if (credentials.Exists())
            {
                credentials.Load();

                return new Credentials
                {
                    Username = credentials.Username,
                    Password = credentials.Password
                };
            }

            return null;
        }

        public void SaveCredentials(string username, string password)
        {
            var credentials = new Credential(username, password, _storageName, CredentialType.Generic);
            credentials.Save();
        }
    }
}
