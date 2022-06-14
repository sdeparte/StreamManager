namespace StreamManager.Model
{
    public class Playlist
    {
        public string Name { get; set; }

        public string Dossier { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
