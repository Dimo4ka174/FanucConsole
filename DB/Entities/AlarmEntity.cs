namespace FanucFocasConsole.DB.Entities
{
    public class AlarmEntity
    {
        public int Id { get; set; }
        public int SnapshotId { get; set; }
        public short Axis { get; set; }
        public short AlarmNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string History { get; set; } = string.Empty;
    }
}
