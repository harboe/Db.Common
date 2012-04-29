namespace Db.Common
{
    using System;
    using System.Data;
    using System.Data.Common;

    using System.Configuration;
    using System.Linq;
    using System.Collections.Generic;

    using System.Diagnostics;

    public abstract class Repository
    {
        #region Field Members

        static DbProviderFactory _factory;
        static string _providerName, _connString;
        static bool _isInitialized;

        ISqlPaginator _paginator;

        #endregion

        #region Constructor Members

        /// <summary>
        /// Initialize a new instance of the <c>Repository</c> class.
        /// </summary>
        protected Repository(string connName = "") : this(new OracleSqlPaginator()) { }
        /// <summary>
        /// Initialize a new instance of the <c>Repository</c> class.
        /// </summary>
        protected Repository(ISqlPaginator paginator, string connName = "")
        {
            _paginator = paginator;

            if (!_isInitialized)
            {
                connName = InitializeConnection(connName);

                Trace.WriteLine("Database Connection:");
                Trace.WriteLine(string.Format(" - Name: {0}.\n - Connection: {1}.\n - Provider: {2}",
                    connName, _connString, _providerName));

                _isInitialized = true;
            }
        }

        private static string InitializeConnection(string connName)
        {
            var connList = ConfigurationManager.ConnectionStrings;

            if (string.IsNullOrEmpty(connName))
                connName = connList[0].Name;

            if (connList[connName] != null)
            {
                if (!string.IsNullOrEmpty(connList[connName].ProviderName))
                    _providerName = connList[connName].ProviderName;
            }
            else
                throw new InvalidOperationException("Can't find a connectionstring with the name '" + connName + "'.");

            _factory = DbProviderFactories.GetFactory(_providerName);
            _connString = connList[connName].ConnectionString;
            return connName;
        }

        #endregion

        #region Select Members

        /// <summary>
        /// 
        /// </summary>
        /// <example>
        /// <![CDATA[
        ///     return SelectAll(x => new ProductModel { Id = x.Get<int>("id"), Name = x.Get<string>("name") }, "SELECT * FROM products");
        /// ]]>
        /// </example>
        protected IEnumerable<TModel> SelectAll<TModel>(Func<IDataReader, TModel> projection, string sql, params object[] args)
        {
            using (var cmd = CreateCommand(sql, args))
                return cmd.SelectAll(projection);
        }
        
        protected IEnumerable<TModel> SelectAll<TModel>(Func<IDataReader, TModel> projection, PaginatorFilter filter, string sql, params object[] args)
        {
            if (_paginator == null || filter == null)
                return SelectAll(projection, sql, args);

            return SelectAll(projection, _paginator.Apply(sql, filter), args);
        }

        /// <example>
        /// <![CDATA[
        ///     return Select(x => new ProductModel { Id = x.Get<int>("id"), Name = x.Get<string>("name") }, "SELECT * FROM products WHERE id = {0}", id);
        /// ]]>
        /// </example>
        protected TModel Select<TModel>(Func<IDataReader, TModel> projection, string sql, params object[] args)
        {
            return SelectAll<TModel>(projection, sql, args).FirstOrDefault();
        }

        #endregion

        #region Execute Members

        protected bool Execute(string sql, params object[] args)
        {
            Func<IDbCommand, bool> action = c => c.ExecuteNonQuery() > 0 ? true : false;
            return Execute(action, sql, args);
        }

        /// <summary>
        /// Execute sql query, but lets the readerAction determine the prober action. 
        /// This can be very useful for populating or modifing existing objects for instance a list
        /// </summary>
        /// <param name="readerAction"></param>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        protected bool Execute(Action<IDataReader> readerAction, string sql, params object[] args)
        {
            Func<IDbCommand, bool> action = c => 
            {
                using (var reader = c.ExecuteReader())
                    readerAction(reader);

                return true;
            };

            return Execute(action, sql, args);            
        }

        protected T Scalar<T>(string sql, params object[] args)
        {
            Func<IDbCommand, T> action = c => DbConvert.Change<T>(c.ExecuteScalar()); 
            return Execute(action, sql, args);
        }

        protected IDictionary<string, object> Procedure(string procedureName, Action<IDbCommand> parameters)
        {
            using (var cmd = CreateCommand(procedureName))
                return cmd.ExecuteProcedure(procedureName, parameters);
        }

        private T Execute<T>(Func<IDbCommand, T> cmdAction, string sql, params object[] args)
        {
            using (var conn = GetConnection())
            using (var cmd = CreateCommand(conn, sql, args))
            {
                try
                {
                    conn.Open();
                    return cmdAction(cmd);
                }
                catch (DbException ex)
                {
                    Error(ex, "failed execution: {0}", cmd.CommandText);
                    throw ex;
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                        conn.Close();
                }
            }
        }

        #endregion

        #region Method Members

        protected virtual IDbConnection GetConnection()
        {
            var conn = _factory.CreateConnection();
            conn.ConnectionString = _connString;

            return conn;
        }

        protected virtual IDbTransaction BeginTransaction(IDbCommand cmd)
        {
            var conn = cmd.Connection;

            if (conn == null)
            {
                conn = GetConnection();
                cmd.Connection = conn;
            }

            if (conn.State != ConnectionState.Open)
                conn.Open();

            return conn.BeginTransaction();
        }

        protected IDbCommand CreateCommand(string sql = "", params object[] args)
        {
            using(var conn = GetConnection())
                return CreateCommand(conn, sql, args);
        }

        protected virtual IDbCommand CreateCommand(IDbConnection conn, string sql = "", params object[] args)
        {
            if (conn == null)
                throw new ArgumentNullException("conn");

            var cmd = conn.CreateCommand();

            if (string.IsNullOrEmpty(sql))
                cmd.CommandText = string.Format(sql, args);

            return cmd;
        }

        protected void Error(string format, params object[] args) { Error(null, format, args); }
        protected virtual void Error(Exception ex, string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));

            if (ex != null)
                Trace.WriteLine(ex.Message);
        }

        protected void Info(string format, params object[] args) { Error(null, format, args); }
        protected virtual void Info(Exception ex, string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));

            if (ex != null)
                Trace.WriteLine(ex.Message);
        }

        #endregion
    }
}
