using Brizbee.Common.Models;
using Brizbee.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class CommitRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given commit in the database.
        /// </summary>
        /// <param name="commit">The commit to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created commit</returns>
        public Commit Create(Commit commit, User currentUser)
        {
            // Ensure that user is authorized
            if (!CommitPolicy.CanCreate(commit, currentUser))
            {
                throw new Exception("Not authorized to create the object");
            }

            // Auto-generated
            commit.CreatedAt = DateTime.Now;
            commit.OrganizationId = currentUser.OrganizationId;
            commit.UserId = currentUser.Id;

            // Commit all the punches within range
            db.Punches.Where(p => (p.InAt >= commit.InAt) && (p.OutAt <= commit.OutAt));

            db.Commits.Add(commit);

            db.SaveChanges();

            return commit;
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the commit with the given id.
        /// </summary>
        /// <param name="id">The id of the commit</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The commit with the given id</returns>
        public Commit Get(int id, User currentUser)
        {
            return db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of commits.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of commits</returns>
        public IQueryable<Commit> GetAll(User currentUser)
        {
            return db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .AsQueryable<Commit>();
        }

        /// <summary>
        /// Deletes the commit from the database and clears the relationship
        /// that all the committed punches had with this commit.
        /// </summary>
        /// <param name="id">The id of the commit to undo</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Undo(int id, User currentUser)
        {
            var commit = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);

            if (commit != null)
            {
                var punches = db.Punches.Where(p => p.CommitId == commit.Id).ToList();
                punches.ForEach(p => {
                    p.CommitId = null;
                });

                db.Commits.Remove(commit);

                db.SaveChanges();
            }
            else
            {
                throw new Exception("The object with that ID does not exist");
            }
        }
    }
}