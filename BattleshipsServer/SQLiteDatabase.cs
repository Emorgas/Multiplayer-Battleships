using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace BattleshipsServer
{
    class SQLiteDatabase
    {
        string connection;

        public SQLiteDatabase()
        {
            connection = "Data Source=clientData.s3db";
        }

        public SQLiteDatabase(string fileName)
        {
            connection = string.Format("Data Source={0}", fileName);
        }

        public DataTable QueryDatabase(string sql)
        {
            DataTable table = new DataTable();
            try
            {
                SQLiteConnection con = new SQLiteConnection(connection);
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(con);
                cmd.CommandText = sql;
                SQLiteDataReader reader = cmd.ExecuteReader();
                table.Load(reader);
                reader.Close();
                con.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return table;
        }

        public int ExecuteNonQuery(string sql)
        {
            SQLiteConnection con = new SQLiteConnection(connection);
            con.Open();
            SQLiteCommand cmd = new SQLiteCommand(con);
            cmd.CommandText = sql;
            int rowsUpdated = cmd.ExecuteNonQuery();
            con.Close();
            return rowsUpdated;
        }

        public string GetSingleEntry(string sql)
        {
            SQLiteConnection con = new SQLiteConnection(connection);
            con.Open();
            SQLiteCommand cmd = new SQLiteCommand(con);
            cmd.CommandText = sql;
            object value = cmd.ExecuteScalar();
            con.Close();
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }

        public bool Update(string tableName, Dictionary<string, string> data, string where)
        {
            string vals = "";
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<string, string> val in data)
                {
                    vals += string.Format(" {0} = '{1'},", val.Key.ToString(), val.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                this.ExecuteNonQuery(string.Format("update {0} set {1} where {2};", tableName, vals, where));
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Delete(string tableName, string where)
        {
            try
            {
                this.ExecuteNonQuery(string.Format("delete from {0} where {1};", tableName, where));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public bool Insert(string tableName, Dictionary<string, string> data)
        {
            string columns = "";
            string values = "";
            
            foreach (KeyValuePair<string, string> val in data)
            {
                columns += string.Format(" {0},", val.Key.ToString());
                values += string.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                this.ExecuteNonQuery(string.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
    }
}
