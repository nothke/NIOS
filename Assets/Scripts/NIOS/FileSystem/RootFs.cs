using System;
using System.IO;

namespace NIOS
{
    public class RootFs : IFileSystem
    {
        OperatingSystem os;
        public ISystemClock Clock => os.Machine.clock;

        public RootFs(OperatingSystem os)
        {
            this.os = os;
        }

        public void CreateDirectory(DirEntry directory, DirectorySecurity directorySecurity)
        {
        }

        public void DeleteDirectory(DirEntry directory)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(FileEntry file)
        {
            throw new NotImplementedException();
        }

        public Stream Open(FileEntry file, FileMode mode, FileAccess access, FileShare share)
        {
            throw new NotImplementedException();
        }

        public void UpdateDirectoryInfo(DirEntry.UpdateHandle handle)
        {
        }
    }
}