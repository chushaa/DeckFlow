namespace DeckFlow.Data.Database;

using DeckFlow.Data.Models;
using SQLite;

public class AppDatabase
{
	private readonly SQLiteAsyncConnection _connection;
	private readonly SemaphoreSlim _initLock = new(1, 1);
	private volatile bool _initialized;

	public AppDatabase(string dbPath)
	{
		_connection = new SQLiteAsyncConnection(dbPath);
	}

	public SQLiteAsyncConnection Connection => _connection;

	public async Task EnsureInitializedAsync()
	{
		if (_initialized)
			return;

		await _initLock.WaitAsync();
		try
		{
			if (_initialized)
				return;

			await _connection.CreateTableAsync<CardModel>();
			await _connection.CreateTableAsync<PrintingModel>();
			await _connection.CreateTableAsync<LocationModel>();
			await _connection.CreateTableAsync<OwnedCopyModel>();
			await _connection.CreateTableAsync<DeckModel>();
			await _connection.CreateTableAsync<DeckRequirementModel>();

			// Migration: existing locations get C# defaults that sqlite-net-pcl won't apply
			await _connection.ExecuteAsync(
				"UPDATE Locations SET AvailableForDeckAssignment = 1, Color = '#2D8A96' WHERE Color IS NULL");

			_initialized = true;
		}
		finally
		{
			_initLock.Release();
		}
	}
}