﻿using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Policies;
using Brizbee.Services;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

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
                throw new NotAuthorizedException("You are not authorized to create the commit");
            }
            
            var inAt = new DateTime(commit.InAt.Year, commit.InAt.Month, commit.InAt.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var outAt = new DateTime(commit.OutAt.Year, commit.OutAt.Month, commit.OutAt.Day, 23, 59, 59, DateTimeKind.Unspecified);

            // Ensure that no two commits overlap
            var overlap = db.Commits
                .Where(c => (inAt < c.OutAt) && (c.InAt < outAt))
                .FirstOrDefault();
            if (overlap != null)
            {
                throw new NotAuthorizedException(
                    string.Format(
                        "The commit overlaps another commit: {0} thru {1}",
                        overlap.InAt.ToString("yyyy-MM-dd"),
                        overlap.OutAt.ToString("yyyy-MM-dd"))
                    );
            }

            // Auto-generated
            commit.CreatedAt = DateTime.UtcNow;
            commit.OrganizationId = currentUser.OrganizationId;
            commit.UserId = currentUser.Id;
            commit.InAt = inAt;
            commit.OutAt = outAt;

            db.Commits.Add(commit);

            db.SaveChanges();

            // Split at midnight every night
            var splitter = new PunchSplitter();
            var midnight = new DateTime(2018, 1, 1, 0, 0, 0).Hour;
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            splitter.SplitAtHour(userIds, inAt, outAt, midnight, currentUser);

            // Commit all the punches within range and save the PunchCount
            var punches = db.Punches.Where(p => (p.InAt >= inAt) && (p.OutAt <= outAt));
            foreach (var punch in punches)
            {
                punch.CommitId = commit.Id;
            }
            commit.PunchCount = punches.Count();

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
        public IQueryable<Commit> Get(int id, User currentUser)
        {
            return db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of commits.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of commits</returns>
        public IQueryable<Commit> GetAll(User currentUser)
        {
            return db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId);
        }

        /// <summary>
        /// Updates the given commit with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the commit</param>
        /// <param name="patch">The changes that should be made to the commit</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated commit</returns>
        public Commit Update(int id, Delta<Commit> patch, User currentUser)
        {
            var commit = db.Commits.Find(id);

            // Ensure that object was found
            if (commit == null)
            {
                throw new NotFoundException("We could not find a commit with that ID");
            }

            // Ensure that user is authorized
            if (!CommitPolicy.CanUpdate(commit, currentUser))
            {
                throw new NotAuthorizedException("You are not authorized to modify the commit");
            }

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            {
                throw new NotAuthorizedException("You are not authorized to modify the OrganizationId");
            }

            // Peform the update
            patch.Patch(commit);

            db.SaveChanges();

            return commit;
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
                throw new NotFoundException("We could not find a commit with that ID");
            }
        }
    }
}