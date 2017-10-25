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
	public class MPDResponses
	{
		public const String MPD_RESPONSE_ALBUM_NAME = "Album: ";
		public const String MPD_RESPONSE_ALBUM_MBID = "MUSICBRAINZ_ALBUMID: ";

		public const String MPD_RESPONSE_ARTIST_NAME = "Artist: ";
		public const String MPD_RESPONSE_ALBUMARTIST_NAME = "AlbumArtist: ";
		public const String MPD_RESPONSE_FILE = "file: ";
		public const String MPD_RESPONSE_DIRECTORY = "directory: ";
		public const String MPD_RESPONSE_TRACK_TITLE = "Title: ";
		public const String MPD_RESPONSE_ALBUM_ARTIST_NAME = "AlbumArtist: ";
		public const String MPD_RESPONSE_TRACK_TIME = "Time: ";
		public const String MPD_RESPONSE_DATE = "Date: ";

		public const String MPD_RESPONSE_TRACK_MBID = "MUSICBRAINZ_TRACKID: ";
		public const String MPD_RESPONSE_ALBUM_ARTIST_MBID = "MUSICBRAINZ_ALBUMARTISTID: ";
		public const String MPD_RESPONSE_ARTIST_MBID = "MUSICBRAINZ_ARTISTID: ";
		public const String MPD_RESPONSE_TRACK_NUMBER = "Track: ";
		public const String MPD_RESPONSE_DISC_NUMBER = "Disc: ";
		public const String MPD_RESPONSE_SONG_POS = "Pos: ";
		public const String MPD_RESPONSE_SONG_ID = "Id: ";


		public const String MPD_RESPONSE_PLAYLIST = "playlist: ";
		public const String MPD_RESPONSE_LAST_MODIFIED = "Last-Modified: ";

		/* MPD currentstatus responses */
		public const String MPD_RESPONSE_VOLUME = "volume: ";
		public const String MPD_RESPONSE_REPEAT = "repeat: ";
		public const String MPD_RESPONSE_RANDOM = "random: ";
		public const String MPD_RESPONSE_SINGLE = "single: ";
		public const String MPD_RESPONSE_CONSUME = "consume: ";
		public const String MPD_RESPONSE_PLAYLIST_VERSION = "playlist: ";
		public const String MPD_RESPONSE_PLAYLIST_LENGTH = "playlistlength: ";
		public const String MPD_RESPONSE_CURRENT_SONG_INDEX = "song: ";
		public const String MPD_RESPONSE_CURRENT_SONG_ID = "songid: ";
		public const String MPD_RESPONSE_NEXT_SONG_INDEX = "nextsong: ";
		public const String MPD_RESPONSE_NEXT_SONG_ID = "nextsongid: ";
		public const String MPD_RESPONSE_TIME_INFORMATION_OLD = "time: ";
		public const String MPD_RESPONSE_ELAPSED_TIME = "elapsed: ";
		public const String MPD_RESPONSE_DURATION = "duration: ";
		public const String MPD_RESPONSE_BITRATE = "bitrate: ";
		public const String MPD_RESPONSE_AUDIO_INFORMATION = "audio: ";
		public const String MPD_RESPONSE_UPDATING_DB = "updating_db: ";
		public const String MPD_RESPONSE_ERROR = "error: ";

		public const String MPD_RESPONSE_PLAYBACK_STATE = "state: ";
		public const String MPD_PLAYBACK_STATE_RESPONSE_PLAY = "play";
		public const String MPD_PLAYBACK_STATE_RESPONSE_PAUSE = "pause";
		public const String MPD_PLAYBACK_STATE_RESPONSE_STOP = "stop";

		public const String MPD_OUTPUT_ID = "outputid: ";
		public const String MPD_OUTPUT_NAME = "outputname: ";
		public const String MPD_OUTPUT_ACTIVE = "outputenabled: ";

		public const String MPD_STATS_UPTIME = "uptime: ";
		public const String MPD_STATS_PLAYTIME = "playtime: ";
		public const String MPD_STATS_ARTISTS = "artists: ";
		public const String MPD_STATS_ALBUMS = "albums: ";
		public const String MPD_STATS_SONGS = "songs: ";
		public const String MPD_STATS_DB_PLAYTIME = "db_playtime: ";
		public const String MPD_STATS_DB_LAST_UPDATE = "db_update: ";

		public const String MPD_COMMAND = "command: ";
		public const String MPD_TAGTYPE = "tagtype: ";

		//M.Schleinkofer 26.05.2017
		public const String MPD_RESPONSE_STICKER = "sticker: ";
		//End M.Schleinkofer

		public const String MPD_PARSE_ARGS_LIST_ERROR = "not able to parse args";
	}
}