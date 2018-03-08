using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GitReleaseNotes.FileSystem
{
    public class ReleaseFileWriter
    {
        private readonly IFileSystem _fileSystem;

        public ReleaseFileWriter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void OutputReleaseNotesFile(string releaseNotesOutput, string outputFile)
        {
            if (string.IsNullOrEmpty(outputFile))
                return;
            _fileSystem.WriteAllText(outputFile, releaseNotesOutput);
            Console.WriteLine("Release notes written to {0}", outputFile);
        }

        public void OutputReleaseNotesHtml(string inputFile, string outputFile)
        {
            // Generate body out of .md file
            using (var reader = new StreamReader(inputFile))
            {
                using (var writer = new StreamWriter(outputFile))
                {
                    CommonMark.CommonMarkConverter.Convert(reader, writer);
                }
            }

            // Add html skeleton with bootstrap css
            string htmlBody = File.ReadAllText(outputFile);
            string htmlPrefix = "<!DOCTYPE html>\n<html>\n<head>\n<title>ReleaseNotes</title>\n<meta charset=\"utf-8\">\n<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/css/bootstrap.min.css\">\n</head>\n<body>\n<div class=\"col-md-12\">\n";
            string htmlSuffix = "\n</div>\n</body>\n</html>";
            string fullHtml = new StringBuilder(htmlPrefix).Append(htmlBody).Append(htmlSuffix).ToString();

            // Change all links to open in new tab
            fullHtml = Regex.Replace(fullHtml, "<(a)([^>]+)>", "<$1 target=\"_blank\"$2>");

            File.WriteAllText(outputFile, fullHtml);

            Console.WriteLine("Release notes html written to {0}", outputFile);
        }
    }
}