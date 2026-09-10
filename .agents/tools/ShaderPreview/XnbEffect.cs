using System;
using System.IO;
using System.Text;

namespace ShaderPreview
{
    /// <summary>
    /// Pulls the compiled shader blob out of an XNB container. Terraria/tModLoader effect XNBs are
    /// uncompressed with a single EffectReader, so this is: header -> reader manifest -> blob length
    /// -> bytecode. Refuses loudly rather than guessing if one turns up compressed.
    /// </summary>
    internal static class XnbEffect
    {
        internal static byte[] Extract(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            if (reader.ReadByte() != 'X' || reader.ReadByte() != 'N' || reader.ReadByte() != 'B')
            {
                throw new InvalidDataException($"not an XNB: {path}");
            }

            reader.ReadByte();          // platform
            reader.ReadByte();          // format version
            byte flags = reader.ReadByte();
            reader.ReadInt32();         // file size

            if ((flags & 0xC0) != 0)
            {
                throw new NotSupportedException($"compressed XNB (flags 0x{flags:X2}): {path}");
            }

            int readerCount = Read7Bit(reader);
            for (int i = 0; i < readerCount; i++)
            {
                reader.ReadString();
                reader.ReadInt32();
            }

            Read7Bit(reader);           // shared resource count
            Read7Bit(reader);           // type id
            int length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        private static int Read7Bit(BinaryReader reader)
        {
            int result = 0;
            int shift = 0;
            while (true)
            {
                byte b = reader.ReadByte();
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) { return result; }
                shift += 7;
            }
        }
    }
}
