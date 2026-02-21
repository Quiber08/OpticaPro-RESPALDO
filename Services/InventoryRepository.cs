using System;
using System.Collections.Generic;
using System.Linq;
using OpticaPro.Models;

namespace OpticaPro.Services
{
    public static class InventoryRepository
    {
        // Constructor estático: Inicializa la BD al primer uso
        static InventoryRepository()
        {
            DatabaseService.Initialize();
        }

        public static List<Product> GetAllProducts()
        {
            var db = DatabaseService.GetConnection();
            if (db == null) return new List<Product>(); // Protección extra
            return db.Table<Product>().ToList();
        }

        public static void AddProduct(Product p)
        {
            var db = DatabaseService.GetConnection();
            db?.Insert(p);
        }

        public static void UpdateProduct(Product p)
        {
            var db = DatabaseService.GetConnection();
            db?.Update(p);
        }

        public static void DeleteProduct(Product p)
        {
            var db = DatabaseService.GetConnection();
            db?.Delete(p);
        }

        public static void SaveProducts(List<Product> newProducts)
        {
            var db = DatabaseService.GetConnection();
            if (db == null) return;

            db.RunInTransaction(() =>
            {
                db.DeleteAll<Product>();
                db.InsertAll(newProducts);
            });
        }
    }
}