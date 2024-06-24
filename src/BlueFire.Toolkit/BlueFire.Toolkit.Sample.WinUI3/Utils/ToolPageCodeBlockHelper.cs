using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Utils
{
    public static class ToolPageCodeBlockHelper
    {
        public enum FileType
        {
            Xaml,
            CSharp,
        }

        public static string GetFileBlockContent(string toolName, string blockName, FileType fileType)
        {
            if (string.IsNullOrEmpty(blockName)) return string.Empty;

            var fileName = "";

            var hashIdx = blockName.IndexOf('#');
            if (hashIdx != -1)
            {
                fileName = blockName.Substring(hashIdx + 1);
                blockName = blockName.Substring(0, hashIdx);
            }
            else
            {
                fileName = $"{toolName}Page";
            }

            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockName)) return string.Empty;

            var filePath = BuildFilePath(fileName, fileType);
            if (!File.Exists(filePath)) return string.Empty;

            try
            {
                var fileContent = File.ReadAllText(filePath);
                return GetBlock(fileContent, blockName, fileType);
            }
            catch { }
            return string.Empty;
        }

        private static string GetBlock(string fileContent, string blockName, FileType fileType)
        {
            if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(fileContent)) return string.Empty;

            var (blockStart, blockStartTail, blockEnd) = fileType switch
            {
                FileType.Xaml => ("<!--region", "-->", "<!--endregion"),
                FileType.CSharp => ("#region", "", "#endregion"),
                _ => throw new NotSupportedException()
            };

            var idx = 0;
            while (idx != -1 && idx < fileContent.Length)
            {
                var blockStartIndex = fileContent.IndexOf(blockStart, idx);
                if (blockStartIndex == -1) return string.Empty;

                var blockStartNameIndex = blockStartIndex + blockStart.Length + 1;
                while (blockStartNameIndex < fileContent.Length && fileContent[blockStartNameIndex] == ' ') blockStartNameIndex++;
                if (fileContent.IndexOf(blockName, blockStartNameIndex) == blockStartNameIndex)
                {
                    var blockEndIndex = fileContent.IndexOf(blockEnd, blockStartNameIndex + blockName.Length);
                    if (blockEndIndex != -1)
                    {
                        var blockEndNameIndex = blockEndIndex + blockEnd.Length + 1;
                        while (blockEndNameIndex < fileContent.Length && fileContent[blockEndNameIndex] == ' ') blockEndNameIndex++;
                        if (fileContent.IndexOf(blockName, blockEndNameIndex) == blockEndNameIndex)
                        {
                            var startIdx = blockStartNameIndex + blockName.Length;
                            var endIdx = blockEndIndex;

                            if (!string.IsNullOrEmpty(blockStartTail))
                            {
                                startIdx = fileContent.IndexOf(blockStartTail, startIdx);
                                if (startIdx != -1)
                                {
                                    startIdx += blockStartTail.Length;
                                }
                            }

                            if (startIdx != -1)
                            {
                                var test = fileContent[startIdx..];

                                while (startIdx < fileContent.Length
                                    && (fileContent[startIdx] == '\n'
                                        || fileContent[startIdx] == '\r'
                                        || fileContent[startIdx] == ' '))
                                {
                                    startIdx++;
                                }

                                if (startIdx != fileContent.Length)
                                {
                                    while (startIdx > 0 && fileContent[startIdx] != '\n') startIdx--;
                                    startIdx++;

                                    while (endIdx > startIdx && fileContent[endIdx] != '\n') endIdx--;
                                    while (endIdx > startIdx && fileContent[endIdx] == '\n' || fileContent[endIdx] == '\r') endIdx--;
                                    if (endIdx > startIdx && endIdx + 1 < fileContent.Length)
                                    {
                                        var blockContent = fileContent[startIdx..(endIdx + 1)];
                                        var indentedCount = 0;
                                        while (indentedCount < blockContent.Length && blockContent[indentedCount] == ' ') indentedCount++;

                                        var indentString = new string(' ', indentedCount);

                                        var lines = blockContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

                                        for (int i = 0; i < lines.Count; i++)
                                        {
                                            if (string.IsNullOrWhiteSpace(lines[i]))
                                            {
                                                lines.RemoveAt(i);
                                                i--;
                                            }
                                            else break;
                                        }
                                        for (int i = lines.Count - 1; i >= 0; i--)
                                        {
                                            if (string.IsNullOrWhiteSpace(lines[i]))
                                            {
                                                lines.RemoveAt(i);
                                            }
                                            else break;
                                        }

                                        var stringBuilder = new StringBuilder();
                                        foreach (var line in lines)
                                        {
                                            if (line != "\n" && line != "\r\n")
                                            {
                                                if (line.StartsWith(indentString)) stringBuilder.AppendLine(line[indentedCount..]);
                                                else stringBuilder.AppendLine(line);
                                            }
                                        }

                                        return stringBuilder.ToString();
                                    }
                                }

                            }
                        }
                    }


                }

                idx = blockStartIndex + 1;
            }

            return string.Empty;
        }

        private static string BuildFilePath(string name, FileType fileType)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Views", "ToolPages", "Sources");
            var ext = fileType switch
            {
                FileType.Xaml => ".xaml.txt",
                FileType.CSharp => ".xaml.cs.txt",
                _ => throw new NotSupportedException()
            };
            return Path.Combine(dir, $"{name}{ext}");
        }
    }
}
