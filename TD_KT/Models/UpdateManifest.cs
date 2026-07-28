namespace TD_KT.Models
{
    public sealed class UpdateManifest
    {
        public string application { get; set; }
        public string version { get; set; }
        public string package { get; set; }
        public bool mandatory { get; set; }
        public string sha256 { get; set; }
        public string description { get; set; }
    }
}
