using KHSave.Lib2;
using KHSave.Lib2.Types;
using System.IO;
using System.Linq;
using Xunit;

namespace KHSave.Tests
{
    public class Kh2Tests
    {
        private static readonly string FilePath = "Saves/kh2fm.bin";
        private SaveKh2.SaveFinalMix save;

        public Kh2Tests()
        {
            using (var stream = File.OpenRead(FilePath))
            {
                save = Xe.BinaryMapper.BinaryMapping.ReadObject<SaveKh2.SaveFinalMix>(stream);
            }
        }

        [Theory]
        [InlineData(0x4a32484b, true)]
        [InlineData(0x5532484b, true)]
        [InlineData(0x4532484b, true)]
        [InlineData(0xcccccccc, false)]
        public void TestIsValid(uint header, bool expected)
        {
            var stream = new MemoryStream(4);
            new BinaryWriter(stream).Write(header);
            stream.Position = 0;

            Assert.Equal(expected, SaveKh2.IsValid(stream));
        }

        [Theory]
        [InlineData(0x4a32484b, 0x2a, GameVersion.Japanese)]
        [InlineData(0x5532484b, 0x2a, GameVersion.Japanese)]
        [InlineData(0x4532484b, 0x2a, GameVersion.Japanese)]
        [InlineData(0x4532484b, 0x2d, GameVersion.American)]
        [InlineData(0x4532484b, 0x3a, GameVersion.FinalMix)]
        [InlineData(0xcccccccc, 0x2a, null)]
        public void TestVersion(uint header, int version, GameVersion? expected)
        {
            var stream = new MemoryStream(4);
            var writer = new BinaryWriter(stream);
            writer.Write(header);
            writer.Write(version);
            stream.Position = 0;

            Assert.Equal(expected, SaveKh2.GetGameVersion(stream));
        }

        [Fact]
        public void TestChecksum()
        {
            var rand = new System.Random(0);
            var data = Enumerable.Range(0, 0x10000)
                .Select(x => (byte)rand.Next())
                .ToArray();
            Assert.Equal(1527299405U, SaveKh2.CalculateChecksum(data, data.Length, uint.MaxValue));
        }

        [Fact]
        public void TestRead()
        {
            Assert.Equal(EquipmentType.UltimaWeapon, save.Characters[(int)CharacterType.Sora].Weapon);
            Assert.Equal(60, save.Characters[(int)CharacterType.Sora].HpCur);
            Assert.Equal(60, save.Characters[(int)CharacterType.Sora].HpMax);
            Assert.Equal(120, save.Characters[(int)CharacterType.Sora].MpMax);
            Assert.Equal(120, save.Characters[(int)CharacterType.Sora].MpMax);
            Assert.Equal(99, save.Characters[(int)CharacterType.Sora].Level);
            Assert.Equal(4, save.Characters[(int)CharacterType.Sora].AccessoryCount);
            Assert.Equal(4, save.Characters[(int)CharacterType.Sora].ArmorCount);
            Assert.Equal(8, save.Characters[(int)CharacterType.Sora].ItemCount);
            Assert.Equal(0, save.Characters[(int)CharacterType.Sora].UnknownCount);
            Assert.Equal((int)EquipmentType.FinishingPlus | 0x8000, save.Characters[(int)CharacterType.Sora].Abilities[0]);

            Assert.Equal(EquipmentType.SaveTheQueen, save.Characters[(int)CharacterType.Donald].Weapon);
            Assert.Equal(BattleStyleType.SoraAttack, save.Characters[(int)CharacterType.Donald].BattleStyle);
            Assert.Equal(EquipmentType.ChampionBelt, (EquipmentType)save.Characters[(int)CharacterType.Donald].Armors[0]);
            Assert.Equal(AbilityStyleType.Free, save.Characters[(int)CharacterType.Donald].AbilityStyle1);
            Assert.Equal(AbilityStyleType.Free, save.Characters[(int)CharacterType.Donald].AbilityStyle2);
            Assert.Equal(AbilityStyleType.Free, save.Characters[(int)CharacterType.Donald].AbilityStyle3);
            Assert.Equal(AbilityStyleType.Free, save.Characters[(int)CharacterType.Donald].AbilityStyle4);
            Assert.Equal(EquipmentType.Empty, (EquipmentType)save.Characters[(int)CharacterType.Goofy].Armors[0]);
            Assert.Equal(EquipmentType.ShadowArchive, (EquipmentType)save.Characters[(int)CharacterType.Goofy].Accessories[0]);
            Assert.Equal(EquipmentType.Empty, (EquipmentType)save.Characters[(int)CharacterType.Riku].Armors[0]);

            Assert.Equal(PlayableCharacterType.Sora, save.WorldPartyMembers[18].PlayableCharacter);
            Assert.Equal(PlayableCharacterType.WorldCharacter, save.WorldPartyMembers[18].CompanionCharacter1);
            Assert.Equal(PlayableCharacterType.Goofy, save.WorldPartyMembers[18].CompanionCharacter2);
            Assert.Equal(PlayableCharacterType.Donald, save.WorldPartyMembers[18].CompanionCharacter3);

            Assert.Equal(Difficulty.Critical, save.Difficulty);
            Assert.Equal(2885291, save.Experience);
            Assert.Equal(60, save.BonusLevel);

            Assert.Equal(9, save.DriveBarCurrent);
            Assert.Equal(9, save.DriveBarMax);
        }

        private const int DriveBarCurrentOffset = 0x3529;
        private const int DriveBarMaxOffset = 0x352a;
        private const int ChecksumOffset = 0x8;
        private const int ChecksumLength = 0x4;

        [Theory]
        [InlineData(DriveBarCurrentOffset)]
        [InlineData(DriveBarMaxOffset)]
        public void TestDriveGaugeIsWrittenAtTheExpectedOffset(int offset)
        {
            var expectedData = File.ReadAllBytes(FilePath);

            var save = SaveKh2.Read(new MemoryStream(expectedData));
            if (offset == DriveBarMaxOffset)
                save.DriveBarMax = 5;
            else
                save.DriveBarCurrent = 5;

            var outStream = new MemoryStream();
            SaveKh2.Write(outStream, save);
            var actualData = outStream.ToArray();

            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal(5, actualData[offset]);

            // Only the edited byte and the checksum are allowed to change.
            for (var i = 0; i < expectedData.Length; i++)
            {
                if (i == offset)
                    continue;
                if (i >= ChecksumOffset && i < ChecksumOffset + ChecksumLength)
                    continue;

                Assert.True(expectedData[i] == actualData[i],
                    $"Unexpected change at {i:X}: expected {expectedData[i]:X02} but found {actualData[i]:X02}");
            }
        }

        [Fact]
        public void TestDriveGaugeRoundTrips()
        {
            var save = SaveKh2.Read(new MemoryStream(File.ReadAllBytes(FilePath)));
            save.DriveBarMax = 7;
            save.DriveBarCurrent = 3;

            var outStream = new MemoryStream();
            SaveKh2.Write(outStream, save);

            var actual = SaveKh2.Read(outStream);
            Assert.Equal(7, actual.DriveBarMax);
            Assert.Equal(3, actual.DriveBarCurrent);
        }

        [Fact]
        public void TestWriteBackTheSameExactFile() =>
        File.OpenRead(FilePath).Using(stream => Helpers.AssertStream(stream, inStream =>
        {
            var save = SaveKh2.Read(inStream);

            var outStream = new MemoryStream();
            SaveKh2.Write(outStream, save);

            return outStream;
        }));
    }
}
