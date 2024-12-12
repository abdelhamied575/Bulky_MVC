using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    internal class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public void Update(Product product)
        {
            var modelFromDb=_db.Products.FirstOrDefault(u=>u.Id== product.Id);

            if(modelFromDb!=null)
            {
                // Manual Mapping
                modelFromDb.Title = product.Title;
                modelFromDb.Description = product.Description;
                modelFromDb.CategoryId = product.CategoryId;
                modelFromDb.ListPrice = product.ListPrice;
                modelFromDb.Price = product.Price;
                modelFromDb.Price50 = product.Price50;
                modelFromDb.Price100 = product.Price100;
                modelFromDb.ISBN = product.ISBN;
                modelFromDb.Author = product.Author;
                if(product.ImageUrl is not null)
                {
                    modelFromDb.ImageUrl = product.ImageUrl;
                }
            }


        }
    }
}
