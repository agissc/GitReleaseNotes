using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitReleaseNotes
{
    public static class RepositoryHelper
    {
        /// <summary>
        /// Get all commits of a repositories head branch and all its submodules
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="orderByTime">[Optional] Order by the time the commit was created (most recent first). Default: True</param>
        public static IList<Commit> GetCommitsRecursive(this IRepository repository, bool orderByTime = true)
        {
            var allCommits = repository.Head.Commits.ToList();

            foreach (var submodule in repository.Submodules)
            {
                allCommits.AddRange(new Repository(submodule.Path).GetCommitsRecursive(orderByTime));
            }

            return allCommits.OrderByDescending(x => x.Author.When).ToList();
        }
    }
}
