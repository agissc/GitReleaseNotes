using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitReleaseNotes
{
    public class JiraIssueTagger
    {

        private const string JIRA_URL = "https://jira.ag.ch/rest/api/2/";
        private const string UNRELEASED_TAG = "vNext";

        public void TagIssues(string releaseNotes, string accountId, string password, string excludedProjects, bool tagAllIssues)
        {
            StringReader reader = new StringReader(releaseNotes);
            int releaseCounter = 0;
            string release = "";
            string releaseBuildNumber = "";
            DateTime releaseDate;
            // Go through all pull requests
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null) break;

                // Find release
                if (line.StartsWith("#"))
                {
                    if (line.StartsWith("# rel-isagis-") && line.Contains('('))
                    {
                        if (!tagAllIssues && releaseCounter == 1) return;
                        releaseBuildNumber = line.Substring(13, line.IndexOf('(') - 14);
                        releaseDate = DateTime.ParseExact(line.Substring(line.IndexOf('(') + 1, line.IndexOf(')') - line.IndexOf('(') - 1), "dd MMMM yyyy", CultureInfo.CurrentCulture);
                        release = releaseDate.Year + "." + releaseDate.Month + "." + releaseBuildNumber;
                        releaseCounter++;
                    }
                    else if (line == "# vNext")
                    {
                        release = UNRELEASED_TAG;
                    }
                    else
                    {
                        Console.WriteLine("Ignoring release because of invalid name: {0}", line);
                    }
                }

                // Find JIRA issue name
                bool issueFound = true;
                string pattern = ") - [";
                int issueNameStartIndex = line.IndexOf(pattern);
                int issueNameEndIndex = -1;
                if (issueNameStartIndex <= pattern.Length - 1) issueFound = false;
                if (issueFound) {
                    issueNameStartIndex += pattern.Length;
                    issueNameEndIndex = line.IndexOf(']', issueNameStartIndex + 1);
                    if (issueNameEndIndex == -1) issueFound = false;
                }
                // Tag issue with release name
                if (issueFound)
                {
                    string issueKey = line.Substring(issueNameStartIndex, issueNameEndIndex - issueNameStartIndex).ToUpper();
                    if (!(IsExcluded(issueKey, excludedProjects.Split(',')) || release == ""))
                    {
                        TagJiraIssue(issueKey, release, accountId, password);
                    }
                }
            }
        }

        private bool IsExcluded(string issueKey, string[] excludedProjects)
        {
            foreach (string project in excludedProjects)
            {
                if (issueKey.StartsWith(project)) return true;
            }
            return false;
        }

        /// <summary>
        /// Tags a JIRA issue with a certain tag if it hasn't this tag already. If existing, also removes the unreleased tag on the issue.
        /// </summary>
        /// <param name="issueKey">Name of the JIRA issue (i.e. AV-45)</param>
        /// <param name="tag">String the issue should get tagged with</param>
        /// <param name="accountId">Account Id of the JIRA user tagging the issue</param>
        /// <param name="password">Password of the JIRA user tagging the issue</param>
        private void TagJiraIssue(string issueKey, string tag, string accountId, string password)
        {
            Console.WriteLine("Tagging Jira issue {0} with {1}", issueKey, tag);
            if (tag == UNRELEASED_TAG && HasReleaseTag(issueKey, accountId, password))
            {
                Console.WriteLine("Will not tag issue {0} with {1} because another branch with this issue has alread been released", issueKey, tag);
                return;
            }
            if(GetLabels(issueKey, accountId, password).Contains(tag))
            {
                Console.WriteLine("Will not tag issue {0} with {1} because the issue already contains this tag", issueKey, tag);
                return;
            }
            if (tag != UNRELEASED_TAG) RemoveUnreleasedTag(issueKey, accountId, password);

            RestRequest request = new RestRequest("issue/{key}", Method.PUT);
            request.AddUrlSegment("key", issueKey);
            request.RequestFormat = DataFormat.Json;

            string jSonContent = @"{""update"":{""labels"":[{""add"":""" + tag + @"""}]}}";
            request.AddParameter("application/json", jSonContent, ParameterType.RequestBody);

            var response = Execute(request, accountId, password);
        }

        /// <summary>
        /// If existing, removes the unreleased tag from the issue.
        /// </summary>
        private void RemoveUnreleasedTag(string issueKey, string accountId, string password)
        {
            string[] labels = GetLabels(issueKey, accountId, password);

            if (labels.Contains(UNRELEASED_TAG))
            {
                Console.WriteLine("Removing {0} tag from issue {1}", UNRELEASED_TAG, issueKey);
                RestRequest removeRequest = new RestRequest("issue/{key}", Method.PUT);
                removeRequest.AddUrlSegment("key", issueKey);
                removeRequest.RequestFormat = DataFormat.Json;

                string jsonContent = @"{""update"":{""labels"":[{""remove"":""" + UNRELEASED_TAG + @"""}]}}";
                removeRequest.AddParameter("application/json", jsonContent, ParameterType.RequestBody);
                var removeResponse = Execute(removeRequest, accountId, password);
                Console.WriteLine("repsonse: " + removeResponse);
            }
        }

        /// <summary>
        /// Checks and returns if the issue has a release tag attached to it
        /// </summary>
        private bool HasReleaseTag(string issueKey, string accountId, string password)
        {
            string[] labels = GetLabels(issueKey, accountId, password);
            foreach(string label in labels)
            {
                Version v = null;
                if (Version.TryParse(label, out v)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all lables / keywords of a jira issue as a string array.
        /// </summary>
        private string[] GetLabels(string issueKey, string accountId, string password)
        {
            RestRequest request = new RestRequest("issue/{key}", Method.GET);
            request.AddUrlSegment("key", issueKey);

            var response = Execute(request, accountId, password);

            int labelsStartIndex = response.IndexOf("labels") + 9;
            if (labelsStartIndex == -1) return new string[] { };
            int labelsEndIndex = response.IndexOf("],", labelsStartIndex);
            if (labelsEndIndex == -1) return new string[] { };
            return (response.Substring(labelsStartIndex, labelsEndIndex - labelsStartIndex).Split(',')).Select(s => !String.IsNullOrEmpty(s) ? s.Substring(1, s.Length - 2) : "").ToArray();
        }

        /// <summary>
        /// Executes a JIRA API request.
        /// </summary>
        private string Execute(RestRequest request, string accountId, string password)
        {
            var client = new RestClient(JIRA_URL);

            client.Authenticator = new HttpBasicAuthenticator(accountId, password);
            request.AddParameter("AccountSid", accountId, ParameterType.UrlSegment);
            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var jiraManagerException = new ApplicationException(message, response.ErrorException);
                throw jiraManagerException;
            }

            return response.Content;
        }
    }
}
