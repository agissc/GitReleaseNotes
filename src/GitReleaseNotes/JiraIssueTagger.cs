using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitReleaseNotes
{
    public class JiraIssueTagger
    {

        private const string JIRA_URL = "https://jira.ag.ch/rest/api/2/";

        public void TagAllIssues(string releaseNotes, string accountId, string password, string excludedProjects)
        {
            StringReader reader = new StringReader(releaseNotes);
            string release = "";
            string releaseBuildNumber = "";
            DateTime releaseDate;
            // Go through all pull requests
            while(true)
            {
                string line = reader.ReadLine();
                if (line == null) break;

                // Find release
                if (line.StartsWith("# rel-isagis-") && line.Contains('('))
                {
                    releaseBuildNumber = line.Substring(13, line.IndexOf('(') - 14);
                    releaseDate = DateTime.ParseExact(line.Substring(line.IndexOf('(') + 1, line.IndexOf(')') - line.IndexOf('(') - 1), "dd MMMM yyyy", CultureInfo.CurrentCulture);
                    release = releaseDate.Year + "." + releaseDate.Month + "." + releaseBuildNumber;
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
                        Console.WriteLine("Tagging Jira issue {0} with {1}", issueKey, release);
                        TagJiraIssue(issueKey, release, accountId, password);
                    }
                }
            }
            //TagJiraIssue("DEVENV-354", "test2", accountId, password);
        }

        private bool IsExcluded(string issueKey, string[] excludedProjects)
        {
            foreach(string project in excludedProjects)
            {
                if (issueKey.StartsWith(project)) return true;
            }
            return false;
        }

        private void TagJiraIssue(string issueKey, string tag, string accountId, string password)
        {
            RestRequest request = new RestRequest("issue/{key}", Method.PUT);
            request.AddUrlSegment("key", issueKey);
            request.RequestFormat = DataFormat.Json;

            string jSonContent = @"{""update"":{""labels"":[{""add"":""" + tag + @"""}]}}";
            request.AddParameter("application/json", jSonContent, ParameterType.RequestBody);

            var response = Execute(request, accountId, password);
        }

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
                Console.WriteLine(message + " " + response.ErrorException);
            }

            return response.Content;
        }
    }
}
