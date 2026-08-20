using Microsoft.Extensions.Logging;
using FanucFocasConsole.DTO;
using Npgsql;

namespace FanucFocasConsole.DB
{
    public class FanucRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<FanucRepository> _logger;

        public FanucRepository(string connectionString, ILogger<FanucRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task EnsureDatabaseExistsAsync()
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            string targetDb = builder.Database;
            builder.Database = "postgres";

            await using var conn = new NpgsqlConnection(builder.ConnectionString);
            await conn.OpenAsync();

            var cmdCheck = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{targetDb}'", conn);
            if (await cmdCheck.ExecuteScalarAsync() == null)
            {
                var cmdCreate = new NpgsqlCommand($"CREATE DATABASE \"{targetDb}\"", conn);
                await cmdCreate.ExecuteNonQueryAsync();
                _logger.LogInformation("Database {Db} created", targetDb);
            }
        }

        public async Task EnsureCreatedAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS snapshots (
                        id SERIAL PRIMARY KEY,
                        machine_ip VARCHAR(15) NOT NULL,
                        timestamp TIMESTAMPTZ NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS alarms (
                        id SERIAL PRIMARY KEY,
                        snapshot_id INT REFERENCES snapshots(id) ON DELETE CASCADE,
                        axis SMALLINT,
                        alarm_number SMALLINT,
                        message VARCHAR(256),
                        history TEXT
                    );
                    CREATE TABLE IF NOT EXISTS status (
                        id SERIAL PRIMARY KEY,
                        snapshot_id INT REFERENCES snapshots(id) ON DELETE CASCADE,
                        run_status SMALLINT,
                        mode SMALLINT,
                        op_mode SMALLINT,
                        main_program INT,
                        current_program INT,
                        program_name VARCHAR(36)
                    );
                    CREATE TABLE IF NOT EXISTS loads (
                        id SERIAL PRIMARY KEY,
                        snapshot_id INT REFERENCES snapshots(id) ON DELETE CASCADE,
                        spindle_load DOUBLE PRECISION,
                        servo_load_x DOUBLE PRECISION,
                        servo_load_y DOUBLE PRECISION,
                        servo_load_z DOUBLE PRECISION,
                        spindle_speed DOUBLE PRECISION,
                        feed_rate DOUBLE PRECISION
                    );
                    CREATE TABLE IF NOT EXISTS working_time (
                        id SERIAL PRIMARY KEY,
                        snapshot_id INT REFERENCES snapshots(id) ON DELETE CASCADE,
                        power_on_time_min DOUBLE PRECISION,
                        total_parts INT,
                        working_time_min DOUBLE PRECISION,
                        cutting_time_min DOUBLE PRECISION
                    );
                ", conn);
                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Database and tables are ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database tables");
                throw;
            }
        }

        public async Task SaveSnapshotAsync(MachineSnapshot dto)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // 1. snapshots
                var cmdSnapshot = new NpgsqlCommand(@"
                    INSERT INTO snapshots (machine_ip, timestamp)
                    VALUES (@ip, @ts)
                    RETURNING id;
                ", conn);
                cmdSnapshot.Parameters.AddWithValue("ip", dto.MachineIp);
                cmdSnapshot.Parameters.AddWithValue("ts", dto.Timestamp);
                int snapshotId = (int)(await cmdSnapshot.ExecuteScalarAsync() ?? 0);

                // 2. status
                if (dto.RunStatus != 0 || dto.Mode != 0 || dto.CurrentProgram != 0 || !string.IsNullOrEmpty(dto.ProgramName))
                {
                    var cmdStatus = new NpgsqlCommand(@"
                        INSERT INTO status (snapshot_id, run_status, mode, op_mode, main_program, current_program, program_name)
                        VALUES (@sid, @run, @mode, @opmode, @mainProg, @curProg, @progName)
                    ", conn);
                    cmdStatus.Parameters.AddWithValue("sid", snapshotId);
                    cmdStatus.Parameters.AddWithValue("run", dto.RunStatus);
                    cmdStatus.Parameters.AddWithValue("mode", dto.Mode);
                    cmdStatus.Parameters.AddWithValue("opmode", dto.OpMode);
                    cmdStatus.Parameters.AddWithValue("mainProg", dto.MainProgram);
                    cmdStatus.Parameters.AddWithValue("curProg", dto.CurrentProgram);
                    cmdStatus.Parameters.AddWithValue("progName", dto.ProgramName);
                    await cmdStatus.ExecuteNonQueryAsync();
                }

                // 3. loads
                if (dto.SpindleLoad != 0 || dto.ServoLoadX != 0 || dto.SpindleSpeed != 0 || dto.FeedRate != 0)
                {
                    var cmdLoad = new NpgsqlCommand(@"
                        INSERT INTO loads (snapshot_id, spindle_load, servo_load_x, servo_load_y, servo_load_z, spindle_speed, feed_rate)
                        VALUES (@sid, @spLoad, @svX, @svY, @svZ, @spSpeed, @feed)
                    ", conn);
                    cmdLoad.Parameters.AddWithValue("sid", snapshotId);
                    cmdLoad.Parameters.AddWithValue("spLoad", dto.SpindleLoad);
                    cmdLoad.Parameters.AddWithValue("svX", dto.ServoLoadX);
                    cmdLoad.Parameters.AddWithValue("svY", dto.ServoLoadY);
                    cmdLoad.Parameters.AddWithValue("svZ", dto.ServoLoadZ);
                    cmdLoad.Parameters.AddWithValue("spSpeed", dto.SpindleSpeed);
                    cmdLoad.Parameters.AddWithValue("feed", dto.FeedRate);
                    await cmdLoad.ExecuteNonQueryAsync();
                }

                // 4. working_time
                if (dto.PowerOnTimeMin != 0 || dto.TotalParts != 0 || dto.WorkingTimeMin != 0)
                {
                    var cmdTime = new NpgsqlCommand(@"
                        INSERT INTO working_time (snapshot_id, power_on_time_min, total_parts, working_time_min, cutting_time_min)
                        VALUES (@sid, @powerTime, @totalParts, @workTime, @cutTime)
                    ", conn);
                    cmdTime.Parameters.AddWithValue("sid", snapshotId);
                    cmdTime.Parameters.AddWithValue("powerTime", dto.PowerOnTimeMin);
                    cmdTime.Parameters.AddWithValue("totalParts", dto.TotalParts);
                    cmdTime.Parameters.AddWithValue("workTime", dto.WorkingTimeMin);
                    cmdTime.Parameters.AddWithValue("cutTime", dto.CuttingTimeMin);
                    await cmdTime.ExecuteNonQueryAsync();
                }

                // 5. alarms
                foreach (var alarm in dto.Alarms)
                {
                    var cmdAlarm = new NpgsqlCommand(@"
                        INSERT INTO alarms (snapshot_id, axis, alarm_number, message, history)
                        VALUES (@sid, @axis, @num, @msg, @hist)
                    ", conn);
                    cmdAlarm.Parameters.AddWithValue("sid", snapshotId);
                    cmdAlarm.Parameters.AddWithValue("axis", alarm.Axis);
                    cmdAlarm.Parameters.AddWithValue("num", alarm.AlarmNumber);
                    cmdAlarm.Parameters.AddWithValue("msg", alarm.Message);
                    cmdAlarm.Parameters.AddWithValue("hist", alarm.History);
                    await cmdAlarm.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Snapshot for {Ip} written to DB", dto.MachineIp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save snapshot for {Ip}", dto.MachineIp);
                throw;
            }
        }
    }
}
