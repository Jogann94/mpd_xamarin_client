using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using MPDApp.Models;

namespace MPDApp.Services
{
	public class ServerProfileDatabase
	{
		readonly SQLiteAsyncConnection database;

		private object _lock = new object();

		public ServerProfileDatabase(string dbPath)
		{
			database = new SQLiteAsyncConnection(dbPath);
			database.CreateTableAsync<MPDServerProfile>().Wait();
		}

		public Task<List<MPDServerProfile>> GetProfilesAsync()
		{
			lock(_lock)
			{
				return database.Table<MPDServerProfile>().ToListAsync();
			}
		}

		public Task<MPDServerProfile> GetProfileAsync(int id)
		{
			lock(_lock)
			{
				return database.Table<MPDServerProfile>().Where(i => i.ID == id).FirstOrDefaultAsync();
			}
		}

		public Task<int> SaveProfileAsync(MPDServerProfile profile)
		{
			lock (_lock)
			{
				if (profile.ID != 0)
				{
					return database.UpdateAsync(profile);
				}
				else
				{
					return database.InsertAsync(profile);
				}
			}
		}

		public Task<int> DeleteItemAsync(MPDServerProfile profile)
		{
			lock (_lock)
			{
				return database.DeleteAsync(profile);
			}

		}

		public Task<MPDServerProfile> GetActiveProfile()
		{
			lock (_lock)
			{
				return database.Table<MPDServerProfile>().Where
					(i => i.IsActiveProfile == true).FirstOrDefaultAsync();
			}
		}

		public Task<int> UpdateProfile(MPDServerProfile profile)
		{
			lock (_lock)
			{
				return database.UpdateAsync(profile);
			}
		}

	}
}