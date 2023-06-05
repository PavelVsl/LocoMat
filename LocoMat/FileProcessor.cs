namespace LocoMat;

public class FileProcessor
{
    public static async Task ProcessFilesAsync(
        string inputPath,
        string outputPath,
        bool recursive,
        Func<string, string, Task> fileAction,
        bool createOutputPath = false,
        string fileMask = "*.*"
    )
    {
        if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
        if (fileAction == null) throw new ArgumentNullException(nameof(fileAction));

        var inputFolder = inputPath;
        if (!Utilities.IsDirectory(inputPath))
        {
             inputFolder = Path.GetDirectoryName(inputPath);
             fileMask = Path.GetFileName(inputPath);
        }
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(inputFolder, fileMask , searchOption);
        foreach (var inputFile in files)
        {
            var outputFilePath = GetOutputFilePath(inputFile, inputFolder, outputPath);
            if (createOutputPath)
            {
                Directory.CreateDirectory(outputFilePath);
            }

            await fileAction(inputFile, outputFilePath);
        }
    }


    private static string GetOutputFilePath(string inputFile, string inputFolderPath, string outputFolderPath)
    {
        if (string.IsNullOrEmpty(outputFolderPath))
        {
            return Path.GetDirectoryName(inputFile);
        }
        else
        {
            string relativePath = Path.GetRelativePath(inputFolderPath, Path.GetDirectoryName(inputFile));
            return Path.Combine(outputFolderPath, relativePath.Replace(Path.DirectorySeparatorChar, '/'));
        }
    }


}
