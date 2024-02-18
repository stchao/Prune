namespace Prune.Wrapper
{
    public class DirectoryInfoWrapper : IDirectoryInfoWrapper
    {
        public IFileWrapper[] GetFiles(string path)
        {
            var directory = new DirectoryInfo(path);
            var fileInfos = directory.GetFiles();
            return fileInfos.Select(fileInfo => new FileWrapper(fileInfo)).ToArray();
        }

        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }
    }

    public interface IDirectoryInfoWrapper
    {
        public IFileWrapper[] GetFiles(string path);

        public bool Exists(string path);
    }
}
