using System;
using System.Linq;
using GitReleaseNotes.Git;
using GitReleaseNotes.IssueTrackers;
using LibGit2Sharp;

namespace GitReleaseNotes
{
    public static class ReleaseNotesGenerator
    {
        public static SemanticReleaseNotes GenerateReleaseNotes(
            IRepository gitRepo, IIssueTracker issueTracker, string[] categories, TaggedCommit tagToStartFrom, ReleaseInfo currentReleaseInfo, string diffUrlFormat)
        {
            var releases = ReleaseFinder.FindReleases(gitRepo, tagToStartFrom, currentReleaseInfo);
            var findIssuesSince = tagToStartFrom.Commit.Author.When;

            var closedIssues = issueTracker.GetClosedIssues(findIssuesSince).ToArray();

            var semanticReleases = (
                from release in releases
                let releaseNoteItems = closedIssues
                    .Where(i => (release.When == null || i.DateClosed < release.When) && (release.PreviousReleaseDate == null || i.DateClosed > release.PreviousReleaseDate))
                    .Select(i => new ReleaseNoteItem(i.Title, i.Id, i.HtmlUrl, i.Labels, i.DateClosed, i.Contributors))
                    .ToList<IReleaseNoteLine>()
                let beginningSha = release.FirstCommit?.Substring(0, 10)
                let endSha = release.LastCommit?.Substring(0, 10)
                select new SemanticRelease(release.Name, release.When, releaseNoteItems, new ReleaseDiffInfo
                {
                    BeginningSha = beginningSha, 
                    EndSha = endSha,
                    DiffUrlFormat = diffUrlFormat
                })).ToList();

            return new SemanticReleaseNotes(semanticReleases, categories);
        }
    }
}