namespace ESP32_NF_MQTT_DHT.OTA
{
    internal sealed class StrVec
    {
        private object[] _buf = new object[4];

        private int _count = 0;

        public void Add(object x)
        {
            if (this._count == this._buf.Length)
            {
                this.Grow();
            }

            this._buf[this._count++] = x;
        }

        public OtaFile[] ToArray()
        {
            var a = new OtaFile[this._count];

            for (int i = 0; i < this._count; i++)
            {
                a[i] = (OtaFile)this._buf[i];
            }

            return a;
        }

        private void Grow()
        {
            var n = this._buf.Length * 2;
            var nb = new object[n];

            for (int i = 0; i < this._buf.Length; i++)
            {
                nb[i] = this._buf[i];
            }

            this._buf = nb;
        }
    }
}