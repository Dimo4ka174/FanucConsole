namespace FanucFocasConsole.Interop
{
    public interface IFocasNative
    {
        // Lifecycle
        short StartupProcess(int type, string logfile);
        short ExitProcess();

        // Connection
        short Connect(string ip, ushort port, int timeout, out ushort handle);
        short Disconnect(ushort handle);

        // Status
        short ReadStatInfo(ushort handle, out ODBST stat);
        short ReadAlarmStatus(ushort handle, out int alarmStatus);   // cnc_alarm2
        short ReadOpMode(ushort handle, out short mode);             // cnc_rdopmode

        // Program
        short ReadProgramNumber(ushort handle, out ODBPRO pro);
        short ReadProgramName(ushort handle, out ODBEXEPRG exeprg);

        // Speeds and loads
        short ReadActs(ushort handle, out ODBACT act);               // spindle speed
        short ReadActf(ushort handle, out ODBACT act);               // feed rate
        short ReadSpindleLoad(ushort handle, short spindleNo, out ODBSPN load);
        short ReadServoLoad(ushort handle, short axisNo, out ODBSVLOAD load);
        short ReadSpindleMeter(ushort handle, short type, short spindleNo, out ODBSPLOAD meter);

        // Parameters
        short ReadParameter(ushort handle, short paramNo, short axis, short type, out IODBPSD param);
        short ReadProcTime(ushort handle, out ODBPTIME ptime);

        // Alarms
        short ReadAlarmInfo2(ushort handle, out ALMINFO2 info);
        short ReadAlarmMessage2(ushort handle, short axis, ref short almNo, out ODBALMMSG2 msg);
    }
}
