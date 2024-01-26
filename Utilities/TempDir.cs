using System;
using System.Diagnostics;
using System.IO;

namespace PrimerTools
{
    public class TempDir : IDisposable
    {
        private readonly DirectoryInfo info;
        private int childrenCount = 0;
        private readonly bool isRoot;
        private bool exists = false;

        public string FullPath => info.FullName;

        public TempDir()
        {
            var tempPath = Path.GetTempPath();
            var tmpDir = Path.Combine(tempPath, $"unity-primer-{Guid.NewGuid()}");
            info = Directory.CreateDirectory(tmpDir);
            isRoot = true;
            exists = true;
        }

        private TempDir(DirectoryInfo info)
        {
            this.info = info;
            isRoot = false;
            exists = true;
        }

        ~TempDir() => CleanUpResources();

        public void Dispose() => CleanUpResources();

        public void Open()
        {
            Process.Start($"{FullPath}{Path.DirectorySeparatorChar}");
        }

        public string GetChildPath(string filename)
        {
            return Path.Combine(info.FullName, filename);
        }

        public TempDir CreateChild(string name, bool autoIncrement = false)
        {
            if (autoIncrement)
                name += childrenCount++;
            else if (string.IsNullOrWhiteSpace(name))
                name = $"{childrenCount++}";

            var child = info.CreateSubdirectory(name);
            return new TempDir(child);
        }

        public override string ToString() => info.FullName;

        private void CleanUpResources()
        {
            if (!exists || !isRoot)
                return;

            void DeleteRecursively(DirectoryInfo directory)
            {
                foreach (var subDirectory in directory.EnumerateDirectories())
                    DeleteRecursively(subDirectory);

                directory.Delete(true);
            }

            DeleteRecursively(info);
            exists = false;
        }
    }
}
