namespace ESP32_NF_MQTT_DHT.OTA
{
    using System;

    internal static class OtaCrypto
    {
        // Uses built‑in pure C# SHA‑256 impl to avoid external dependency.
        public static bool VerifySha256Hex(byte[] data, string expectedHex)
        {
            if (string.IsNullOrEmpty(expectedHex))
            {
                return false;
            }

            try
            {
                var hash = Sha256Lite.ComputeHash(data);
                return HexEqualsIgnoreCase(hash, expectedHex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CRYPTO] " + ex.Message);
                return false;
            }
        }

        // Helper: convert a byte[] to lowercase hex for diagnostics/logging
        public static string ToHexLower(byte[] bytes)
        {
            if (bytes == null) return null;
            var c = new char[bytes.Length * 2];
            int i = 0;
            for (int j = 0; j < bytes.Length; j++)
            {
                byte b = bytes[j];
                c[i++] = GetHex(b >> 4);
                c[i++] = GetHex(b & 0xF);
            }
            return new string(c);
        }

        private static bool HexEqualsIgnoreCase(byte[] hash, string expectedHex)
        {
            int n = hash.Length;
            if (expectedHex.Length != n * 2)
            {
                return false;
            }

            for (int i = 0; i < n; i++)
            {
                byte b = hash[i];
                char c1 = expectedHex[i * 2];
                char c2 = expectedHex[i * 2 + 1];
                int hi = HexNibbleToInt(c1);
                int lo = HexNibbleToInt(c2);
                if (hi < 0 || lo < 0)
                {
                    return false;
                }

                byte v = (byte)((hi << 4) | lo);
                if (v != b)
                {
                    return false;
                }
            }

            return true;
        }

        private static int HexNibbleToInt(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            return -1;
        }

        private static char GetHex(int n)
        {
            return (char)(n < 10 ? ('0' + n) : ('a' + (n - 10)));
        }
    }
}