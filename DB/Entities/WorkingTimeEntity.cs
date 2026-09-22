namespace FanucFocasConsole.DB.Entities
{
    public class WorkingTimeEntity
    {
        public int Id { get; set; }
        public int SnapshotId { get; set; }
        public double PowerOnTimeMin { get; set; }
        public int TotalParts { get; set; }
        public double WorkingTimeMin { get; set; }
        public double CuttingTimeMin { get; set; }
    }
}
