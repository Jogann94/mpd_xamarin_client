using System;
using System.Collections.Generic;
using ApiAiSDK.Model;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;

namespace MPDApp.Services
{
    class AIFullfillment
    {
		public AIResponse response;

		public event Action<string> OnActionFullfilled;

		private MPDConnection con;

		public AIFullfillment(AIResponse toFullfill)
		{
			response = toFullfill;
		}

		public void FullfillAIResponse()
		{
			if (!SetAndTestMPD())
			{
				OnActionFullfilled?.Invoke("There is no connected MPD Server");
				return;
			}

			if (response.Result.ActionIncomplete)
			{
				OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
				return;
			}

			string action = response.Result.Action;

			if (action == "music.play")
			{
				FullfillPlayAction();
			}
			else if (action == "music.playlist")
			{
				FullfillPlaylistAction();
			}
			else if (action == "music.artist")
			{
				FullfillArtistAction();
			}
			else if (action == "music.pause")
			{
				FullfillPauseAction();
			}
			else if (action == "music.previous")
			{
				FullfillPreviousAction();
			}
			else if (action == "music.shuffle")
			{
				FullfillShuffleAction();
			}
			else if (action == "music.skip")
			{
				FullfillSkipAction();
			}
			else if (action == "music.resume")
			{
				FullfillSkipAction();
			}
			else if (action == "music.stop")
			{
				FullfillStopAction();
			}

		}

		private void FullfillPlayAction()
		{
			string songTitle = response.Result.Parameters["Song"] as String;
			var fileList = con.GetSearchedFiles(songTitle, MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_FILE);
			if (fileList.Count == 0)
			{
				OnActionFullfilled?.Invoke(
					String.Format(
						"Sorry there is no song with the title {0} in your Files", songTitle));
			}
			else
			{
				con.ClearPlaylist();
				con.AddSong(fileList[0].Path);
				con.PlaySongIndex(0);
				OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
			}
		}

		private void FullfillPlaylistAction()
		{
			string playlistName = response.Result.Parameters["Playlist"] as string;
			MPDFileEntry playlist = con.GetPlaylists().Find(x => x.Path.Contains(playlistName));

			if (playlist != null)
			{
				con.LoadPlaylist(playlist.Path);
				con.PlaySongIndex(0);
				OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
			}
			else
			{
				OnActionFullfilled?.Invoke("Sorry there is no playlist named " + playlistName);
			}
		}

		private void FullfillArtistAction()
		{
			string artistName = response.Result.Parameters["Artist"] as string;
			List<MPDFileEntry> fileList = con.GetSearchedFiles(
				artistName, MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ARTIST);

			if (fileList.Count != 0)
			{
				con.ClearPlaylist();
				foreach (var file in fileList)
				{
					con.AddSong(file.Path);
				}
				Random r = new Random();
				con.PlaySongIndex(r.Next(0, fileList.Count));
				OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
			}
			else
			{
				OnActionFullfilled?.Invoke(
					String.Format("There is no artist called {0} in your list", artistName));
			}
		}

		private void FullfillPauseAction()
		{
			con.Pause(true);
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private void FullfillPreviousAction()
		{
			con.PreviousSong();
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private void FullfillShuffleAction()
		{
			con.ShufflePlaylist();
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private void FullfillSkipAction()
		{
			con.NextSong();
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private void FullfillResumeAction()
		{
			con.Pause(false);
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private void FullfillStopAction()
		{
			con.StopPlayback();
			OnActionFullfilled?.Invoke(response.Result.Fulfillment.Speech);
		}

		private bool SetAndTestMPD()
		{
			con = MPDConnection.GetInstance();

			if (!con.IsConnected())
			{
				return false;
			}

			return true;
		}

	}
}

