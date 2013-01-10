using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct Key128
    {
        public uint Part1;
        public uint Part2;
        public uint Part3;
        public uint Part4;

        public override string ToString()
        {
            return string.Format("{0:x}{1:x}{2:x}{3:x}", Part1, Part2, Part3, Part4);
        }
    }

    class Murmur
    {
        public static Key128 Hash(Stream stream, uint seed)
        {
            const uint c1 = 0x239b961b;
            const uint c2 = 0xab0e9789;
            const uint c3 = 0x38b34ae5;
            const uint c4 = 0xa1e38b93;

            uint h1 = seed;
            uint h2 = seed;
            uint h3 = seed;
            uint h4 = seed;

            byte[] chunk = null;
            uint streamLength = 0;
            using (var reader = new BinaryReader(stream))
            {
                while ((chunk = reader.ReadBytes(16)).Length == 16)
                {
                    streamLength += (uint)chunk.Length;
                    uint k1 = (uint)(chunk[0] | chunk[1] << 8 | chunk[2] << 16 | chunk[3] << 24);
                    uint k2 = (uint)(chunk[4] | chunk[5] << 8 | chunk[6] << 16 | chunk[7] << 24);
                    uint k3 = (uint)(chunk[8] | chunk[9] << 8 | chunk[10] << 16 | chunk[11] << 24);
                    uint k4 = (uint)(chunk[12] | chunk[13] << 8 | chunk[14] << 16 | chunk[15] << 24);

                    k1 *= c1; k1 = rotl32(k1, 15); k1 *= c2; h1 ^= k1;
                    h1 = rotl32(h1, 19); h1 += h2; h1 = h1 * 5 + 0x561ccd1b;
                    k2 *= c2; k2 = rotl32(k2, 16); k2 *= c3; h2 ^= k2;
                    h2 = rotl32(h2, 17); h2 += h3; h2 = h2 * 5 + 0x0bcaa747;
                    k3 *= c3; k3 = rotl32(k3, 17); k3 *= c4; h3 ^= k3;
                    h3 = rotl32(h3, 15); h3 += h4; h3 = h3 * 5 + 0x96cd1c35;
                    k4 *= c4; k4 = rotl32(k4, 18); k4 *= c1; h4 ^= k4;
                    h4 = rotl32(h4, 13); h4 += h1; h4 = h4 * 5 + 0x32ac3b17;
                }
            }

            uint m1 = 0;
            uint m2 = 0;
            uint m3 = 0;
            uint m4 = 0;

            streamLength += (uint)chunk.Length;
            switch (chunk.Length)
            {
                case 15: m4 ^= (uint)chunk[14] << 16; goto case 14;
                case 14: m4 ^= (uint)chunk[13] << 8; goto case 13;
                case 13:
                    m4 ^= (uint)chunk[12] << 0;
                    m4 *= c4; m4 = rotl32(m4, 18); m4 *= c1; h4 ^= m4;
                    goto case 12;

                case 12: m3 ^= (uint)chunk[11] << 24; goto case 11;
                case 11: m3 ^= (uint)chunk[10] << 16; goto case 10;
                case 10: m3 ^= (uint)chunk[9] << 8; goto case 9;
                case 9:
                    m3 ^= (uint)chunk[8] << 0;
                    m3 *= c3; m3 = rotl32(m3, 17); m3 *= c4; h3 ^= m3;
                    goto case 8;

                case 8: m2 ^= (uint)chunk[7] << 24; goto case 7;
                case 7: m2 ^= (uint)chunk[6] << 16; goto case 6;
                case 6: m2 ^= (uint)chunk[5] << 8; goto case 5;
                case 5:
                    m2 ^= (uint)chunk[4] << 0;
                    m2 *= c2; m2 = rotl32(m2, 16); m2 *= c3; h2 ^= m2;
                    goto case 4;

                case 4: m1 ^= (uint)chunk[3] << 24; goto case 3;
                case 3: m1 ^= (uint)chunk[2] << 16; goto case 2;
                case 2: m1 ^= (uint)chunk[1] << 8; goto case 1;
                case 1:
                    m1 ^= (uint)chunk[0] << 0;
                    m1 *= c1; m1 = rotl32(m1, 15); m1 *= c2; h1 ^= m1;
                    break;
            }

            h1 ^= streamLength;
            h2 ^= streamLength;
            h3 ^= streamLength;
            h4 ^= streamLength;

            h1 += h2; h1 += h3; h1 += h4;
            h2 += h1; h3 += h1; h4 += h1;

            h1 = fmix(h1);
            h2 = fmix(h2);
            h3 = fmix(h3);
            h4 = fmix(h4);

            h1 += h2; h1 += h3; h1 += h4;
            h2 += h1; h3 += h1; h4 += h1;

            // finalization, magic chants to wrap it all up
            h1 ^= streamLength;
            h1 = fmix(h1);

            Key128 result;
            result.Part1 = h1;
            result.Part2 = h2;
            result.Part3 = h3;
            result.Part4 = h4;

            return result;
        }

        public unsafe static int Hash(string input, uint seed)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            uint h1 = seed;
            uint k1 = 0;
            uint streamLength = (uint)input.Length * sizeof(char);

            uint sizet = sizeof(int);
            uint nblocks = streamLength / sizet;
            nblocks *= sizet;

            fixed (char* ptr = input)
            {
                uint i = 0;
                byte* chunk = (byte*)ptr;
                for (; i < nblocks; i += sizet)
                {
                    k1 = (uint)(chunk[0] | chunk[1] << 8 | chunk[2] << 16 | chunk[3] << 24);
                    k1 *= c1;
                    k1 = rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    h1 = rotl32(h1, 13);
                    h1 = h1 * 5 + 0xe6546b64;
                    chunk += 4;
                }

                k1 = 0;
                switch (streamLength - i)
                {
                    case 3: k1 = (uint)(chunk[1] << 8 | chunk[2] << 16); goto case 1;
                    case 2: k1 = (uint)(chunk[1] << 8); goto case 1;
                    case 1:
                        k1 |= (uint)chunk[0];
                        k1 *= c1;
                        k1 = rotl32(k1, 15);
                        k1 *= c2;
                        h1 ^= k1;
                        break;
                }                
            }

            // finalization, magic chants to wrap it all up
            h1 ^= streamLength;
            h1 = fmix(h1);

            unchecked { return (int)h1; }
        }

        static uint rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        static uint fmix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }
}
