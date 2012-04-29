namespace Db.Common
{
    using System;
    using System.Data.SqlClient;

    public enum Order
    {
        ASC,
        DESC
    }

    public class PaginatorFilter
    {
        #region Field Members

        private int page;

        #endregion

        #region Constructor Members

        public PaginatorFilter() { Page = 0; PageSize = 15; OrderBy = ""; }

        #endregion

        #region Property Members

        public string OrderBy { get; set; }
        public Order Order { get; set; }
        public int PageSize { get; set; }
        public int Page
        {
            get { return page <= 0 ? 1 : page; }
            set { page = value; }
        }

        public int Skip
        {
            get 
            { 
                int skip = (Page - 1) * PageSize;

                if (skip > 0)
                    skip++;

                return skip;
            }
        }

        public int Take
        {
            get 
            {
                int take = Skip + PageSize;

                //if (Skip > 0)
                //    take--;

                return take;
            }
        }

        #endregion
    }

    public interface ISqlPaginator
    {
        string Apply(string sql, PaginatorFilter filter);
    }

    public class OracleSqlPaginator : ISqlPaginator
    {
        #region ISqlPaginatorProvider Members

        public string Apply(string sql, PaginatorFilter filter)
        {
            return Pagineted(OrderBy(sql, filter), filter);
        }

        #endregion

        #region Method Members

        private static string Pagineted(string sql, PaginatorFilter filter)
        {
            if (filter == null || filter.PageSize <= 0)
                return sql;

            sql = @"SELECT * FROM (SELECT v.*, ROWNUM rn FROM (" + sql +
                ") v WHERE rownum <= " + (filter.Skip > 0 ? filter.Take - 1 : filter.Take) + ") WHERE rn >= " + filter.Skip;
            return sql;
        }

        private static string OrderBy(string sql, PaginatorFilter filter)
        {
            if (filter == null || string.IsNullOrEmpty(filter.OrderBy))
                return sql;

            sql += string.Format(" ORDER BY \"{0}\" {1}", filter.OrderBy.ToUpper(), filter.Order.ToString());
            return sql;
        }

        #endregion
    }
}
