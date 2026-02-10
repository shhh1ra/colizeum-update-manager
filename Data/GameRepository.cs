using colizeumUpdateManager.Models;
using Npgsql;

namespace colizeumUpdateManager.Data;

public class GameRepository
{
    public async Task<List<Pc>> GetPcs()
    {
        var list = new List<Pc>();

        await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT id, name FROM pcs ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Pc
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    public async Task<List<PcGame>> GetGamesForPc(int pcId, DateTime? date = null)
    {
        var list = new List<PcGame>();

        var actualDate = (date ?? DateTime.Today).Date;
        var yesterday = actualDate.AddDays(-1);

        const string sql = """
            SELECT 
                g.id,
                g.name,
                COALESCE(s_today.status, 0) AS status_today,
                COALESCE(s_yest.status, 0)  AS status_yest
            FROM pc_required_games prg
            JOIN games g 
                    ON g.id = prg.game_id
            LEFT JOIN pc_game_status s_today
                    ON s_today.pc_id = prg.pc_id
                AND s_today.game_id = prg.game_id
                AND s_today.status_date = @date
            LEFT JOIN pc_game_status s_yest
                    ON s_yest.pc_id = prg.pc_id
                AND s_yest.game_id = prg.game_id
                AND s_yest.status_date = @yest
            WHERE prg.pc_id = @pc
            ORDER BY g.name
            """;

        await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("pc", pcId);
        cmd.Parameters.AddWithValue("date", actualDate);
        cmd.Parameters.AddWithValue("yest", yesterday);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new PcGame
            {
                PcId = pcId,
                GameId = reader.GetInt32(0),
                GameName = reader.GetString(1),
                Status = (UpdateStatus)reader.GetInt16(2),
                YesterdayStatus = (UpdateStatus)reader.GetInt16(3)
            });
        }

        return list;
    }

    public async Task<bool> HasAnyStatusForDate(int pcId, DateTime date)
    {
        const string sql = """
            SELECT EXISTS (
            SELECT 1
            FROM pc_game_status
            WHERE pc_id = @pc AND status_date = @date
            )
            """;

        await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("pc", pcId);
        cmd.Parameters.AddWithValue("date", date.Date);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }

    public async Task<List<int>> GetRequiredGameIdsForPc(int pcId)
    {
        var list = new List<int>();

        const string sql = """
            SELECT game_id
            FROM pc_required_games
            WHERE pc_id = @pc
            """;

        await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("pc", pcId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(reader.GetInt32(0));

        return list;
    }

    public async Task SaveStatus(int pcId, int gameId, UpdateStatus status, DateTime? date = null)
    {
        await using var conn = new NpgsqlConnection(DbConfig.ConnectionString);
        await conn.OpenAsync();

        var actualDate = (date ?? DateTime.Today).Date;

        var cmd = new NpgsqlCommand(@"
            INSERT INTO pc_game_status (pc_id, game_id, status, status_date)
            VALUES (@pc, @game, @st, @date)
            ON CONFLICT (pc_id, game_id, status_date)
            DO UPDATE SET status = @st
            ", conn);

        cmd.Parameters.AddWithValue("pc", pcId);
        cmd.Parameters.AddWithValue("game", gameId);
        cmd.Parameters.AddWithValue("st", (short)status);
        cmd.Parameters.AddWithValue("date", actualDate);

        await cmd.ExecuteNonQueryAsync();
    }
}
