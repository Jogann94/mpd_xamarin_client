/*
 *  Copyright (C) 2017 Team Gateship-One
 *  (Hendrik Borghorst & Frederik Luetkes)
 *
 *  The AUTHORS.md file contains a detailed contributors list:
 *  <https://github.com/gateship-one/malp/blob/master/AUTHORS.md>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */
using System;

namespace MPDProtocol.MPDDataobjects
{

	public class MPDStatistics
	{
		public int ArtistsCount { get; set; }
		public int AlbumCount { get; set; }
		public int SongCount { get; set; }

		public int ServerUptime { get; set; }
		public int AllSongDuration { get; set; }

		public long LastDBUpdate { get; set; }
		public int PlayDuration { get; set; }

		public MPDStatistics()
		{
			ArtistsCount = 0;
			AlbumCount = 0;
			SongCount = 0;

			ServerUptime = 0;
			AllSongDuration = 0;

			LastDBUpdate = DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond;
			PlayDuration = 0;
		}

	}
}