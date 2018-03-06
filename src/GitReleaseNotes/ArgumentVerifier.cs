using System;

namespace GitReleaseNotes
{
    public class ArgumentVerifier
    {
        public static bool VerifyArguments(GitReleaseNotesArguments arguments)
        {
            VerifyOutputFile(arguments.OutputFile);
            VerifyOutputFileHtml(arguments.OutputFileHtml);
            return true;
        }

        private static void VerifyOutputFile(string outputFile)
        {
            if (string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine("WARN: No Output file specified (*.md) [/OutputFile ...]");
            }
            if (!string.IsNullOrEmpty(outputFile) && !outputFile.EndsWith(".md"))
            {
                Console.WriteLine("WARN: Output file should have a .md extension [/OutputFile ...]");
                outputFile = null;
            }
        }

        private static void VerifyOutputFileHtml(string outputFileHtml)
        {
            if (string.IsNullOrEmpty(outputFileHtml))
            {
                Console.WriteLine("WARN: No Output HTML file specified (*.html) [/OutputFileHtml ...]");
            }
            if (!string.IsNullOrEmpty(outputFileHtml) && !outputFileHtml.EndsWith(".html"))
            {
                Console.WriteLine("WARN: Output HTML file should have a .html extension [/OutputFileHtml ...]");
                outputFileHtml = null;
            }
        }
    }
}