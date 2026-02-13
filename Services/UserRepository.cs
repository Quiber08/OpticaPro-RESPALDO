using OpticaPro.Models;
using System.Collections.Generic;
using System.Linq;

namespace OpticaPro.Services
{
    public static class UserRepository
    {
        // Constructor: Asegura que la tabla exista
        static UserRepository()
        {
            var db = DatabaseService.GetConnection();
            db.CreateTable<AppUser>();
        }

        public static List<AppUser> GetAllUsers()
        {
            var db = DatabaseService.GetConnection();
            return db.Table<AppUser>().ToList();
        }

        public static void AddUser(AppUser user)
        {
            var db = DatabaseService.GetConnection();
            db.Insert(user);
        }

        public static void UpdateUser(AppUser user)
        {
            var db = DatabaseService.GetConnection();
            db.Update(user);
        }

        public static void DeleteUser(int id)
        {
            var db = DatabaseService.GetConnection();
            db.Delete<AppUser>(id);
        }
    }
}