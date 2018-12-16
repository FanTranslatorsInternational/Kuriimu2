using System.Data;
using System.Data.SQLite;

namespace plugin_valkyria_chronicles
{
    /// <summary>
    /// SQLite data interchange class.
    /// </summary>
    public static class SQLiteDB
    {
        /// <summary>
        /// The connection used to connect to the database.
        /// </summary>
        public static string ConnectionString { get; set; }

        /// <summary>
        /// Gets a table of data from the SQLite database.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="isStoredProcedure"></param>
        /// <returns></returns>
        public static DataTable GetTable(string query, bool isStoredProcedure = false)
        {
            var ds = new DataSet();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand(query, connection)
                {
                    CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text
                };
                var da = new SQLiteDataAdapter { SelectCommand = cmd };

                da.Fill(ds);
            }

            return ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

    }
}
