namespace FanucFocasConsole.DB.Entities
{
    public class SnapshotEntity
    {
        public int Id { get; set; }
        public string MachineIp { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
