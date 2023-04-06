using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xutils.ConsoleApp;

namespace DeepRen
{
    public class AffectedCounts
    {
        public int FilesRenamed { get; set; }
        public int ReplacementsMade { get; set; }
        public int FilesReplaced { get; set; }
        public int DirectoriesRenamed { get; set; }
    }

    [Command(Name = "DeepRen",
             Description = "Utility to rename files, directories, and replace text in file contents using regular expressions",
             OptionsComparison = StringComparison.InvariantCultureIgnoreCase, 
             UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw)]
    [HelpOption("-?")]
    public class Core
    {
        private readonly IColoredConsole _console;

        [Required(ErrorMessage = "The DirectoryPath argument is required")]
        [PathMustExist(ErrorMessage = "The provided path in DirectoryPath argument does not exist")]
        [Argument(0, Name = "DirectoryPath",
                  Description = "The directory path in which the renamings and replacings shall be made.",
                  ShowInHelpText = true)]
        public string DirectoryPath { get; set; }

        [Required(ErrorMessage = "The FindPattern argument is required")]
        [Argument(1, Name = "FindPattern",
                  Description = "Regular Expression to be found in the paths and contents.",
                  ShowInHelpText = true)]
        public string FindPattern { get; set; }

        [Required(ErrorMessage = "The ReplacementText argument is required")]
        [Argument(2, Name = "ReplacementText",
                  Description = "Text which will replace the matches of the Find Pattern.",
                  ShowInHelpText = true)]
        public string ReplacementText { get; set; }

        [Option(Template = "-F|--filter",
                Description = "File search pattern to filter the file names to be renamed.",
                ValueName = "*.*",
                ShowInHelpText = true)]
        public string FileSearchPattern { get; set; } = "*.*";

        [Option(Template = "-W|--whatif",
                Description = "If specified, allows to simulate the changes done, outputting the results without making any change.",
                ShowInHelpText = true)]
        public bool WhatIf { get; set; } = false;

        public Core(IColoredConsole console)
        {
            _console = console;
            _console.ColoredWriteLine("<w>DeepRen</w>");
            _console.ColoredWriteLine("<g>Copyright ©2023 - Robson Rocha de Araújo <b>&lt;http://github.com/robson-rocha&gt;</b></g>");
        }

        public void OnExecute(CommandLineApplication app)
        {
            if (WhatIf)
            {
                _console.ColoredWriteLine("<bg c='dy'><k> WhatIf option applied. No changes will be made. </k></bg>");
            }
            RenameFilesFoldersAndContents(DirectoryPath, FindPattern, ReplacementText, FileSearchPattern);
        }

        private void RenameFilesFoldersAndContents(string directoryPath, string findPattern, string replaceText, string fileSearchPattern = "*.*", AffectedCounts affected = null)
        {
            var dir = new DirectoryInfo(directoryPath);
            bool affectedWasNull = (affected == null);
            Stopwatch sw = null;
            if (affectedWasNull)
            {
                sw = new Stopwatch();
                sw.Start();
                affected = new AffectedCounts();
            }
            foreach (var subDirectoryName in dir.EnumerateDirectories()
                                                .Select(d => d.Name))
            {
                string subDirectoryPath = Path.Combine(directoryPath, subDirectoryName);
                RenameFilesFoldersAndContents(subDirectoryPath, findPattern, replaceText, fileSearchPattern, affected);
                if (Regex.IsMatch(subDirectoryName, findPattern))
                {
                    try
                    {
                        string newSubDirectoryPath = Path.Combine(directoryPath, Regex.Replace(subDirectoryName, findPattern, replaceText));
                        if (subDirectoryPath.ToLowerInvariant() == newSubDirectoryPath.ToLowerInvariant())
                        {
                            string tempSubDirectoryPath = Path.Combine(directoryPath, Regex.Replace(subDirectoryName, findPattern, replaceText + "___TEMP"));
                            if (!WhatIf)
                            {
                                Directory.Move(subDirectoryPath, tempSubDirectoryPath);
                                Directory.Move(tempSubDirectoryPath, newSubDirectoryPath);
                            }
                        }
                        else
                        {
                            if (!WhatIf)
                            {
                                Directory.Move(subDirectoryPath, newSubDirectoryPath);
                            }
                        }
                        _console.ColoredWriteLine($"Renamed directory <w>\"{subDirectoryPath}\"</w> to <w>\"{newSubDirectoryPath}\"</w>");
                        affected.DirectoriesRenamed++;
                    }
                    catch (Exception ex)
                    {
                        _console.ColoredWriteLine($"<r>{ex.Message}</r>");
                    }
                }
            }

            foreach (var fileName in dir.EnumerateFiles(fileSearchPattern)
                                        .Select(f => f.Name))
            {
                try
                {
                    string filePath = Path.Combine(directoryPath, fileName);
                    string fileContent = File.ReadAllText(filePath);
                    int qtdMatches = Regex.Matches(fileContent, findPattern).Count;
                    if (qtdMatches > 0)
                    {
                        fileContent = Regex.Replace(fileContent, findPattern, replaceText);
                        if (!WhatIf)
                        {
                            File.WriteAllText(filePath, fileContent);
                        }
                        _console.ColoredWriteLine($"Replaced <w>{qtdMatches}</w> occurrences of \"<w>{findPattern}</w>\" in contents of \"<w>{filePath}</w>\" with \"<w>{replaceText}\"</w>");
                        affected.ReplacementsMade += qtdMatches;
                        affected.FilesReplaced++;
                    }
                    if (Regex.IsMatch(fileName, findPattern))
                    {
                        string newFilePath = Path.Combine(directoryPath, Regex.Replace(fileName, findPattern, replaceText));
                        if (!WhatIf)
                        {
                            File.Move(filePath, newFilePath);
                        }
                        _console.ColoredWriteLine($"Renamed file \"<w>{filePath}</w>\" to \"<w>{newFilePath}</w>\"");
                        affected.FilesRenamed++;
                    }
                }
                catch (Exception ex)
                {
                    _console.ColoredWriteLine($"<r>{ex.Message}</r>");
                }
            }

            if (affectedWasNull)
            {
                sw.Stop();
                _console.ColoredWriteLine($"\r\n<dg>Done in <g>{sw.Elapsed}</g></dg>");
                _console.ColoredWriteLine($"\t<dc><c>{affected.DirectoriesRenamed}</c> directories renamed</dc>");
                _console.ColoredWriteLine($"\t<dc><c>{affected.ReplacementsMade}</c> replacements made in <c>{affected.FilesReplaced}</c> files</dc>");
                _console.ColoredWriteLine($"\t<dc><c>{affected.FilesRenamed}</c> files renamed</dc>");
            }
        }
    }
}
