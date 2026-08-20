namespace FanucFocasConsole.DTO
{
    public class MachineSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MachineIp { get; set; } = string.Empty;

        // Status
        public short RunStatus { get; set; }          // 0 = stopped, 1 = running, 5 = alarm
        public short Mode { get; set; }               // selected mode (0 = MDI, 1 = MEM, 4 = JOG, ...)
        public bool Emergency { get; set; }
        public bool Alarm { get; set; }
        public string AlarmMessage { get; set; } = string.Empty;
        public short OpMode { get; set; }

        // Program
        public int MainProgram { get; set; }
        public int CurrentProgram { get; set; }
        public string ProgramName { get; set; } = string.Empty;

        // Counters and times (minutes)
        public int TotalParts { get; set; }
        public int PartsPerCycle { get; set; }
        public double WorkingTimeMin { get; set; }
        public double CuttingTimeMin { get; set; }
        public double PowerOnTimeMin { get; set; }

        // Positions (X, Y, Z for reference)
        public double XAbsolute { get; set; }
        public double YAbsolute { get; set; }
        public double ZAbsolute { get; set; }

        // Speeds and loads
        public double SpindleSpeed { get; set; }
        public double FeedRate { get; set; }
        public double SpindleLoad { get; set; }       // %
        public double ServoLoadX { get; set; }        // %
        public double ServoLoadY { get; set; }
        public double ServoLoadZ { get; set; }

        // Active alarms (may be empty)
        public List<AlarmInfo> Alarms { get; set; } = new();
    }

    public class AlarmInfo
    {
        public short Axis { get; set; }
        public short AlarmNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string History { get; set; } = string.Empty;
    }
}
