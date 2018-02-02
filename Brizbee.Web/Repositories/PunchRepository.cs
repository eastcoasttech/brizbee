using Brizbee.Common.Models;
using System;
using System.Linq;

namespace Brizbee.Repositories
{
    public class PunchRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given punch in the repository.
        /// </summary>
        /// <param name="punch">The punch to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created punch</returns>
        public Punch Create(Punch punch, User currentUser)
        {
            punch.CreatedAt = DateTime.Now;
            db.Punches.Add(punch);

            db.SaveChanges();

            return punch;
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the punch with the given id.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The punch with the given id</returns>
        public Punch Get(int id, User currentUser)
        {
            return db.Punches.Find(id);
        }

        /// <summary>
        /// Returns a queryable collection of punches.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of punches</returns>
        public IQueryable<Punch> GetAll(User currentUser)
        {
            return db.Punches.AsQueryable<Punch>();
        }
    }
}