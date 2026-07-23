using Xunit;
using KHSave.Archives;
using KHSave.LibRecom;
using System.IO;
using System;
using KHSave.LibRecom.Types;

namespace KHSave.Tests
{
    public class RecomTests
    {
        [Fact]
        public void IsValidTest()
        {
            var data = new byte[0x10];
            data[0] = 7;
            using (var stream = new MemoryStream(data))
                Assert.True(SaveKhRecom.IsValid(stream));
        }

        [Fact]
        public void IsNotValidIfMagicCodeIsNotRecognizedTest()
        {
            var data = new byte[0x10];
            data[0] = 6;
            using (var stream = new MemoryStream(data))
                Assert.False(SaveKhRecom.IsValid(stream));
        }

        [Fact]
        public void IsNotValidIfLengthIsNotEnoughTest()
        {
            var data = new byte[0xf];
            data[0] = 7;
            using (var stream = new MemoryStream(data))
                Assert.False(SaveKhRecom.IsValid(stream));
        }

        [Fact]
        public void SaveCorrectHeader() => OnSave(save =>
        {
            using (var memStream = new MemoryStream())
            {
                save.MagicCode = 999;
                save.Checksum = 888;
                save.Length = 777;
                save.Zeroed = 666;

                save.Write(memStream);
                memStream.Position = 0;
                var reader = new BinaryReader(memStream);

                Assert.Equal(7, reader.ReadInt32());
                Assert.Equal(180124905, reader.ReadInt32());
                Assert.Equal(13856, reader.ReadInt32());
                Assert.Equal(0, reader.ReadInt32());
            }
        });

        [Fact]
        public void CheckStoryFlags() => OnSaveData(save =>
        {
            Assert.True(save.Table0.SoraStoryFlag.TraverseTown);
            Assert.False(save.Table0.SoraStoryFlag.Agrabah);
            Assert.False(save.Table0.RikuStoryFlag.TraverseTown);
            Assert.False(save.Table0.RikuStoryFlag.Agrabah);
        });

        [Fact]
        public void CheckTable2() => OnSaveData(save =>
        {
            Assert.Equal(2, save.Table2.Data[2]);
        });

        [Fact]
        public void CheckCardInventory() => OnSaveData(save =>
        {
            Assert.Equal(1, save.McWork.CardInventoryCount[0]);
            Assert.Equal(1, save.McWork.CardInventoryCount[1]);
            Assert.Equal(2, save.McWork.CardInventoryCount[2]);
            Assert.Equal(2, save.McWork.CardInventoryCount[3]);
            Assert.Equal(2, save.McWork.CardInventoryCount[4]);
            Assert.Equal(1, save.McWork.CardInventoryCount[5]);
            Assert.Equal(1, save.McWork.CardInventoryCount[6]);
        });

        [Fact]
        public void CheckTutorialsClearedFlags() => OnSaveData(save =>
        {
            Assert.False(save.Table0.Tutorial.KeyRoom);
            Assert.False(save.Table0.Tutorial.MoogleShop);
            Assert.False(save.Table0.Tutorial.FloorMove);
            Assert.False(save.Table0.Tutorial.WarpPoint);
            Assert.True(save.Table0.Tutorial.SavePoint);
            Assert.True(save.Table0.Tutorial.Field);
            Assert.True(save.Table0.Tutorial.WorldSelect);
        });

        [Fact]
        public void CheckPlayMode() => OnSaveData(save =>
        {
            Assert.Equal(PlayMode.Sora, save.Table1.PlayMode);
        });

        [Fact]
        public void CheckDifficulty() => OnSaveData(save =>
        {
            Assert.Equal(Difficulty.Standard, save.Table1.Difficulty);
        });

        [Fact]
        public void CheckExpAmount() => OnSaveData(save =>
        {
            Assert.Equal(15, save.McWork.Experience);
        });

        [Theory]
        [InlineData("Saves/KHReCoM_WW_1162MP.png", 1162)]
        [InlineData("Saves/KHReCoM_WW_1244MP.png", 1244)]
        [InlineData("Saves/BASLUS-21799434F4D2D3032.PSV", 128)]
        public void CheckMooglePointsAmount(string filePath, int expected)
        {
            var save = ReadArchivedSave(filePath);

            Assert.Equal(expected, save.McWork.MooglePoints);
            Assert.Equal(expected, BitConverter.ToInt32(save.RealData, 4));
        }

        [Fact]
        public void WriteUpdatesTheMooglePointsMirror() => OnSave(save =>
        {
            save.Data.McWork.MooglePoints = 9999;

            using (var stream = new MemoryStream())
            {
                save.Write(stream);
                stream.Position = 0;
                var actual = SaveKhRecom.Read(stream).Data;

                Assert.Equal(9999, actual.McWork.MooglePoints);
                Assert.Equal(9999, BitConverter.ToInt32(actual.RealData, 4));
            }
        });

        [Fact]
        public void WriteBackSaveUnchanged() => File.OpenRead("Saves/BISLPM-66676COM-01").Using(expected =>
            Helpers.AssertStream(expected, stream =>
            {
                var actual = new MemoryStream();
                SaveKhRecom.Read(stream).Write(actual);

                return actual;
            }));

        [Fact]
        public void CheckTables() => OnSave2(save =>
        {
            Assert.Equal(0xCCCCCC00, (uint)save.Data.Table0.Unknown00);

            Assert.Equal(0x01, save.Data.Table1.Unknown00);
            Assert.Equal(0xCC, save.Data.Table1.Unknown01);
            Assert.Equal(0xCC, save.Data.Table1.Unknown02);
            Assert.Equal(0xCC, save.Data.Table1.Unknown03);

            Assert.Equal(0x02, save.Data.Table2.Data[0]);
            Assert.Equal(0xCC, save.Data.Table2.Data[1]);
            Assert.Equal(0xCC, save.Data.Table2.Data[2]);
            Assert.Equal(0xCC, save.Data.Table2.Data[3]);

            Assert.Equal(0x00, save.Data.McWork.Data[0]);
            Assert.Equal(0x03, save.Data.McWork.Data[1]);
            Assert.Equal(0x02, save.Data.McWork.Data[2]);
            Assert.Equal(0x02, save.Data.McWork.Data[3]);
        });

        private static void OnSave(Action<SaveKhRecom> test)
        {
            const string FilePath = "Saves/BISLPM-66676COM-01";
            using (var stream = File.OpenRead(FilePath))
                test(SaveKhRecom.Read(stream));
        }

        private static void OnSave2(Action<SaveKhRecom> test)
        {
            const string FilePath = "Saves/BASLUS-21799COM-02";
            using (var stream = File.OpenRead(FilePath))
                test(SaveKhRecom.Read(stream));
        }

        private static void OnSaveData(Action<DataRecom> test) =>
            OnSave(save => test(save.Data));

        private static DataRecom ReadArchivedSave(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                Assert.True(ArchiveFactories.TryGetFactory(stream, out var archiveFactory));

                var archive = archiveFactory.Read(stream);
                using (var entryStream = new MemoryStream(archive.Entries[0].Data))
                    return SaveKhRecom.Read(entryStream).Data;
            }
        }
    }
}
