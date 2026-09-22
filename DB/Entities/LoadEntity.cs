namespace FanucFocasConsole.DB.Entities
{
    public class LoadEntity
    {
        public int Id { get; set; }
        public int SnapshotId { get; set; }
        public double SpindleLoad { get; set; }
        public double ServoLoadX { get; set; }
        public double ServoLoadY { get; set; }
        public double ServoLoadZ { get; set; }
        public double SpindleSpeed { get; set; }
        public double FeedRate { get; set; }
    }
}
