using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHSave.Archives.Factories
{
    public class Ps2CbsFactory : IArchiveFactory
    {
        public string Name => "PS2 CBS";

        public string Description => "PlayStation 2 CodeBreaker save";

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
            return result == 0x00554643;
        }

        public IArchive Read(Stream stream)
        {
            var archive = Ps2CbsArchive.Read(stream, 10);
            return archive;
        }
    }
}
