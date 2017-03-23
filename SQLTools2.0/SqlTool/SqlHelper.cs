namespace SqlTool
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Runtime.InteropServices;

    public class SqlHelper
    {
        private string connectionString = string.Empty;

        public SqlHelper(string sConnectionString)
        {
            this.connectionString = sConnectionString;
        }

        public int ExecuteNonQuery(string sSql)
        {
            int num = 0;
            if (!string.IsNullOrEmpty(this.connectionString))
            {
                using (SqlConnection connection = new SqlConnection(this.connectionString))
                {
                    connection.Open();
                    sSql = sSql.Replace(" 上午 ", " ");
                    sSql = sSql.Replace(" 下午 ", " ");
                    sSql = sSql.Replace(" AM ", " ");
                    sSql = sSql.Replace(" PM ", " ");
                    num = new SqlCommand(sSql, connection).ExecuteNonQuery();
                }
            }
            return num;
        }

        public SqlDataReader ExecuteReader(string sSql, out SqlConnection connSql)
        {
            connSql = new SqlConnection(this.connectionString);
            if (string.IsNullOrEmpty(this.connectionString))
            {
                return null;
            }
            connSql.Open();
            SqlCommand command = new SqlCommand(sSql, connSql);
            return command.ExecuteReader();
        }

        public object ExecuteScalar(string sSql)
        {
            if (string.IsNullOrEmpty(this.connectionString))
            {
                return null;
            }
            using (SqlConnection connection = new SqlConnection(this.connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sSql, connection);
                return command.ExecuteScalar();
            }
        }

        public DataRow GetDataRow(string sSql)
        {
            DataRow row = null;
            if (!string.IsNullOrEmpty(this.connectionString))
            {
                using (SqlConnection connection = new SqlConnection(this.connectionString))
                {
                    DataTable dataTable = new DataTable();
                    new SqlDataAdapter(sSql, connection).Fill(dataTable);
                    if (dataTable.Rows.Count != 0)
                    {
                        row = dataTable.Rows[0];
                    }
                }
            }
            return row;
        }

        public DataTable GetDataTable(string sSql)
        {
            DataTable dataTable = null;
            if (!string.IsNullOrEmpty(this.connectionString))
            {
                using (SqlConnection connection = new SqlConnection(this.connectionString))
                {
                    dataTable = new DataTable();
                    new SqlDataAdapter(sSql, connection).Fill(dataTable);
                }
            }
            return dataTable;
        }
    }
}

