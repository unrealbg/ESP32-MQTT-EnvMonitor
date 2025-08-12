namespace ESP32_NF_MQTT_DHT.OTA
{
    internal sealed class OtaManifest
    {
        public string Version;
        public OtaFile[] Files;

        public static OtaManifest Parse(string json)
        {
            if (json == null)
            {
                return null;
            }

            var m = new OtaManifest();
            m.Version = OtaUtil.JsonGetString(json, "version");

            // Parse all objects inside files[]
            var objs = OtaUtil.JsonArrayObjects(json, "files");
            if (objs != null && objs.Length > 0)
            {
                var vec = new StrVec();
                for (int i = 0; i < objs.Length; i++)
                {
                    var o = objs[i];
                    var f = new OtaFile();
                    f.Name = OtaUtil.JsonGetString(o, "name");
                    f.Url = OtaUtil.JsonGetString(o, "url");
                    f.Sha256 = OtaUtil.JsonGetString(o, "sha256");
                    if (!string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Url)
                                                      && !string.IsNullOrEmpty(f.Sha256))
                        vec.Add(f);
                }

                m.Files = vec.ToArray();
            }

            return m;
        }
    }
}