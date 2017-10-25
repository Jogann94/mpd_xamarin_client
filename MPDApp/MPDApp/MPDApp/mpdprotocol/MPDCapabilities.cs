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
using System.Collections;
using System.Collections.Generic;

namespace MPDProtocol
{
	public class MPDCapabilities
	{
		private static string TAG;

		private const String MPD_TAG_TYPE_MUSICBRAINZ = "musicbrainz";
		private const String MPD_TAG_TYPE_ALBUMARTIST = "albumartist";
		private const String MPD_TAG_TYPE_DATE = "date";

		private int mMajorVersion;
		private int mMinorVersion;

		private bool mHasIdle;
		private bool mHasRangedCurrentPlaylist;
		private bool mHasSearchAdd;

		private bool mHasMusicBrainzTags;
		private bool mHasListGroup;

		private bool mHasListFiltering;

		private bool mHasCurrentPlaylistRemoveRange;

		private bool mMopidyDetected;

		private bool mTagAlbumArtist;
		private bool mTagDate;

		public MPDCapabilities(String version, List<String> commands, List<String> tags)
		{
			TAG = typeof(MPDCapabilities).FullName;
			String[] versions = version.Split(new string[] { "\\." }, StringSplitOptions.None);
			if (versions.Length == 3)
			{
				mMajorVersion = int.Parse(versions[0]);
				mMinorVersion = int.Parse(versions[1]);
			}

			// Only MPD servers greater version 0.14 have ranged playlist fetching, this allows fallback
			if (mMinorVersion > 14 || mMajorVersion > 0)
			{
				mHasRangedCurrentPlaylist = true;
			}
			else
			{
				mHasRangedCurrentPlaylist = false;
			}

			if (mMinorVersion >= 19 || mMajorVersion > 0)
			{
				mHasListGroup = true;
				mHasListFiltering = true;
			}

			if (mMinorVersion >= 16 || mMajorVersion > 0)
			{
				mHasCurrentPlaylistRemoveRange = true;
			}

			if (null != commands)
			{
				if (commands.Contains(MPDCommands.MPD_COMMAND_START_IDLE))
				{
					mHasIdle = true;
				}
				else
				{
					mHasIdle = false;
				}

				if (commands.Contains(MPDCommands.MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME))
				{
					mHasSearchAdd = true;
				}
				else
				{
					mHasSearchAdd = false;
				}
			}


			if (null != tags)
			{
				foreach (String tag in tags)
				{
					String tagLC = tag.ToLower();
					if (tagLC.Contains(MPD_TAG_TYPE_MUSICBRAINZ))
					{
						mHasMusicBrainzTags = true;
						break;
					}
					else if (tagLC.Equals(MPD_TAG_TYPE_ALBUMARTIST))
					{
						mTagAlbumArtist = true;
					}
					else if (tagLC.Equals(MPD_TAG_TYPE_DATE))
					{
						mTagDate = true;
					}
				}
			}
		}

		public bool hasIdling()
		{
			return mHasIdle;
		}

		public bool hasRangedCurrentPlaylist()
		{
			return mHasRangedCurrentPlaylist;
		}

		public bool hasSearchAdd()
		{
			return mHasSearchAdd;
		}

		public bool hasListGroup()
		{
			return mHasListGroup;
		}

		public bool hasListFiltering()
		{
			return mHasListFiltering;
		}

		public int getMajorVersion()
		{
			return mMajorVersion;
		}

		public int getMinorVersion()
		{
			return mMinorVersion;
		}

		public bool hasMusicBrainzTags()
		{
			return mHasMusicBrainzTags;
		}

		public bool hasCurrentPlaylistRemoveRange()
		{
			return mHasCurrentPlaylistRemoveRange;
		}

		public bool hasTagAlbumArtist()
		{
			return mTagAlbumArtist;
		}

		public bool hasTagDate()
		{
			return mTagDate;
		}

		public String getServerFeatures()
		{
			return "MPD protocol version: " + mMajorVersion + '.' + mMinorVersion + '\n'
							+ "TAGS:" + '\n'
							+ "MUSICBRAINZ: " + mHasMusicBrainzTags + '\n'
							+ "AlbumArtist: " + mTagAlbumArtist + '\n'
							+ "Date: " + mTagDate + '\n'
							+ "IDLE support: " + mHasIdle + '\n'
							+ "Windowed playlist: " + mHasRangedCurrentPlaylist + '\n'
							+ "Fast search add: " + mHasSearchAdd + '\n'
							+ "List grouping: " + mHasListGroup + '\n'
							+ "List filtering: " + mHasListFiltering + '\n'
							+ "Fast ranged currentplaylist delete: " + mHasCurrentPlaylistRemoveRange
							+ (mMopidyDetected ? "\nMopidy detected, consider using the real MPD server (www.musicpd.org)!" : "");
		}

		public void enableMopidyWorkaround()
		{
			//Log.w(TAG, "Enabling workarounds for detected Mopidy server");
			mHasListGroup = false;
			mHasListFiltering = false;
			mMopidyDetected = true;
		}
	}
}