Db.Common
=========

Small .net library contains common Db extension methods and a RepositoryBase

Sample
======

    public enum ProductType
    {
        UnknownEntity,
        Toy,
        Cloth
    }

    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public ProductType ProductType { get; set; }
    }

    public interface IProductRepository
    {
        IEnumerable<ProductModel> FindAll();
        ProductModel Find(int id);
    }

    public class ProductRepository : Repository, IProductRepository
    {
        #region Projection

        private Func<IDataReader, ProductModel> productProjection = (r) =>
        {
            return new ProductModel
            {
                Id = r.Get<int>("id"),
                Name = r.Get<string>("name"),
                Price = r.Get<double>("price"),
                ProductType = r.Get<ProductType>("type")
            };
        };

        #endregion

        #region IProductRepository Members

        public IEnumerable<ProductModel> FindAll()
        {
            return SelectAll(productProjection, "SELECT * FROM products");
        }

        public ProductModel Find(int id)
        {
            return Select(productProjection, "SELECT * FROM products WHERE id = {0}", id);
        }

        #endregion
    }