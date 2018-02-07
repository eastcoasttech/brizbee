﻿using Brizbee.Common.Models;
using System;
using System.Linq;

namespace Brizbee.Repositories
{
    public class PunchRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given punch in the database.
        /// </summary>
        /// <param name="punch">The punch to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created punch</returns>
        public Punch Create(Punch punch, User currentUser)
        {
            // Auto-generated
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

        /// <summary>
        /// Creates a punch in the database for the given user with the
        /// given task and current timestamp.
        /// </summary>
        /// <param name="taskId">The id of the task</param>
        /// <param name="currentUser">The user to punch in</param>
        public void PunchIn(int taskId, User currentUser)
        {
            var punch = new Punch();

            // Auto-generated
            punch.CreatedAt = DateTime.Now;
            punch.TaskId = taskId;
            punch.UserId = currentUser.Id;

            db.Punches.Add(punch);

            db.SaveChanges();
        }

        /// <summary>
        /// Finds the most recent punch in the database which does not
        /// have an "out" timestamp, and updates the punch's out value
        /// to be the current timestamp.
        /// </summary>
        /// <param name="currentUser">The user to punch out</param>
        public void PunchOut(User currentUser)
        {
            var punch = db.Punches.Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            punch.OutAt = DateTime.Now;

            db.SaveChanges();
        }
    }
}