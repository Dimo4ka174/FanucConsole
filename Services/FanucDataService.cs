using System.Runtime.InteropServices;
using FanucFocasConsole.DB;
using FanucFocasConsole.DTO;
using FanucFocasConsole.Interop;
using Microsoft.Extensions.Logging;

namespace FanucFocasConsole.Services
{
    public class FanucDataService
    {
        private readonly IFocasNative _native;
        private readonly FanucRepository _repository;
        private readonly ILogger<FanucDataService> _logger;
        private ushort _handle;

        public FanucDataService(IFocasNative native, FanucRepository repository, ILogger<FanucDataService> logger)
        {
            _native = native;
            _repository = repository;
            _logger = logger;
        }

        public async Task RunTestAsync(string ip, ushort port, int timeoutSeconds, bool dbAvailable)
        {
            _logger.LogInformation("Starting FANUC data collection for {Ip}:{Port}", ip, port);

            // cnc_startupprocess is required on Linux before any other FOCAS call.
            // On Windows the DLL self-initializes, so we skip it there.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                short initRet = _native.StartupProcess(0, "focas.log");
                if (initRet != 0)
                    _logger.LogWarning("StartupProcess returned {Ret} ({Desc})",
                        initRet, FocasError.Describe(initRet));
                else
                    _logger.LogInformation("StartupProcess succeeded");
            }

            try
            {
                short ret = _native.Connect(ip, port, timeoutSeconds, out _handle);
                if (ret != 0)
                {
                    _logger.LogError("Failed to connect to {Ip}:{Port}. Code {Ret} ({Desc})",
                        ip, port, ret, FocasError.Describe(ret));
                    return;
                }
                _logger.LogInformation("Connected to {Ip}:{Port}. Handle: {Handle}", ip, port, _handle);

                for (int i = 0; i < 10; i++)
                {
                    _logger.LogInformation("--- Polling iteration {Iteration} for {Ip} ---", i + 1, ip);

                    var snapshot = await CollectSnapshotAsync(ip);

                    if (snapshot != null && dbAvailable)
                    {
                        try
                        {
                            await _repository.SaveSnapshotAsync(snapshot);
                            _logger.LogInformation("Snapshot saved to DB for {Ip}", ip);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to save snapshot for {Ip}. Continuing.", ip);
                        }
                    }
                    else if (snapshot != null)
                    {
                        _logger.LogWarning("Database unavailable, skipping save for {Ip}.", ip);
                    }

                    // EW_HANDLE (-8): the CNC closed the session on its side.
                    // Reconnect once; if that fails, stop polling this machine.
                    if (_handle == 0)
                    {
                        _logger.LogWarning("Handle lost for {Ip}. Reconnecting...", ip);
                        await Task.Delay(2000);

                        short reconnect = _native.Connect(ip, port, timeoutSeconds, out _handle);
                        if (reconnect != 0)
                        {
                            _logger.LogError("Reconnect to {Ip} failed with code {Ret} ({Desc}). Stopping.",
                                ip, reconnect, FocasError.Describe(reconnect));
                            break;
                        }
                        _logger.LogInformation("Reconnected to {Ip}. New handle: {Handle}", ip, _handle);
                    }

                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data collection for {Ip}", ip);
            }
            finally
            {
                if (_handle != 0)
                {
                    short discRet = _native.Disconnect(_handle);
                    _logger.LogInformation("Disconnected from machine. Ret={Ret}", discRet);
                    _handle = 0;
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _native.ExitProcess();
                    _logger.LogInformation("ExitProcess called");
                }
            }
        }

        // Returns null when the FOCAS handle is lost mid-iteration,
        // so a partial snapshot is never persisted.
        private Task<MachineSnapshot?> CollectSnapshotAsync(string ip)
        {
            var snapshot = new MachineSnapshot
            {
                Timestamp = DateTime.UtcNow,
                MachineIp = ip
            };

            if (Ok(_native.ReadStatInfo(_handle, out var stat), "ReadStatInfo"))
            {
                snapshot.RunStatus = stat.run;
                snapshot.Mode = stat.aut;
                snapshot.Emergency = stat.emergency != 0;
                snapshot.Alarm = stat.alarm != 0;
                _logger.LogInformation("ReadStatInfo: run={Run}, mode={Mode}, emergency={Emg}, alarm={Alm}",
                    stat.run, stat.aut, stat.emergency, stat.alarm);
            }

            if (Ok(_native.ReadAlarmStatus(_handle, out int alarmStatus), "ReadAlarmStatus"))
            {
                snapshot.Alarm = alarmStatus != 0;
                _logger.LogInformation("ReadAlarmStatus: alarmStatus={Alarm}", alarmStatus);
            }

            if (snapshot.Alarm)
            {
                if (Ok(_native.ReadAlarmInfo2(_handle, out var alarmInfo), "ReadAlarmInfo2"))
                {
                    var axes = new[] { alarmInfo.axis1, alarmInfo.axis2, alarmInfo.axis3, alarmInfo.axis4, alarmInfo.axis5 };
                    var nums = new[] { alarmInfo.alm_no1, alarmInfo.alm_no2, alarmInfo.alm_no3, alarmInfo.alm_no4, alarmInfo.alm_no5 };

                    for (int j = 0; j < 5; j++)
                    {
                        if (nums[j] == 0) continue;

                        var alarm = new AlarmInfo { Axis = axes[j], AlarmNumber = nums[j] };
                        short almNo = nums[j];

                        if (Ok(_native.ReadAlarmMessage2(_handle, axes[j], ref almNo, out var msg), "ReadAlarmMessage2"))
                        {
                            alarm.Message = msg.alm_msg?.TrimEnd('\0', ' ') ?? string.Empty;
                            _logger.LogInformation("Alarm {No} axis {Axis}: {Msg}", almNo, axes[j], alarm.Message);
                        }
                        snapshot.Alarms.Add(alarm);
                    }

                    if (snapshot.Alarms.Count > 0)
                    {
                        snapshot.AlarmMessage = string.Join("; ",
                            snapshot.Alarms.Select(a => $"{a.AlarmNumber}: {a.Message}"));
                    }
                }
            }

            if (Ok(_native.ReadOpMode(_handle, out short opMode), "ReadOpMode"))
            {
                snapshot.OpMode = opMode;
                _logger.LogInformation("ReadOpMode: opMode={OpMode}", opMode);
            }

            if (Ok(_native.ReadProgramNumber(_handle, out var pro), "ReadProgramNumber"))
            {
                snapshot.MainProgram = pro.mdata;
                snapshot.CurrentProgram = pro.data;
                _logger.LogInformation("ReadProgramNumber: main={Main}, current={Cur}", pro.mdata, pro.data);
            }

            if (Ok(_native.ReadProgramName(_handle, out var exeprg), "ReadProgramName"))
            {
                snapshot.ProgramName = exeprg.name?.TrimEnd('\0') ?? string.Empty;
                _logger.LogInformation("ReadProgramName: name={Name}", snapshot.ProgramName);
            }

            if (Ok(_native.ReadActs(_handle, out var actS), "ReadActs"))
            {
                snapshot.SpindleSpeed = actS.data;
                _logger.LogInformation("ReadActs: spindleSpeed={Spindle}", actS.data);
            }

            if (Ok(_native.ReadActf(_handle, out var actF), "ReadActf"))
            {
                snapshot.FeedRate = actF.data;
                _logger.LogInformation("ReadActf: feedRate={Feed}", actF.data);
            }

            if (Ok(_native.ReadSpindleLoad(_handle, 1, out var spLoad), "ReadSpindleLoad")
                && spLoad.data is { Length: > 0 })
            {
                snapshot.SpindleLoad = spLoad.data[0];
                _logger.LogInformation("ReadSpindleLoad: load={Load}", spLoad.data[0]);
            }

            if (Ok(_native.ReadServoLoad(_handle, 1, out var svLoad), "ReadServoLoad"))
            {
                snapshot.ServoLoadX = svLoad.data;
                _logger.LogInformation("ReadServoLoad: servoLoadX={Srv}", svLoad.data);
            }

            // cnc_rdparam(6750), cnc_rdparam(6712) and cnc_rdproctime are intentionally
            // not called: on the tested CNC models they always return EW_FUNC (2) or
            // EW_NODATA (6) because the corresponding options are not enabled.

            if (_handle == 0)
            {
                _logger.LogWarning("Handle lost during snapshot collection for {Ip}", ip);
                return Task.FromResult<MachineSnapshot?>(null);
            }

            return Task.FromResult<MachineSnapshot?>(snapshot);
        }

        // Wraps every FOCAS call: logs non-zero codes with a readable
        // description and resets _handle on EW_HANDLE (-8).
        private bool Ok(short ret, string operation)
        {
            if (ret == 0) return true;

            if (ret == -8)
            {
                _logger.LogWarning("{Operation}: handle lost (EW_HANDLE)", operation);
                _handle = 0;
                return false;
            }

            _logger.LogWarning("{Operation} failed with code {Ret} ({Desc})",
                operation, ret, FocasError.Describe(ret));
            return false;
        }
    }
}
