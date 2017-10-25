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

namespace MPDProtocol
{
	public class MPDCommands
	{

		public const String MPD_COMMAND_CLOSE = "close";

		public const String MPD_COMMAND_PASSWORD = "password ";

		private static String CreateAlbumGroupString(MPDCapabilities caps)
		{
			String groups = "";
			if (caps.hasTagAlbumArtist())
			{
				groups += " group albumartist";
			}
			if (caps.hasMusicBrainzTags())
			{
				groups += " group musicbrainz_albumid";
			}
			if (caps.hasTagDate())
			{
				groups += " group date";
			}
			return groups;
		}

		/* Database request commands */
		public static String MPD_COMMAND_REQUEST_ALBUMS(MPDCapabilities caps)
		{
			if (caps.hasListGroup())
			{
				return "list album" + CreateAlbumGroupString(caps);
			}
			else
			{
				return "list album";
			}
		}

		public static String MPD_COMMAND_REQUEST_ARTIST_ALBUMS(String artistName, MPDCapabilities caps)
		{
			if (caps.hasListGroup())
			{
				return "list album artist \"" + artistName.Replace("\"", "\\\\\"") + "\"" + CreateAlbumGroupString(caps);
			}
			else
			{
				return "list album \"" + artistName.Replace("\"", "\\\\\"") + "\"";
			}
		}

		public static String MPD_COMMAND_REQUEST_ALBUMS_FOR_PATH(String path, MPDCapabilities caps)
		{
			if (caps.hasListGroup())
			{
				return "list album base \"" + path + "\"" + CreateAlbumGroupString(caps);
			}
			else
			{
				// FIXME check if correct. Possible fallback for group missing -> base command also missing.
				return "list album";
			}
		}

		public static String MPD_COMMAND_REQUEST_ALBUMARTIST_ALBUMS(String artistName, MPDCapabilities caps)
		{
			return "list album AlbumArtist \"" + artistName.Replace("\"", "\\\\\"") + "\"" + CreateAlbumGroupString(caps);
		}

		public static String MPD_COMMAND_REQUEST_ALBUM_TRACKS(String albumName)
		{
			return "find album \"" + albumName.Replace("\"", "\\\\\"") + "\"";
		}

		public static String MPD_COMMAND_REQUEST_ARTISTS(bool groupMBID)
		{
			if (!groupMBID)
			{
				return "list artist";
			}
			else
			{
				return "list artist group MUSICBRAINZ_ARTISTID";
			}
		}

		public static String MPD_COMMAND_REQUEST_ALBUMARTISTS(bool groupMBID)
		{
			if (!groupMBID)
			{
				return "list albumartist";
			}
			else
			{
				return "list albumartist group MUSICBRAINZ_ARTISTID";
			}
		}

		public const String MPD_COMMAND_REQUEST_ALL_FILES = "listallinfo";

		/* Control commands */
		public static String MPD_COMMAND_PAUSE(bool pause)
		{
			return "pause " + (pause ? "1" : "0");
		}

		public const String MPD_COMMAND_NEXT = "next";
		public const String MPD_COMMAND_PREVIOUS = "previous";
		public const String MPD_COMMAND_STOP = "stop";

		public const String MPD_COMMAND_GET_CURRENT_STATUS = "status";
		public const String MPD_COMMAND_GET_STATISTICS = "stats";

		public const String MPD_COMMAND_GET_SAVED_PLAYLISTS = "listplaylists";

		public const String MPD_COMMAND_GET_CURRENT_PLAYLIST = "playlistinfo";

		public static String MPD_COMMAND_GET_CURRENT_PLAYLIST_WINDOW(int start, int end)
		{
			return "playlistinfo " + start + ':' + end;
		}

		public static String MPD_COMMAND_GET_SAVED_PLAYLIST(String playlistName)
		{
			return "listplaylistinfo \"" + playlistName + "\"";
		}

		public static String MPD_COMMAND_GET_FILES_INFO(String path)
		{
			return "lsinfo \"" + path + "\"";
		}

		public static String MPD_COMMAND_SAVE_PLAYLIST(String playlistName)
		{
			return "save \"" + playlistName + "\"";
		}

		public static String MPD_COMMAND_REMOVE_PLAYLIST(String playlistName)
		{
			return "rm \"" + playlistName + "\"";
		}

		public static String MPD_COMMAND_LOAD_PLAYLIST(String playlistName)
		{
			return "load \"" + playlistName + "\"";
		}

		public static String MPD_COMMAND_ADD_TRACK_TO_PLAYLIST(String playlistName, String url)
		{
			return "playlistadd \"" + playlistName + "\" \"" + url + '\"';
		}

		public static String MPD_COMMAND_REMOVE_TRACK_FROM_PLAYLIST(String playlistName, int position)
		{
			return "playlistdelete \"" + playlistName + "\" " + position;
		}

		public const String MPD_COMMAND_GET_CURRENT_SONG = "currentsong";

		public const String MPD_COMMAND_START_IDLE = "idle";
		public const String MPD_COMMAND_STOP_IDLE = "noidle";

		public const String MPD_START_COMMAND_LIST = "command_list_begin";
		public const String MPD_END_COMMAND_LIST = "command_list_end";

		public static String MPD_COMMAND_ADD_FILE(String url)
		{
			return "add \"" + url + "\"";
		}

		public static String MPD_COMMAND_ADD_FILE_AT_INDEX(String url, int index)
		{
			return "addid \"" + url + "\"  " + index;
		}

		public static String MPD_COMMAND_REMOVE_SONG_FROM_CURRENT_PLAYLIST(int index)
		{
			return "delete " + index;
		}

		public static String MPD_COMMAND_REMOVE_RANGE_FROM_CURRENT_PLAYLIST(int start, int end)
		{
			return "delete " + start + ':' + end;
		}

		public static String MPD_COMMAND_MOVE_SONG_FROM_INDEX_TO_INDEX(int from, int to)
		{
			return "move " + from + ' ' + to;
		}

		public const String MPD_COMMAND_CLEAR_PLAYLIST = "clear";

		public static String MPD_COMMAND_SET_RANDOM(bool random)
		{
			return "random " + (random ? "1" : "0");
		}

		public static String MPD_COMMAND_SET_REPEAT(bool repeat)
		{
			return "repeat " + (repeat ? "1" : "0");
		}

		public static String MPD_COMMAND_SET_SINGLE(bool single)
		{
			return "single " + (single ? "1" : "0");
		}

		public static String MPD_COMMAND_SET_CONSUME(bool consume)
		{
			return "consume " + (consume ? "1" : "0");
		}


		public static String MPD_COMMAND_PLAY_SONG_INDEX(int index)
		{
			return "play " + index;
		}

		public static String MPD_COMMAND_SEEK_SECONDS(int index, int seconds)
		{
			return "seek " + index + ' ' + seconds;
		}

		public static String MPD_COMMAND_SET_VOLUME(int volume)
		{
			if (volume > 100)
			{
				volume = 100;
			}
			else if (volume < 0)
			{
				volume = 0;
			}
			return "setvol " + volume;
		}

		public const String MPD_COMMAND_GET_OUTPUTS = "outputs";

		public static String MPD_COMMAND_TOGGLE_OUTPUT(int id)
		{
			return "toggleoutput " + id;
		}

		public static String MPD_COMMAND_UPDATE_DATABASE(String path)
		{
			if (null != path && !path.Equals(""))
			{
				return "update \"" + path + "\"";
			}
			else
			{
				return "update";
			}
		}

		public enum MPD_SEARCH_TYPE
		{
			MPD_SEARCH_TRACK,
			MPD_SEARCH_ALBUM,
			MPD_SEARCH_ARTIST,
			MPD_SEARCH_FILE,
			MPD_SEARCH_ANY,
		}

		public static String MPD_COMMAND_SEARCH_FILES(String searchTerm, MPD_SEARCH_TYPE type)
		{
			switch (type)
			{
				case MPD_SEARCH_TYPE.MPD_SEARCH_TRACK:
					return "search title \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ALBUM:
					return "search album \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ARTIST:
					return "search artist \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_FILE:
					return "search file \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ANY:
					return "search any \"" + searchTerm + '\"';
			}
			return "ping";
		}

		public const String MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME = "searchadd";

		public static String MPD_COMMAND_ADD_SEARCH_FILES(String searchTerm, MPD_SEARCH_TYPE type)
		{
			switch (type)
			{
				case MPD_SEARCH_TYPE.MPD_SEARCH_TRACK:
					return MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME + " title \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ALBUM:
					return MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME + " album \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ARTIST:
					return MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME + " artist \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_FILE:
					return MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME + " file \"" + searchTerm + '\"';
				case MPD_SEARCH_TYPE.MPD_SEARCH_ANY:
					return MPD_COMMAND_ADD_SEARCH_FILES_CMD_NAME + " any \"" + searchTerm + '\"';
			}
			return "ping";
		}

		public const String MPD_COMMAND_GET_COMMANDS = "commands";

		public const String MPD_COMMAND_GET_TAGS = "tagtypes";

		/**
     * Searches the song of an given URL in the current playlist. MPD will respond by
     * returning a track object if found or nothing else.
     * @param url URL to search for.
     * @return command string for MPD
     */
		public static String MPD_COMMAND_PLAYLIST_FIND_URI(String url)
		{
			return "playlistfind file \"" + url + "\"";
		}

		public const String MPD_COMMAND_SHUFFLE_PLAYLIST = "shuffle";
	}
}