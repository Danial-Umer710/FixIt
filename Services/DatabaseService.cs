using FixIt.Models;
using SQLite;

namespace FixIt.Services;

/// <summary>SQLite persistence for <see cref="IssueReport"/> (Create, Read, Update, Delete).</summary>
public class DatabaseService
{
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "fixit.db3");

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                var connection = new SQLiteAsyncConnection(DatabasePath, Flags);
                await connection.CreateTableAsync<IssueReport>();
                _connection = connection;
            }
            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // READ
    public async Task<List<IssueReport>> GetReportsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<IssueReport>().OrderByDescending(r => r.UpdatedAt).ToListAsync();
    }

    public async Task<IssueReport?> GetReportAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<IssueReport>().Where(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<IssueReport>> GetReportsPendingAddressAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<IssueReport>().Where(r => r.AddressPending == true).ToListAsync();
    }

    // CREATE + UPDATE
    /// <param name="touch">Update the "last changed" timestamp (false for background fixes like address sync).</param>
    public async Task SaveReportAsync(IssueReport report, bool touch = true)
    {
        var db = await GetConnectionAsync();
        var now = DateTime.Now;

        if (report.Id == 0)
        {
            report.CreatedAt = now;
            report.UpdatedAt = now;
            await db.InsertAsync(report);
        }
        else
        {
            if (touch) report.UpdatedAt = now;
            await db.UpdateAsync(report);
        }
    }

    // DELETE
    public async Task DeleteReportAsync(IssueReport report)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(report);
    }
}
