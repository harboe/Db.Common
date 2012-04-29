namespace Db.Common
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using System.Linq;

    public static class Extensions
    {
        public static T AddParameter<T>(this IDbCommand cmd, string name, object value, DbType type = DbType.String, ParameterDirection direction = ParameterDirection.Input)
            where T: IDbDataParameter
        {
            return (T)AddParameter(cmd, name, value, type, direction);
        }

        public static IDbDataParameter AddParameter(this IDbCommand cmd, string name, object value, DbType type = DbType.String, ParameterDirection direction = ParameterDirection.Input)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            param.DbType = type;
            param.Direction = direction;

            cmd.Parameters.Add(param);
            return param;
        }

        public static IDictionary<string, object> ExecuteProcedure(this IDbCommand cmd, string procedureName, Action<IDbCommand> parameters)
        {
            var conn = cmd.Connection;

            try
            {
                cmd.CommandText = procedureName;
                cmd.CommandType = CommandType.StoredProcedure;

                parameters(cmd);

                conn.Open();
                cmd.ExecuteNonQuery();

                return cmd.Parameters.Cast<IDbDataParameter>()
                    .Where(x => x.Direction != ParameterDirection.Input)
                    .ToDictionary(x => x.ParameterName, y => y.Value);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        public static IEnumerable<T> SelectAll<T>(this IDbCommand cmd, Func<IDataReader, T> projection, string sql, params object[] args)
        {
            var conn = cmd.Connection;

            try
            {
                if (!string.IsNullOrEmpty(sql))
                    cmd.CommandText = string.Format(sql, args);

                conn.Open();
                return SelectAll(cmd, projection);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        public static IEnumerable<T> SelectAll<T>(this IDbCommand cmd, Func<IDataReader, T> projection)
        {
            using (var reader = cmd.ExecuteReader())
                return reader.SelectAll(projection);
        }

        public static T Select<T>(this IDbCommand cmd, Func<IDataReader, T> projection)
        {
            return SelectAll(cmd, projection).FirstOrDefault();
        }
    }

    public static class DbDataReaderExtension
    {
        public static IEnumerable<T> SelectAll<T>(this IDataReader reader, Func<IDataReader, T> projection)
        {
            while (reader.Read())
                yield return projection(reader);
        }

        public static T Get<T>(this IDataReader reader, int columnIndex, T defaultValue = default(T))
        {
            try
            {
                if (reader.IsDBNull(columnIndex))
                    return DbConvert.Change<T>(null);

                return DbConvert.Change<T>(reader.GetValue(columnIndex));
            }
            catch (IndexOutOfRangeException)
            {
                return defaultValue;
            }
        }

        public static T Get<T>(this IDataReader reader, string columnName, T defaultValue = default(T))
        {
            return Get<T>(reader, reader.GetOrdinal(columnName));
        }
    }
}