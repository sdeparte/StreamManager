namespace StreamManager.Model
{
    public class StreamConfig
    {
        public string Title { get; set; }

        public Category Category { get; set; }

        public override string ToString()
        {
            return $"Titre : \"{Title}\" | Catégorie : \"{Category.Name}\"";
        }
    }
}
