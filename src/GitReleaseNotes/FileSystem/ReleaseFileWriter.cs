using System;
using System.IO;

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
            using (var reader = new StreamReader(inputFile))
            {
                using (var writer = new StreamWriter(outputFile))
                {
                    CommonMark.CommonMarkConverter.Convert(reader, writer);
                }
            }
        }
    }
}