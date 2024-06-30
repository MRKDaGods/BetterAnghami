using System;
using System.IO;

namespace MRK
{
    public class FileManager
    {
        private static FileManager? _instance;

        public string BaseFolderPath { get; init; }

        public static FileManager Instance
        {
            get => _instance ??= new FileManager();
        }

        public FileManager()
        {
            // check for directories
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local))
            {
                throw new DirectoryNotFoundException("Local Appdata does not exist");
            }

            BaseFolderPath = $"{local}/{Configuration.Static.BaseFolderName}";

            // create dir if doesnt exist
            if (!Directory.Exists(BaseFolderPath))
            {
                Directory.CreateDirectory(BaseFolderPath);
            }
        }

        /// <summary>
        /// Opens a file in the given mode, given the relative path
        /// </summary>
        public FileStream Open(string relativePath, FileMode mode)
        {
            return File.Open($"{BaseFolderPath}/{relativePath}", mode);
        }

        /// <summary>
        /// Determines whether the relative file path exists or not
        /// </summary>
        public bool Exists(string relativePath)
        {
            return File.Exists($"{BaseFolderPath}/{relativePath}");
        }

        /// <summary>
        /// Determines whether the relative directory path exists or not
        /// </summary>
        public bool DirectoryExists(string relativePath)
        {
            return Directory.Exists($"{BaseFolderPath}/{relativePath}");
        }

        /// <summary>
        /// Creates the specified relative directory
        /// </summary>
        public void DirectoryCreate(string relativePath)
        {
            Directory.CreateDirectory($"{BaseFolderPath}/{relativePath}");
        }

        public void Rename(string relativePath, string newRelativeName)
        {
            File.Move($"{BaseFolderPath}/{relativePath}", $"{BaseFolderPath}/{newRelativeName}");
        }

        public void Delete(string relativePath)
        {
            File.Delete($"{BaseFolderPath}/{relativePath}");
        }
    }
}
