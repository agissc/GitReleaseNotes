using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitReleaseNotes.Git
{
    public static class GitRepositoryInfoFinder
    {
        private static readonly Dictionary<string, TaggedCommit> Cache = new Dictionary<string, TaggedCommit>();

        public static TaggedCommit GetLastTaggedCommit(IRepository gitRepo)
        {
            return GetTag(gitRepo, string.Empty);
        }

        public static TaggedCommit GetFirstCommit(IRepository gitRepo)
        {
            var branch = gitRepo.Head;
            return new TaggedCommit(branch.Commits.Last(), "Initial Commit");
        }

        private static TaggedCommit GetTag(IRepository gitRepo, string fromTag)
        {
            if (!Cache.ContainsKey(fromTag))
                Cache.Add(fromTag, GetLastTaggedCommit(gitRepo, t => string.IsNullOrEmpty(fromTag) || t.TagName.Equals(fromTag)));

            return Cache[fromTag];
        }

        private static TaggedCommit GetLastTaggedCommit(IRepository gitRepo, Func<TaggedCommit, bool> filterTags)
        {
            var branch = gitRepo.Head;
            var tags = gitRepo.Tags
                .Select(t => new TaggedCommit((Commit)t.Target, t.Name))
                .Where(filterTags)
                .ToArray();
            var olderThan = branch.Tip.Author.When;
            var allCommits = gitRepo.GetCommitsRecursive();
            var lastTaggedCommit =
                allCommits.FirstOrDefault(c => c.Author.When <= olderThan && tags.Any(a => a.Commit.Equals(c)));

            if (lastTaggedCommit != null)
                return tags.Last(a => a.Commit.Sha == lastTaggedCommit.Sha);

            return new TaggedCommit(allCommits.Last(), "Initial Commit");
        }

        public static ReleaseInfo GetCurrentReleaseInfo(IRepository repo)
        {
            var lastTaggedCommit = GetLastTaggedCommit(repo);
            var head = repo.Head;
            if (head.Tip.Sha == lastTaggedCommit.Commit.Sha)
                return new ReleaseInfo(null, null, lastTaggedCommit.Commit.Author.When);

            var firstCommitAfterLastTag = repo.GetCommitsRecursive().TakeWhile(c => c.Id != lastTaggedCommit.Commit.Id).Last().Sha;
            return new ReleaseInfo(null, null, lastTaggedCommit.Commit.Author.When)
            {
                FirstCommit = firstCommitAfterLastTag,
                LastCommit = head.Tip.Sha
            };
        }
    }
}