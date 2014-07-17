using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scbot
{
    public class SitecoreDbHelper
    {
        private readonly string _connectionString;

        public SitecoreDbHelper(string sqlServer, string sqlUser, string sqlPassword)
        {
            _connectionString = string.Format("Server={0};User Id={1};Password={2}", sqlServer, sqlUser, sqlPassword);
        }

        public void CreateSqlConfigUserIfNotExists(string username, string password, IEnumerable<string> mapToDbs)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();

                using (var selectUsers = new SqlCommand(
                    string.Format("SELECT count(1) FROM sys.server_principals where name = N'{0}'", username),
                    sqlConnection
                    ))
                {
                    var loginExists = (int) selectUsers.ExecuteScalar() > 0;

                    if (loginExists)
                    {
                        return;
                    }
                }

                Console.WriteLine("Creating '{0}' SQL user...", username);

                using (var createLogin = new SqlCommand(string.Format(
                    "CREATE LOGIN [{0}] " +
                    "WITH PASSWORD=N'{1}', DEFAULT_DATABASE=[master], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF",
                    username, password),
                    sqlConnection
                    ))
                {
                    createLogin.ExecuteNonQuery();
                }

                foreach (var db in mapToDbs)
                {
                    using (var useDb = new SqlCommand(
                        string.Format("USE [{0}]", db), sqlConnection
                        ))
                    {
                        useDb.ExecuteNonQuery();
                    }
                    using (var createUser = new SqlCommand(
                        string.Format("CREATE USER [{0}] FOR LOGIN [{0}]", username), sqlConnection
                        ))
                    {
                        createUser.ExecuteNonQuery();
                    }
                    using (var addRoles = new SqlCommand(string.Format(
                        "EXEC sp_addrolemember N'db_datareader', N'{0}' " +
                        "EXEC sp_addrolemember N'db_datawriter', N'{0}' " +
                        "EXEC sp_addrolemember N'db_owner', N'{0}'",
                        username), sqlConnection
                        ))
                    {
                        addRoles.Parameters.AddWithValue("username", username);

                        addRoles.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
