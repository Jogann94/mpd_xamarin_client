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


	public class MPDCurrentStatus
	{

		public enum MPD_PLAYBACK_STATE
		{
			MPD_PLAYING,
			MPD_PAUSING,
			MPD_STOPPED
		};

		private int _volume;
		public int Volume
		{
			get { return _volume; }
			set { if (value <= 100 || value >= 0) _volume = value; }
		}

		private int _repeat;
		public int Repeat
		{
			get { return _repeat; }
			set { if (value == 0 || value == 1) _repeat = value; }
		}

		private int _random;
		public int Random
		{
			get { return _random; }
			set { if (value == 0 || value == 1) _random = value; }
		}

		private int _singlePlayback;
		public int SinglePlayback
		{
			get { return _singlePlayback; }
			set { if (value == 0 || value == 1) _singlePlayback = value; }
		}

		private int _consume;
		public int Consume
		{
			get { return _consume; }
			set { if (value == 0 || value == 1) _consume = value; }
		}

		public int playlistVersion;
		public int playlistLength;
		public int currentSongIndex;
		public int nextSongIndex;
		public int samplerate;

		/// <summary>
		/// Sample-Resolution in Bits
		/// </summary>  
		public string bitDepth;
		public int channelCount;

		/// <summary>
		/// Codec Bitrate
		/// </summary>
		public int bitrate;

		/// <summary>
		/// Time elapsed on current Song
		/// </summary>
		public int elapsedTime;
		public int currentTrackLength;

		/// <summary>
		/// If an updating job of the database is running, the id gets saved here.
		/// Also the update commands sends back the id of the corresponding update job.
		/// </summary>
		public int updateDBJob;
		public MPD_PLAYBACK_STATE playbackState;

		/*
					protected MPDCurrentStatus(Parcel in) {
							// Create this object from parcel
							pVolume = in.readInt();
							pRepeat = in.readInt();
							pRandom = in.readInt();
					 pSinglePlayback = in.readInt();
							pConsume = in.readInt();
							pPlaylistVersion = in.readInt();
							pPlaylistLength = in.readInt();
							pCurrentSongIndex = in.readInt();
							pNextSongIndex = in.readInt();
							pSamplerate = in.readInt();
							pBitDepth = in.readString();
							pChannelCount = in.readInt();
							pBitrate = in.readInt();
							pElapsedTime = in.readInt();
							pTrackLength = in.readInt();
							pUpdateDBJob = in.readInt();
							pPlaybackState = MPD_PLAYBACK_STATE.values()[in.readInt()];
					}
		*/
		public MPDCurrentStatus()
		{
			updateDBJob = -1;
			playbackState = MPD_PLAYBACK_STATE.MPD_STOPPED;
		}

		public MPDCurrentStatus(MPDCurrentStatus status)
		{
			Volume = status.Volume;
			Repeat = status.Repeat;
			Random = status.Random;
			SinglePlayback = status.SinglePlayback;
			Consume = status.Consume;
			playlistVersion = status.playlistVersion;
			playlistLength = status.playlistLength;
			currentSongIndex = status.currentSongIndex;
			nextSongIndex = status.nextSongIndex;
			samplerate = status.samplerate;
			bitDepth = status.bitDepth;
			channelCount = status.channelCount;
			bitrate = status.bitrate;
			elapsedTime = status.elapsedTime;
			currentTrackLength = status.currentTrackLength;
			updateDBJob = status.updateDBJob;
			playbackState = status.playbackState;
		}

		/*
					@Override
					public void writeToParcel(Parcel dest, int flags) {

							dest.writeInt(pVolume);
							dest.writeInt(pRepeat);
							dest.writeInt(pRandom);
							dest.writeInt(pSinglePlayback);
							dest.writeInt(pConsume);
							dest.writeInt(pPlaylistVersion);
							dest.writeInt(pPlaylistLength);
							dest.writeInt(pCurrentSongIndex);
							dest.writeInt(pNextSongIndex);
							dest.writeInt(pSamplerate);
							dest.writeString(pBitDepth);
							dest.writeInt(pChannelCount);
							dest.writeInt(pBitrate);
							dest.writeInt(pElapsedTime);
							dest.writeInt(pTrackLength);
							dest.writeInt(pUpdateDBJob);

							dest.writeInt(pPlaybackState.ordinal());
					}
		*/
		/*      @Override
					public int describeContents() {
							return 0;
					}
		*/
		public string PrintStatus()
		{
			string retString = "";

			retString += "Volume: " + Volume + "\n";
			retString += "Repeat: " + Repeat + "\n";
			retString += "Random: " + Random + "\n";
			retString += "Single: " + SinglePlayback + "\n";
			retString += "Consume: " + Consume + "\n";
			retString += "Playlist version: " + playlistVersion + "\n";
			retString += "Playlist length: " + playlistLength + "\n";
			retString += "Current song index: " + currentSongIndex + "\n";
			retString += "Next song index: " + nextSongIndex + "\n";
			retString += "Samplerate: " + samplerate + "\n";
			retString += "Bitdepth: " + bitDepth + "\n";
			retString += "Channel count: " + channelCount + "\n";
			retString += "Bitrate: " + bitrate + "\n";
			retString += "Elapsed time: " + elapsedTime + "\n";
			retString += "Track length: " + currentTrackLength + "\n";
			retString += "UpdateDB job id: " + updateDBJob + "\n";
			retString += "Playback state: " + playbackState + "\n";

			return retString;
		}
	}
}