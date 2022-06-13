namespace StreamManager.Model
{
    public class StreamConfig
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public Category Category { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
