namespace Prune.Wrapper
{
    public class FileWrapper : IFileWrapper
    {
        private readonly FileInfo? _fileInfo;
        private readonly string _name = string.Empty;
        private readonly string _fullname = string.Empty;
        private readonly DateTime _lastAccessTime;
        private readonly DateTime _lastAccessTimeUtc;

        public FileWrapper() { }

        public FileWrapper(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public FileWrapper(
            string name,
            string fullName,
            DateTime lastAccessTime,
            DateTime lastAccessTimeUtc
        )
        {
            _name = name;
            _fullname = fullName;
            _lastAccessTime = lastAccessTime;
            _lastAccessTimeUtc = lastAccessTimeUtc;
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public string Name => _fileInfo?.Name ?? _name;

        public string FullName => _fileInfo?.FullName ?? _fullname;

        public DateTime LastAccessTime => _fileInfo?.LastAccessTime ?? _lastAccessTime;

        public DateTime LastAccessTimeUtc => _fileInfo?.LastAccessTimeUtc ?? _lastAccessTimeUtc;
    }

    public interface IFileWrapper
    {
        public void Delete(string path);

        public string Name { get; }

        public string FullName { get; }

        public DateTime LastAccessTime { get; }

        public DateTime LastAccessTimeUtc { get; }
    }
}
