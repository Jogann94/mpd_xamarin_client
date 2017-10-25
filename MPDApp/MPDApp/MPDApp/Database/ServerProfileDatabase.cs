using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;

namespace MPDApp.ProfileManagement
{
	public class ServerProfileDatabase
	{
		readonly SQLiteAsyncConnection database;

		public ServerProfileDatabase(string dbPath)
		{
			database = new SQLiteAsyncConnection(dbPath);
			database.CreateTableAsync<MPDServerProfile>().Wait();
		}

		public Task<List<MPDServerProfile>> GetProfilesAsync()
		{
			return database.Table<MPDServerProfile>().ToListAsync();
		}

		public Task<MPDServerProfile> GetProfileAsync(int id)
		{
			return database.Table<MPDServerProfile>().Where(i => i.ID == id).FirstOrDefaultAsync();
		}

		public Task<int> SaveProfileAsync(MPDServerProfile profile)
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

		public Task<int> DeleteItemAsync(MPDServerProfile profile)
		{
			return database.DeleteAsync(profile);
		}

		public Task<MPDServerProfile> GetActiveProfile()
		{
			return database.Table<MPDServerProfile>().Where
				(i => i.IsActiveProfile == true).FirstOrDefaultAsync();
		}

		public Task<int> UpdateProfile(MPDServerProfile profile)
		{
			return database.UpdateAsync(profile);
		}

	}
}