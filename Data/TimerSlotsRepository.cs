using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace colizeumUpdateManager.Data
{
    public sealed class TimerSlotRow
    {
        public int SlotId { get; init; }
        public string Note { get; set; } = "";
        public string GoalText { get; set; } = "";
        public long ElapsedMs { get; set; }
        public bool IsRunning { get; set; }
    }

    public class TimerSlotsRepository
    {
        public async Task EnsureSlotsExist()
        {
            const string ddl = """
            CREATE TABLE IF NOT EXISTS timer_slots (
                slot_id        INT PRIMARY KEY,
                note           TEXT NOT NULL DEFAULT '',
                goal_text      TEXT NOT NULL DEFAULT '',
                elapsed_ms     BIGINT NOT NULL DEFAULT 0,
                is_running     BOOLEAN NOT NULL DEFAULT FALSE,
                last_saved_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
                updated_at     TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            """;

            const string seed = """
            INSERT INTO timer_slots (slot_id)
            VALUES (1),(2),(3),(4),(5),(6)
            ON CONFLICT DO NOTHING;
            """;

            await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
            await conn.OpenAsync();

            await using (var cmd = new NpgsqlCommand(ddl, conn))
                await cmd.ExecuteNonQueryAsync();

            await using (var cmd = new NpgsqlCommand(seed, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<TimerSlotRow>> GetAll()
        {
            const string sql = """
            SELECT slot_id, note, goal_text, elapsed_ms, is_running
            FROM timer_slots
            ORDER BY slot_id
            """;

            var list = new List<TimerSlotRow>();

            await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new TimerSlotRow
                {
                    SlotId = reader.GetInt32(0),
                    Note = reader.GetString(1),
                    GoalText = reader.GetString(2),
                    ElapsedMs = reader.GetInt64(3),
                    IsRunning = reader.GetBoolean(4)
                });
            }

            return list;
        }

        public async Task Update(TimerSlotRow row)
        {
            const string sql = """
            UPDATE timer_slots
            SET note = @note,
                goal_text = @goal,
                elapsed_ms = @elapsed,
                is_running = @running,
                last_saved_at = now(),
                updated_at = now()
            WHERE slot_id = @slot
            """;

            await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("slot", row.SlotId);
            cmd.Parameters.AddWithValue("note", row.Note ?? "");
            cmd.Parameters.AddWithValue("goal", row.GoalText ?? "");
            cmd.Parameters.AddWithValue("elapsed", row.ElapsedMs);
            cmd.Parameters.AddWithValue("running", row.IsRunning);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Гарантируем правило: после перезапуска таймеры не продолжают бежать сами.
        /// </summary>
        public async Task ResetRunningFlags()
        {
            const string sql = "UPDATE timer_slots SET is_running = FALSE;";
            await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
