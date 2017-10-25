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

/**
 * This class represents an MPDTrack. This is the same type for tracks and files.
 * This is used for tracks in playlist, album, search results,... and for music files when
 * retrieving an directory listing from the mpd server.
 */
namespace MPDProtocol.MPDDataobjects
{
	public class MPDTrack : MPDFileEntry, MPDGenericItem
	{

		public String TrackTitle { get; set; }
		public String TrackArtist { get; set; }
		public String TrackAlbum { get; set; }
		public String TrackAlbumArtist { get; set; }

		public String Date { get; set; }
		public String ArtistMusicbrainzID { get; set; }
		public String TrackMusicbrainzID { get; set; }

		public String AlbumMusicbrainzID { get; set; }
		public String AlbumArtistMusicbrainzID { get; set; }

		public int LengthInSeconds { get; set; }
		public string DurationString
		{
			get
			{
				int durationHours = LengthInSeconds / 60 / 60;
				int durationMinutes = LengthInSeconds / 60 % 60;
				int durationSeconds = LengthInSeconds % 60;
				if (durationHours != 0)
					return String.Format("{0:00}:{1:00}:{2:00}",
						durationHours, durationMinutes, durationSeconds);
				else
					return String.Format("{0:00}:{1:00}", durationMinutes, durationSeconds);
			}
		}
		/// <summary>
		/// Tracknumber within album of the Song
		/// </summary>
		public int AlbumTrackNumber { get; set; }
		public int AlbumTrackCount { get; set; }

		/// <summary>
		/// Songnumber on Disk(Storage-Medium)
		/// </summary>		
		public int DiscNumber { get; set; }

		/// <summary>
		/// Count of Mediums that store this track
		/// </summary>
		public int AlbumDiscCount { get; set; }

		public int PlaylistPosition { get; set; }
		public int PlaylistSongID { get; set; }

		public bool ImageFetching { get; set; }

		/**
		 * Create empty MPDTrack (track). Fill it with setter methods during
		 * parsing of mpds output.
		 *
		 * @param path The path of the file. This should never change.
		 */
		public MPDTrack(String path) : base(path)
		{
			TrackTitle = "";

			TrackArtist = "";
			TrackAlbum = "";
			TrackAlbumArtist = "";

			Date = "";

			ArtistMusicbrainzID = "";
			TrackMusicbrainzID = "";
			AlbumMusicbrainzID = "";
			AlbumArtistMusicbrainzID = "";

			LengthInSeconds = 0;

			ImageFetching = false;
		}

		/// <summary>
		/// Get String that is used for sectionbased Scrolling
		/// </summary>

		public override String getSectionTitle()
		{
			return TrackTitle.Equals("") ? Path : TrackTitle;
		}

		public int IndexCompare(MPDTrack compFile)
		{
			if (!AlbumMusicbrainzID.Equals(compFile.AlbumMusicbrainzID))
			{
				return AlbumMusicbrainzID.CompareTo(compFile.AlbumMusicbrainzID);
			}
			// Compare disc numbers first
			if (DiscNumber > compFile.DiscNumber)
			{
				return 1;
			}
			else if (DiscNumber == compFile.DiscNumber)
			{
				// Compare track number field
				if (AlbumTrackNumber > compFile.AlbumTrackNumber)
				{
					return 1;
				}
				else if (AlbumTrackNumber == compFile.AlbumTrackNumber)
				{
					return 0;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				return -1;
			}
		}

		public int CompareTo(MPDTrack other)
		{
			if (other == null)
			{
				return -1;
			}

			String title = Path;
			String[] pathSplit = title.Split('/');
			if (pathSplit.Length > 0)
			{
				title = pathSplit[pathSplit.Length - 1];
			}


			String titleAnother = Path;
			String[] pathSplitAnother = title.Split('/');
			if (pathSplit.Length > 0)
			{
				titleAnother = pathSplit[pathSplit.Length - 1];
			}

			return string.Compare(title.ToLower(), titleAnother.ToLower());
		}

	}
}