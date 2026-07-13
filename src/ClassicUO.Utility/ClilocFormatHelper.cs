using System;
using System.Buffers.Binary;

namespace ClassicUO.Utility
{
    public static class ClilocFormatHelper
    {
        public static bool UsesBwtCompression(ReadOnlySpan<byte> fileBytes, ClientVersion version)
        {
            if (version < ClientVersion.CV_7010400)
            {
                return false;
            }

            if (LooksLikeUncompressedCliloc(fileBytes))
            {
                return false;
            }

            return true;
        }

        public static byte[] GetClilocPayload(byte[] rawBytes, ClientVersion version)
        {
            if (!UsesBwtCompression(rawBytes, version))
            {
                return rawBytes;
            }

            byte[] decompressed = BwtDecompress.Decompress(rawBytes);

            if (decompressed.Length >= 13 && LooksLikeUncompressedCliloc(decompressed))
            {
                return decompressed;
            }

            if (LooksLikeUncompressedCliloc(rawBytes))
            {
                return rawBytes;
            }

            return decompressed.Length > 0 ? decompressed : rawBytes;
        }

        private static bool LooksLikeUncompressedCliloc(ReadOnlySpan<byte> data)
        {
            if (data.Length < 13)
            {
                return false;
            }

            int id = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(6));
            int length = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(11));

            if (id <= 0 || id > 5_000_000)
            {
                return false;
            }

            if (length <= 0 || length > 16_384)
            {
                return false;
            }

            return 6 + 7 + length <= data.Length;
        }
    }
}