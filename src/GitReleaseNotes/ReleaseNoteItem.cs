using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitReleaseNotes
{
    public class ReleaseNoteItem : IReleaseNoteLine
    {
        private readonly string title;
        private string issueTitle;
        private string issueDescription;
        private readonly string issueNumber;
        private string jiraUrl;
        private readonly Uri htmlUrl;
        private readonly string[] tags;
        private readonly DateTimeOffset? resolvedOn;
        private readonly Contributor[] contributors;

        public ReleaseNoteItem(string title, string issueNumber, Uri htmlUrl, string[] tags, DateTimeOffset? resolvedOn, Contributor[] contributors)
        {
            this.title = title;
            this.issueNumber = issueNumber;
            this.htmlUrl = htmlUrl;
            this.tags = tags ?? new string[0];
            this.resolvedOn = resolvedOn;
            this.contributors = contributors;
            CreateJiraLink();
        }

        public string Title
        {
            get { return title; }
        }

        public string IssueTitle
        {
            get { return issueTitle; }
        }

        public string IssueDescription
        {
            get { return issueDescription; }
        }

        public Uri HtmlUrl
        {
            get { return htmlUrl; }
        }

        public string[] Tags
        {
            get { return tags; }
        }

        public string IssueNumber
        {
            get { return issueNumber; }
        }

        public string JiraUrl
        {
            get { return jiraUrl;}
        }

        public DateTimeOffset? ResolvedOn
        {
            get { return resolvedOn; }
        }

        public Contributor[] Contributors { get { return contributors; }}

        private void CreateJiraLink()
        {
            issueTitle = title;
            if (!IssueTitle.Contains(":") || (IssueTitle.Contains(":") && IssueTitle.IndexOf(":") > 15)) return;
            if (!IssueTitle.Substring(0, IssueTitle.IndexOf(":")).Contains("-")) return;
            issueDescription = IssueTitle.Substring(IssueTitle.IndexOf(":"));
            issueTitle = IssueTitle.Substring(0, IssueTitle.IndexOf(":"));
            jiraUrl = "(https://jira.ag.ch/browse/" + issueTitle + ")";
            issueTitle = "[" + issueTitle + "]";
        }

        public string ToString(string[] categories)
        {
            var taggedCategory = Tags.FirstOrDefault(t => categories.Any(c => c.Equals(t, StringComparison.InvariantCultureIgnoreCase)));
            if ("bug".Equals(taggedCategory, StringComparison.InvariantCultureIgnoreCase))
                taggedCategory = "fix";
            var category = taggedCategory == null
                ? null
                : String.Format(" +{0}", taggedCategory.Replace(" ", "-"));
            var issueNum = IssueNumber == null ? null : String.Format(" [{0}]", IssueNumber);
            var url = HtmlUrl == null ? null : String.Format("({0})", HtmlUrl);
            var contributors = Contributors == null || Contributors.Length == 0 ?
                string.Empty : " contributed by " + String.Join(", ", Contributors.Select(r => String.Format("{0} ([{1}]({2}))", r.Name, r.Username, r.Url)));
            
            return string.Format(" - {1}{2}{4}{0}{6}{7}{5}{3}", issueTitle, issueNum, url, category,
                title.TrimStart().StartsWith("-") ? null : " - ",
                contributors, jiraUrl, issueDescription).Replace("  ", " ").Replace("- -", "-");
        }
    }
}