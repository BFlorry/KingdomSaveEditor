using System;
using System.IO;

namespace KHSave.Archives.Factories
{
    public class Ps3PsvFactory : IArchiveFactory
    {
        public string Name => "PS3 PSV";

        public string Description => "PlayStation 3 PSV memory card export";

        public IArchive Create()
        {
            throw new NotImplementedException();
        }

        public IArchiveEntry CreateEntry()
        {
            throw new NotImplementedException();
        }

        public bool IsValid(Stream stream)
        {
            var currentPosition = stream.Position;
            stream.Position = 0;

            var result = BitConverter.ToUInt32(stream.ReadBytes(4));
            stream.SetPosition((int)currentPosition);
            return result == 0x50535600;
        }

        public IArchive Read(Stream stream)
        {
            return Ps3PsvArchive.Read(stream);
        }
    }
}
