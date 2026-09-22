namespace FanucFocasConsole.DB.Entities
{
    public class StatusEntity
    {
        public int Id { get; set; }
        public int SnapshotId { get; set; }
        public short RunStatus { get; set; }
        public short Mode { get; set; }
        public short OpMode { get; set; }
        public int MainProgram { get; set; }
        public int CurrentProgram { get; set; }
        public string ProgramName { get; set; } = string.Empty;
    }
}
