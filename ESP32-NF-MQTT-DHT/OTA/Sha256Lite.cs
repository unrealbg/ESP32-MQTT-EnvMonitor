namespace ESP32_NF_MQTT_DHT.OTA
{
    internal static class Sha256Lite
    {
        // SHA‑256 constants
        private static readonly uint[] K = new uint[]
                                               {
                                                   0x428A2F98,0x71374491,0xB5C0FBCF,0xE9B5DBA5,0x3956C25B,0x59F111F1,0x923F82A4,0xAB1C5ED5,
                                                   0xD807AA98,0x12835B01,0x243185BE,0x550C7DC3,0x72BE5D74,0x80DEB1FE,0x9BDC06A7,0xC19BF174,
                                                   0xE49B69C1,0xEFBE4786,0x0FC19DC6,0x240CA1CC,0x2DE92C6F,0x4A7484AA,0x5CB0A9DC,0x76F988DA,
                                                   0x983E5152,0xA831C66D,0xB00327C8,0xBF597FC7,0xC6E00BF3,0xD5A79147,0x06CA6351,0x14292967,
                                                   0x27B70A85,0x2E1B2138,0x4D2C6DFC,0x53380D13,0x650A7354,0x766A0ABB,0x81C2C92E,0x92722C85,
                                                   0xA2BFE8A1,0xA81A664B,0xC24B8B70,0xC76C51A3,0xD192E819,0xD6990624,0xF40E3585,0x106AA070,
                                                   0x19A4C116,0x1E376C08,0x2748774C,0x34B0BCB5,0x391C0CB3,0x4ED8AA4A,0x5B9CCA4F,0x682E6FF3,
                                                   0x748F82EE,0x78A5636F,0x84C87814,0x8CC70208,0x90BEFFFA,0xA4506CEB,0xBEF9A3F7,0xC67178F2
                                               };

        public static byte[] ComputeHash(byte[] data)
        {
            // Initial hash values
            uint h0 = 0x6A09E667, h1 = 0xBB67AE85, h2 = 0x3C6EF372, h3 = 0xA54FF53A,
                 h4 = 0x510E527F, h5 = 0x9B05688C, h6 = 0x1F83D9AB, h7 = 0x5BE0CD19;

            // Preprocessing (padding)
            int origLen = data.Length;
            ulong bitLen = (ulong)origLen * 8UL;

            // new length = orig + 1 (0x80) + pad zeros + 8 (length)
            int newLen = origLen + 1;
            while ((newLen % 64) != 56)
            {
                newLen++;
            }

            byte[] msg = new byte[newLen + 8];
            for (int i = 0; i < origLen; i++)
            {
                msg[i] = data[i];
            }

            msg[origLen] = 0x80;
            // append length big‑endian
            for (int i = 0; i < 8; i++)
            {
                msg[newLen + i] = (byte)((bitLen >> (8 * (7 - i))) & 0xFF);
            }

            uint[] w = new uint[64];
            for (int chunk = 0; chunk < msg.Length; chunk += 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    int j = chunk + i * 4;
                    w[i] = (uint)(msg[j] << 24 | msg[j + 1] << 16 | msg[j + 2] << 8 | msg[j + 3]);
                }

                for (int i = 16; i < 64; i++)
                {
                    uint s0 = ROR(w[i - 15], 7) ^ ROR(w[i - 15], 18) ^ (w[i - 15] >> 3);
                    uint s1 = ROR(w[i - 2], 17) ^ ROR(w[i - 2], 19) ^ (w[i - 2] >> 10);
                    w[i] = w[i - 16] + s0 + w[i - 7] + s1;
                }

                uint a = h0, b = h1, c = h2, d = h3, e = h4, f = h5, g = h6, h = h7;
                for (int i = 0; i < 64; i++)
                {
                    uint S1 = ROR(e, 6) ^ ROR(e, 11) ^ ROR(e, 25);
                    uint ch = (e & f) ^ ((~e) & g);
                    uint temp1 = h + S1 + ch + K[i] + w[i];
                    uint S0 = ROR(a, 2) ^ ROR(a, 13) ^ ROR(a, 22);
                    uint maj = (a & b) ^ (a & c) ^ (b & c);
                    uint temp2 = S0 + maj;

                    h = g; g = f; f = e; e = d + temp1; d = c; c = b; b = a; a = temp1 + temp2;
                }

                h0 += a; h1 += b; h2 += c; h3 += d; h4 += e; h5 += f; h6 += g; h7 += h;
            }

            byte[] hash = new byte[32];
            WriteBE(hash, 0, h0);
            WriteBE(hash, 4, h1);
            WriteBE(hash, 8, h2);
            WriteBE(hash, 12, h3);
            WriteBE(hash, 16, h4);
            WriteBE(hash, 20, h5);
            WriteBE(hash, 24, h6);
            WriteBE(hash, 28, h7);
            return hash;
        }

        private static uint ROR(uint x, int n)
        {
            return (x >> n) | (x << (32 - n));
        }

        private static void WriteBE(byte[] b, int off, uint v)
        {
            b[off] = (byte)(v >> 24);
            b[off + 1] = (byte)(v >> 16);
            b[off + 2] = (byte)(v >> 8);
            b[off + 3] = (byte)(v);
        }
    }
}