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

/**
 * This is the main MPDConnection class. It will connect to an MPD server via an java TCP socket.
 * If no action, query, or other command to the server is send, this connection will immediately
 * start to idle. This means that the connection is waiting for a response from the mpd server.
 * <p/>
 * For this this class spawns a new thread which is then blocked by the waiting read operation
 * on the reader of the socket.
 * <p/>
 * If a new command is requested by the handler thread the stopIdling function is called, which
 * will send the "noidle" command to the server and requests to deidle the connection. Only then the
 * server is ready again to receive commands. If this is not done properly the server will just
 * terminate the connection.
 * <p/>
 * This mpd connection needs to be run in a different thread than the UI otherwise the UI will block
 * (or android will just throw an exception).
 * <p/>
 * For more information check the protocol definition of the mpd server or contact me via mail.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.IO;
using MPDProtocol.MPDDataobjects;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MPDProtocol
{

	public class MPDConnection
	{
		private const string TAG = "MPDConnection";

		private String mID;

		private const bool DEBUG_ENABLED = false;

		private const int SOCKET_TIMEOUT = 5 * 1000;

		private static object _lock = new object();
		/**
		 * Time to wait for response from server. If server is not answering this prevents a livelock
		 * after 5 seconds. (time in ns)
		 */
		private const long RESPONSE_TIMEOUT = 5L * 1000L;

		private const int IDLE_WAIT_TIME = 500;

		/* Internal server parameters used for initiating the connection */
		public string hostname;
		public string password;
		public int port;

		private TcpClient pSocket;

		/* True only if server is ready to receive commands */
		private bool pMPDConnectionReady = false;

		/* True if server connection is in idleing state. Needs to be deidled before sending command */
		private bool pMPDConnectionIdle = false;

		/* MPD server properties */
		private MPDCapabilities mServerCapabilities;

		/**
		 * Only get the server capabilities if server parameters changed
		 */
		private bool mCapabilitiesChanged;

		/**
		 * One listener for the state of the connection (connected, disconnected)
		 */
		public delegate void ChangedEventHandler();
		/// <summary>
		/// Event for the Idle State of the Connection, When the server is idled (from other client)
		/// it will Invoke this event
		/// </summary>
		public event Action OnIdle;
		/// <summary>
		/// Event for the Idle State of the Connection, When the server is deidled (from other client)
		/// it will Invoke this event
		/// </summary>
		public event Action OnDeidle;

		/// <summary>
		/// Event that is Invoked if Server connection changes to Connected
		/// </summary>
		public event Action OnConnect;
		/// <summary>
		/// Event that is Invoked if Server connection changes to Disconnected
		/// </summary>
		public event Action OnDisconnect;

		/**
		 * Thread that will spawn when the server is not requested at the moment. Will start an
		 * blocking read operation on the socket reader.
		 */
		private Task idleTask = null;

		/**
		 * Timeout to start the actual idling thread. It will start after IDLE_WAIT_TIME milliseconds
		 * passed. To prevent interfering with possible handler calls at the same time
		 * all the methods that could be called from outside are synchronized to this MPDConnection class.
		 * This means that you have to be careful when calling these functions to prevent deadlocks.
		 */
		private CancellationTokenSource idleWaitCancelTokenSource = new CancellationTokenSource();
		private Task mIdleWait;

		/**
		 * Semaphore lock used by the deidling process. Necessary to guarantee the correct order of
		 * deidling write / read operations.
		 */
		private object idleWaitLock = new object();

		private static MPDConnection mInstance;

		public static MPDConnection GetInstance()
		{
			lock (_lock)
			{
				if (null == mInstance)
				{
					mInstance = new MPDConnection("global");
				}
				return mInstance;
			}
		}

		/**
		 * Creates disconnected MPDConnection with following parameters
		 */
		private MPDConnection(String id)
		{
			pSocket = null;
			mID = id;
			mServerCapabilities = new MPDCapabilities("", null, null);

		}

		/**
		 * Private function to handle read error. Try to disconnect and remove old sockets.
		 * Clear up connection state variables.
		 */
		private void HandleSocketError()
		{
			lock (_lock)
			{
				Debug.WriteLine("Read error exception. Disconnecting and cleaning up");

				try
				{

					/* Clear TCP-Socket up */
					if (null != pSocket)
					{
						pSocket.Dispose();
					}
					pSocket = null;
				}
				catch (Exception)
				{
					Debug.WriteLine("Error during read error handling");
				}

				/* Clear up connection state variables */
				pMPDConnectionIdle = false;
				pMPDConnectionReady = false;


				// Notify listener
				OnDisconnect?.Invoke();
			}
		}

		/**
		 * Set the parameters to connect to. Should be called before the connection attempt
		 * otherwise the connection object does not know where to put it.
		 *
		 * @param hostname Hostname to connect to. Can also be an ip.
		 * @param password Password for the server to authenticate with. Can be left empty.
		 * @param port     TCP port to connect to.
		 */
		public void SetServerParameters(String hostname, String password, int port)
		{
			lock (_lock)
			{
				this.hostname = hostname;
				if (!password.Equals(""))
				{
					this.password = password;
				}
				this.port = port;
				mCapabilitiesChanged = true;
			}
		}

		/**
		 * This is the actual start of the connection. It tries to resolve the hostname
		 * and initiates the connection to the address and the configured tcp-port.
		 */
		public void ConnectToServer()
		{
			lock (_lock)
			{
				/* If a socket is already open, close it and destroy it. */
				if ((null != pSocket) /*&& (pSocket.isConnected())*/)
				{
					DisconnectFromServer();
				}

				if ((null == hostname) || hostname.Equals(""))
				{
					return;
				}
				pMPDConnectionIdle = false;
				pMPDConnectionReady = false;
				/* Create a new socket used for the TCP-connection. */
				pSocket = new TcpClient();
				try
				{
					var connect = pSocket.ConnectAsync(hostname, port);
					connect.Wait();
				}
				catch (Exception)
				{
					HandleSocketError();
					return;
				}

				try
				{
					WaitForResponse();
				}
				catch (IOException)
				{
					HandleSocketError();
					return;
				}

				/* If connected try to get MPDs version */
				String readString = null;

				String versionString = "";
				try
				{
					using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
					{

						while (reader.Peek() > 0)
						{
							readString = ReadLine(reader);
							/* Look out for the greeting message */
							if (readString.StartsWith("OK MPD "))
							{
								versionString = readString.Substring(7);

								String[] versions = versionString.Split(new string[] { "\\." }, StringSplitOptions.None);
								if (versions.Length == 3)
								{
									// Check if server version changed and if, reread server capabilities later.
									if (int.Parse(versions[0]) != mServerCapabilities.getMajorVersion() ||
													(int.Parse(versions[0]) == mServerCapabilities.getMajorVersion() && int.Parse(versions[1]) != mServerCapabilities.getMinorVersion()))
									{
										mCapabilitiesChanged = true;
									}
								}
							}
						}
					}
				}
				catch (IOException)
				{
					HandleSocketError();
					return;
				}

				pMPDConnectionReady = true;

				if (password != null && !password.Equals(""))
				{
					/* Authenticate with server because password is set. */
					bool authenticated = AuthenticateMPDServer();
				}


				if (mCapabilitiesChanged)
				{
					// Get available commands
					SendMPDCommand(MPDCommands.MPD_COMMAND_GET_COMMANDS);

					List<String> commands = null;
					try
					{
						commands = ParseMPDCommands();
					}
					catch (IOException)
					{
						HandleSocketError();
						return;
					}

					// Get list of supported tags
					SendMPDCommand(MPDCommands.MPD_COMMAND_GET_TAGS);
					List<String> tags = null;
					try
					{
						tags =ParseMPDTagTypes();
					}
					catch (IOException)
					{
						HandleSocketError();
						return;
					}

					mServerCapabilities = new MPDCapabilities(versionString, commands, tags);
					mCapabilitiesChanged = false;
				}

				// Start the initial idling procedure.
				StartIdleWait();

				// Notify listener
				OnConnect?.Invoke();
			}
		}
		/**
		 * If the password for the MPDConnection is set then the client should
		 * try to authenticate with the server
		 */
		private bool AuthenticateMPDServer()
		{
			/* Check if connection really is good to go. */
			if (!pMPDConnectionReady || pMPDConnectionIdle)
			{
				return false;
			}

			SendMPDCommand(MPDCommands.MPD_COMMAND_PASSWORD + password);

			/* Check if the result was positive or negative */

			String readString = null;

			bool success = false;
			try
			{

				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{
					while (reader.Peek() >= 0)
					{
						readString = ReadLine(reader);
						if (readString.StartsWith("OK"))
						{
							success = true;
						}
						else if (readString.StartsWith("ACK"))
						{
							success = false;
							Debug.WriteLine("Could not successfully authenticate with mpd server");
						}
					}
				}
				//reader.Dispose();
			}
			catch (IOException)
			{
				HandleSocketError();
			}


			return success;
		}

		/**
		 * Requests to disconnect from server. This will close the conection and cleanup the socket.
		 * After this call it should be safe to reconnect to another server. If this connection is
		 * currently in idle state, then it will be deidled before.
		 */
		public void DisconnectFromServer()
		{
			lock (_lock)
			{
				// Stop possible timers waiting for the timeout to go idle
				StopIdleWait();

				// Check if the connection is currently idling, if then deidle.
				if (pMPDConnectionIdle)
				{
					StopIdleing();
				}

				// Close connection gracefully
				SendMPDRAWCommand(MPDCommands.MPD_COMMAND_CLOSE);

				/* Cleanup reader/writer */
				try
				{
					/* Clear TCP-Socket up */
					if (null != pSocket)
					{
						pSocket.Dispose();
						pSocket = null;
					}
				}
				catch (IOException e)
				{
					Debug.WriteLine("Error during disconnecting:" + e.Message);
				}

				/* Clear up connection state variables */
				pMPDConnectionIdle = false;
				pMPDConnectionReady = false;

				// Notify listener
				OnDisconnect?.Invoke();
			}
		}

		/**
		 * Access to the currently server capabilities
		 *
		 * @return
		 */
		public MPDCapabilities GetServerCapabilities()
		{
			if (IsConnected())
			{
				return mServerCapabilities;
			}
			return null;
		}

		/**
		 * This functions sends the command to the MPD server.
		 * If the server is currently idling then it will deidle it first.
		 *
		 * @param command
		 */
		private void SendMPDCommand(String command)
		{
			Debug.WriteLine("Send command: " + command);
			// Stop possible idling timeout tasks.
			StopIdleWait();


			/* Check if the server is connected. */
			if (pMPDConnectionReady)
			{

				/*
				 * Check if server is in idling mode, this needs unidling first,
				 * otherwise the server will disconnect the client.
				 */
				if (pMPDConnectionIdle)
				{
					StopIdleing();
				}

				// During deidle a disconnect could happen, check again if connection is ready
				if (!pMPDConnectionReady)
				{
					return;
				}

				/*
				 * Send the command to the server
				 *
				 */
				WriteLine(command);

				Debug.WriteLine("Sent command: " + command);

				// This waits until the server sends a response (OK,ACK(failure) or the requested data)
				try
				{
					//waitForResponse();
				}
				catch (IOException)
				{
					HandleSocketError();
				}
				Debug.WriteLine("Sent command, got response");
			}
			else
			{
				Debug.WriteLine("Connection not ready, command not sent");
			}
		}

		/**
		 * This functions sends the command to the MPD server.
		 * This function is used between start command list and the end. It has no check if the
		 * connection is currently idle.
		 * Also it will not wait for a response because this would only deadlock, because the mpd server
		 * waits until the end_command is received.
		 *
		 * @param command
		 */
		private void SendMPDRAWCommand(String command)
		{
			/* Check if the server is connected. */
			if (pMPDConnectionReady)
			{
				/*
				 * Send the command to the server
				 * FIXME Should be validated in the future.
				 */
				WriteLine(command);

			}
		}

		/**
		 * This will start a command list to the server. It can be used to speed up multiple requests
		 * like adding songs to the current playlist. Make sure that the idle timeout is stopped
		 * before starting a command list.
		 */
		private void StartCommandList()
		{
			/* Check if the server is connected. */
			if (pMPDConnectionReady)
			{
				/* Check if server is in idling mode, this needs unidling first,
				otherwise the server will disconnect the client.
				 */
				if (pMPDConnectionIdle)
				{
					StopIdleing();
				}

				/*
				 * Send the command to the server
				 * FIXME Should be validated in the future.
				 */
				WriteLine(MPDCommands.MPD_START_COMMAND_LIST);


			}
		}

		/**
		 * This command will end the command list. After this call it is important to call
		 * checkResponse to clear the possible response in the read buffer. There should be at
		 * least one "OK" or "ACK" from the mpd server.
		 */
		private void EndCommandList()
		{
			/* Check if the server is connected. */
			if (pMPDConnectionReady)
			{
				/*
				 * Send the command to the server
				 * FIXME Should be validated in the future.
				 */
				WriteLine(MPDCommands.MPD_END_COMMAND_LIST);
				try
				{
					//waitForResponse();
				}
				catch (IOException)
				{
					HandleSocketError();
				}
			}
		}


		/**
		 * This method needs to be called before a new MPD command is sent to
		 * the server to correctly unidle. Otherwise the mpd server will disconnect
		 * the disobeying client.
		 */
		private void StopIdleing()
		{
			/* Check if server really is in idling mode */
			if (!pMPDConnectionIdle || !pMPDConnectionReady)
			{
				return;
			}

			try
			{
				//pSocket.setSoTimeout(SOCKET_TIMEOUT);
			}
			catch (Exception)
			{
				
				HandleSocketError();
			}

			/* Send the "noidle" command to the server to initiate noidle */
			WriteLine(MPDCommands.MPD_COMMAND_STOP_IDLE);

			Debug.WriteLine("Sent deidle request");

			Debug.WriteLine("Deidle lock acquired, server usage allowed again");

			//mIdleWaitLock.release();
		}

		/**
		 * Initiates the idling procedure. A separate thread is started to wait (blocked)
		 * for a deidle from the MPD host. Otherwise it is impossible to get notified on changes
		 * from other mpd clients (eg. volume change)
		 */
		private void StartIdleing()
		{
			lock (_lock)
			{
				/* Check if server really is in idling mode */
				if (!pMPDConnectionReady || pMPDConnectionIdle)
				{
					return;
				}
				Debug.WriteLine("Start idle mode");

				// This will send the idle command to the server. From there on we need to deidle before
				// sending new requests.
				WriteLine(MPDCommands.MPD_COMMAND_START_IDLE);


				// Technically we are in idle mode now, set bool
				pMPDConnectionIdle = true;

				// Get the lock to prevent the handler thread from (stopIdling) to interfere with deidling sequence.

				/* TODO THREADING SHIT */

				idleTask = new Task(() => { IdleTask(); });
				idleTask.Start();

				OnIdle?.Invoke();
			}
		}

		/**
		 * Function only actively waits for reader to get ready for
		 * the response.
		 */
		private void WaitForResponse()
		{
			Debug.WriteLine("Waiting for response");
			if (null != pSocket)
			{
				long currentTime = System.DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				while (!pSocket.GetStream().DataAvailable)
				{

					long compareTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - currentTime;
					// Terminate waiting after waiting to long. This indicates that the server is not responding
					if (compareTime > RESPONSE_TIMEOUT)
					{

						Debug.WriteLine("Stuck waiting for server response");

						throw new IOException();
					}
					//                if ( compareTime > 500L * 1000L * 1000L ) {
					//                    SystemClock.sleep(RESPONSE_WAIT_SLEEP_TIME);
					//                }
				}
			}
			else
			{
				throw new IOException();
			}
		}

		/**
		 * Checks if a simple command was successful or not (OK vs. ACK)
		 * <p>
		 * This should only be used for simple commands like play,pause, setVolume, ...
		 *
		 * @return True if command was successfully executed, false otherwise.
		 */
		public bool CheckResponse()
		{
			bool success = false;
			String response;

			Debug.WriteLine("Check response");

			// Wait for data to be available to read. MPD communication could take some time.
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				while (reader.Peek() >= 0)
				{
					response = ReadLine(reader);
					if (response.StartsWith("OK"))
					{
						success = true;
					}
					else if (response.StartsWith("ACK"))
					{
						success = false;
						Debug.WriteLine("Server response error: " + response);
					}
				}
			}
			//reader.Dispose();
			Debug.WriteLine("Response: " + success);
			// The command was handled now it is time to set the connection to idle again (after the timeout,
			// to prevent disconnecting).
			StartIdleWait();
			Debug.WriteLine("Started idle wait");
			// Return if successful or not.
			return success;
		}

		public bool IsConnected()
		{
			if (null != pSocket && pMPDConnectionReady && pSocket.Connected)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/*
		 * *******************************
		 * * Response handling functions *
		 * *******************************
		 */

		/**
		 * Parses the return of MPD when a list of albums was requested.
		 *
		 * @return List of MPDAlbum objects
		 * @throws IOException
		 */
		private List<MPDAlbum> ParseMPDAlbums()
		{
			List<MPDAlbum> albumList = new List<MPDAlbum>();
			if (!IsConnected())
			{
				return albumList;
			}
			/* Parse the MPD response and create a list of MPD albums */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				String response = ReadLine(reader);

				String albumName = "";

				MPDAlbum tempAlbum = null;
				while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
				{
					/* Check if the response is an album */
					if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_NAME))
					{
						/* We found an album, add it to the list. */
						if (null != tempAlbum)
						{
							albumList.Add(tempAlbum);
						}
						albumName = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_NAME.Length);
						tempAlbum = new MPDAlbum(albumName);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_MBID))
					{
						// FIXME this crashed with a null-ptr. This should not happen. Investigate if repeated. (Protocol should always send "Album:" first
						tempAlbum.musicbrainzID = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_MBID.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_NAME))
					{
						/* Check if the response is a albumartist. */
						tempAlbum.artistName = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_NAME.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_DATE))
					{
						// Try to parse Date
						String dateString = response.Substring(MPDResponses.MPD_RESPONSE_DATE.Length);


						try
						{
							DateTime date = DateTime.Parse(dateString);
							tempAlbum.date = date;
						}
						catch (FormatException e)
						{
							Debug.WriteLine(e.StackTrace);
						}
					}
					response = ReadLine(reader);
				}

				if (null != response && response.StartsWith("ACK") && response.Contains(MPDResponses.MPD_PARSE_ARGS_LIST_ERROR))
				{
					Debug.WriteLine("Error parsing artists: " + response);
					EnableMopidyWorkaround();
				}

				/* Because of the loop structure the last album has to be added because no
				"ALBUM:" is sent anymore.
				 */
				if (null != tempAlbum)
				{
					albumList.Add(tempAlbum);
				}
			}

			Debug.WriteLine("Parsed: " + albumList.Count + " albums");

			// Start the idling timeout again.
			StartIdleWait();
			// Sort the albums for later sectioning.
			albumList.Sort();
			//reader.Dispose();
			return albumList;
		}

		/**
     * Parses the return stream of MPD when a list of artists was requested.
     *
     * @return List of MPDArtists objects
     * @throws IOException
     */
		private List<MPDArtist> ParseMPDArtists()
		{
			List<MPDArtist> artistList = new List<MPDArtist>();
			if (!IsConnected())
			{
				return artistList;
			}

			/* Parse MPD artist return values and create a list of MPDArtist objects */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				String response = ReadLine(reader);

				/* Artist properties */
				String artistName = null;
				String artistMBID = "";

				MPDArtist tempArtist = null;

				while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
				{

					if (response == null)
					{
						/* skip this invalid (empty) response */
						continue;
					}

					if (response.StartsWith(MPDResponses.MPD_RESPONSE_ARTIST_NAME))
					{
						if (null != tempArtist)
						{
							artistList.Add(tempArtist);
						}
						artistName = response.Substring(MPDResponses.MPD_RESPONSE_ARTIST_NAME.Length);
						tempArtist = new MPDArtist(artistName);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUMARTIST_NAME))
					{
						if (null != tempArtist)
						{
							artistList.Add(tempArtist);
						}
						artistName = response.Substring(MPDResponses.MPD_RESPONSE_ALBUMARTIST_NAME.Length);
						tempArtist = new MPDArtist(artistName);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ARTIST_MBID))
					{
						artistMBID = response.Substring(MPDResponses.MPD_RESPONSE_ARTIST_MBID.Length);
						tempArtist.addMusicbrainzID(artistMBID);
					}
					else if (response.StartsWith("OK"))
					{
						break;
					}
					response = ReadLine(reader);
				}

				//reader.Dispose();

				if (null != response && response.StartsWith("ACK") && response.Contains(MPDResponses.MPD_PARSE_ARGS_LIST_ERROR))
				{
					Debug.WriteLine(TAG + "\nError parsing artists: " + response);

					EnableMopidyWorkaround();
				}

				// Add last artist
				if (null != tempArtist)
				{
					artistList.Add(tempArtist);
				}
			}

			Debug.WriteLine("Parsed: " + artistList.Count + " artists");

			// Start the idling timeout again.
			StartIdleWait();

			// Sort the artists for later sectioning.
			artistList.Sort();

			// If we used MBID filtering, it could happen that a user as an artist in the list multiple times,
			// once with and once without MBID. Try to filter this by sorting the list first by name and mbid count
			// and then remove duplicates.
			if (mServerCapabilities.hasMusicBrainzTags() && mServerCapabilities.hasListGroup())
			{
				List<MPDArtist> clearedList = new List<MPDArtist>();

				// Remove multiple entries when one artist is in list with and without MBID
				for (int i = 0; i < artistList.Count; i++)
				{
					MPDArtist artist = artistList[i];
					if (i + 1 != artistList.Count)
					{
						MPDArtist nextArtist = artistList[i + 1];
						if (!artist.artistName.Equals(nextArtist.artistName))
						{
							clearedList.Add(artist);
						}
					}
					else
					{
						clearedList.Add(artist);
					}
				}
				return clearedList;
			}
			else
			{
				return artistList;
			}
		}

		/**
     * Parses the response of mpd on requests that return track items. This is also used
     * for MPD file, directory and playlist responses. This allows the GUI to develop
     * one adapter for all three types. Also MPD mixes them when requesting directory listings.
     * <p/>
     * It will return a list of MPDFileEntry objects which is a parent class for (MPDTrack, MPDPlaylist,
     * MPDDirectory) you can use instanceof to check which type you got.
     *
     * @param filterArtist    Artist used for filtering against the Artist AND AlbumArtist tag. Non matching tracks
     *                        will be discarded.
     * @param filterAlbumMBID MusicBrainzID of the album that is also used as a filter criteria.
     *                        This can be used to differentiate albums with same name, same artist but different MBID.
     *                        This is often the case for soundtrack releases. (E.g. LOTR DVD-Audio vs. CD release)
     * @return List of MPDFileEntry objects
     * @throws IOException
     */
		private List<MPDFileEntry> ParseMPDTracks(String filterArtist, String filterAlbumMBID)
		{
			List<MPDFileEntry> trackList = new List<MPDFileEntry>();
			if (!IsConnected())
			{
				return trackList;
			}

			/* Temporary track item (added to list later */
			MPDFileEntry tempFileEntry = null;


			/* Response line from MPD */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				String response = ReadLine(reader);
				while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
				{
					/* This if block will just check all the different response possible by MPDs file/dir/playlist response */
					if (response.StartsWith(MPDResponses.MPD_RESPONSE_FILE))
					{
						if (null != tempFileEntry)
						{
							/* Check the artist filter criteria here */
							if (tempFileEntry is MPDTrack)
							{
								MPDTrack file = tempFileEntry as MPDTrack;
								if ((filterArtist.Equals("") || filterArtist.Equals(file.TrackAlbumArtist) || filterArtist.Equals(file.TrackArtist))
												&& (filterAlbumMBID.Equals("") || filterAlbumMBID.Equals(file.AlbumMusicbrainzID)))
								{
									trackList.Add(tempFileEntry);
								}
							}
							else
							{
								trackList.Add(tempFileEntry);
							}
						}
						tempFileEntry = new MPDTrack(response.Substring(MPDResponses.MPD_RESPONSE_FILE.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_TRACK_TITLE))
					{
						((MPDTrack)tempFileEntry).TrackTitle = response.Substring(MPDResponses.MPD_RESPONSE_TRACK_TITLE.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ARTIST_NAME))
					{
						((MPDTrack)tempFileEntry).TrackArtist = response.Substring(MPDResponses.MPD_RESPONSE_ARTIST_NAME.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_NAME))
					{
						((MPDTrack)tempFileEntry).TrackAlbumArtist = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_NAME.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_NAME))
					{
						((MPDTrack)tempFileEntry).TrackAlbum = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_NAME.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_DATE))
					{
						((MPDTrack)tempFileEntry).Date = (response.Substring(MPDResponses.MPD_RESPONSE_DATE.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_MBID))
					{
						((MPDTrack)tempFileEntry).AlbumMusicbrainzID = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_MBID.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ARTIST_MBID))
					{
						((MPDTrack)tempFileEntry).ArtistMusicbrainzID = response.Substring(MPDResponses.MPD_RESPONSE_ARTIST_MBID.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_MBID))
					{
						((MPDTrack)tempFileEntry).AlbumArtistMusicbrainzID = response.Substring(MPDResponses.MPD_RESPONSE_ALBUM_ARTIST_MBID.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_TRACK_MBID))
					{
						((MPDTrack)tempFileEntry).TrackMusicbrainzID = (response.Substring(MPDResponses.MPD_RESPONSE_TRACK_MBID.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_TRACK_TIME))
					{
						((MPDTrack)tempFileEntry).LengthInSeconds = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_TRACK_TIME.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_SONG_ID))
					{
						((MPDTrack)tempFileEntry).PlaylistSongID = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_SONG_ID.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_SONG_POS))
					{
						((MPDTrack)tempFileEntry).PlaylistPosition = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_SONG_POS.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_DISC_NUMBER))
					{
						/*
						* Check if MPD returned a discnumber like: "1" or "1/3" and set disc count accordingly.
						*/
						String discNumber = response.Substring(MPDResponses.MPD_RESPONSE_DISC_NUMBER.Length);
						discNumber = discNumber.Replace(" ", "");
						String[] discNumberSep = discNumber.Split('/');
						if (discNumberSep.Length > 0)
						{
							try
							{
								((MPDTrack)tempFileEntry).DiscNumber = int.Parse(discNumberSep[0]);
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}

							if (discNumberSep.Length > 1)
							{
								try
								{
									((MPDTrack)tempFileEntry).AlbumDiscCount = int.Parse(discNumberSep[1]);
								}
								catch (FormatException e)
								{
									Debug.WriteLine(e.StackTrace);
								}
							}
						}
						else
						{
							try
							{
								((MPDTrack)tempFileEntry).DiscNumber = int.Parse(discNumber);
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_TRACK_NUMBER))
					{
						/*
						 * Check if MPD returned a tracknumber like: "12" or "12/42" and set albumtrack count accordingly.
						 */
						String trackNumber = response.Substring(MPDResponses.MPD_RESPONSE_TRACK_NUMBER.Length);
						trackNumber = trackNumber.Replace(" ", "");
						String[] trackNumbersSep = trackNumber.Split('/');
						if (trackNumbersSep.Length > 0)
						{
							try
							{
								((MPDTrack)tempFileEntry).AlbumTrackNumber = int.Parse(trackNumbersSep[0]);
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
							if (trackNumbersSep.Length > 1)
							{
								try
								{
									((MPDTrack)tempFileEntry).AlbumTrackCount = int.Parse(trackNumbersSep[1]);
								}
								catch (FormatException e)
								{
									Debug.WriteLine(e.StackTrace);
								}
							}
						}
						else
						{
							try
							{
								((MPDTrack)tempFileEntry).AlbumTrackNumber = int.Parse(trackNumber);
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_LAST_MODIFIED))
					{
						tempFileEntry.LastModified = response.Substring(MPDResponses.MPD_RESPONSE_LAST_MODIFIED.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_PLAYLIST))
					{
						if (null != tempFileEntry)
						{
							/* Check the artist filter criteria here */
							if (tempFileEntry is MPDTrack)
							{
								MPDTrack file = tempFileEntry as MPDTrack;
								if ((filterArtist.Equals("") || filterArtist.Equals(file.TrackAlbumArtist) || filterArtist.Equals(file.TrackArtist))
												&& (filterAlbumMBID.Equals("") || filterAlbumMBID.Equals(file.AlbumMusicbrainzID)))
								{
									trackList.Add(tempFileEntry);
								}
							}
							else
							{
								trackList.Add(tempFileEntry);
							}
						}
						tempFileEntry = new MPDPlaylist(response.Substring(MPDResponses.MPD_RESPONSE_PLAYLIST.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_RESPONSE_DIRECTORY))
					{
						if (null != tempFileEntry)
						{
							/* Check the artist filter criteria here */
							if (tempFileEntry is MPDTrack)
							{
								MPDTrack file = tempFileEntry as MPDTrack;
								if ((filterArtist.Equals("") || filterArtist.Equals(file.TrackAlbumArtist) || filterArtist.Equals(file.TrackArtist))
												&& (filterAlbumMBID.Equals("") || filterAlbumMBID.Equals(file.AlbumMusicbrainzID)))
								{
									trackList.Add(tempFileEntry);
								}
							}
							else
							{
								trackList.Add(tempFileEntry);
							}
						}
						tempFileEntry = new MPDDirectory(response.Substring(MPDResponses.MPD_RESPONSE_DIRECTORY.Length));
					}

					// Move to the next line.
					response = ReadLine(reader);

				}
				//reader.Dispose();
				/* Add last remaining track to list. */
				if (null != tempFileEntry)
				{
					/* Check the artist filter criteria here */
					if (tempFileEntry is MPDTrack)
					{
						MPDTrack file = tempFileEntry as MPDTrack;
						if ((filterArtist.Equals("") || filterArtist.Equals(file.TrackAlbumArtist) || filterArtist.Equals(file.TrackArtist))
										&& (filterAlbumMBID.Equals("") || filterAlbumMBID.Equals(file.AlbumMusicbrainzID)))
						{
							trackList.Add(tempFileEntry);
						}
					}
					else
					{
						trackList.Add(tempFileEntry);
					}
				}
			}

			StartIdleWait();
			return trackList;
		}

		/*
		* **********************
		* * Request functions  
		* **********************
		*/

		/**
     * Get a list of all albums available in the database.
     *
     * @return List of MPDAlbum
     */
		public List<MPDAlbum> GetAlbums()
		{
			// Get a list of albums. Check if server is new enough for MB and AlbumArtist filtering
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUMS(mServerCapabilities));
				try
				{
					// Remove empty albums at beginning of the list
					List<MPDAlbum> albums = ParseMPDAlbums();

					albums.RemoveAll(album => album.name.Equals(""));

					return albums;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Get a list of all albums available in the database.
		 *
		 * @return List of MPDAlbum
		 */
		public List<MPDAlbum> GetAlbumsInPath(String path)
		{
			lock (_lock)
			{
				// Get a list of albums. Check if server is new enough for MB and AlbumArtist filtering
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUMS_FOR_PATH(path, mServerCapabilities));
				try
				{
					// Remove empty albums at beginning of the list
					List<MPDAlbum> albums = ParseMPDAlbums();
					albums.RemoveAll(album => album.name.Equals(""));
					return albums;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Get a list of all albums by an artist where artist is part of or artist is the AlbumArtist (tag)
		 *
		 * @param artistName Artist to filter album list with.
		 * @return List of MPDAlbum objects
		 */
		public List<MPDAlbum> GetArtistAlbums(String artistName)
		{
			lock (_lock)
			{
				// Get all albums that artistName is part of (Also the legacy album list pre v. 0.19)
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ARTIST_ALBUMS(artistName, mServerCapabilities));

				try
				{
					if (mServerCapabilities.hasTagAlbumArtist() && mServerCapabilities.hasListGroup())
					{

						// Also get the list where artistName matches on AlbumArtist
						SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUMARTIST_ALBUMS(artistName, mServerCapabilities));

						var result = new HashSet<MPDAlbum>(ParseMPDAlbums());

						List<MPDAlbum> resultList = new List<MPDAlbum>(result);

						// Sort the created list
						resultList.Sort();
						return resultList;
					}
					else
					{
						List<MPDAlbum> result = ParseMPDAlbums();
						return result;
					}

				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Get a list of all artists available in MPDs database
		 *
		 * @return List of MPDArtist objects
		 */
		public List<MPDArtist> GetArtists()
		{
			lock (_lock)
			{
				// Get a list of artists. If server is new enough this will contain MBIDs for artists, that are tagged correctly.
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ARTISTS(mServerCapabilities.hasListGroup() && mServerCapabilities.hasMusicBrainzTags()));
				try
				{
					// Remove first empty artist
					List<MPDArtist> artists = ParseMPDArtists();
					if (artists.Count > 0 && artists[0].artistName.Equals(""))
					{
						artists.RemoveAt(0);
					}

					return artists;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Get a list of all album artists available in MPDs database
		 *
		 * @return List of MPDArtist objects
		 */
		public List<MPDArtist> GetAlbumArtists()
		{
			lock (_lock)
			{
				// Get a list of artists. If server is new enough this will contain MBIDs for artists, that are tagged correctly.
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUMARTISTS(mServerCapabilities.hasListGroup() && mServerCapabilities.hasMusicBrainzTags()));
				try
				{
					List<MPDArtist> artists = ParseMPDArtists();
					if (artists.Count > 0 && artists[0].artistName.Equals(""))
					{
						artists.RemoveAt(0);
					}

					return artists;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Get a list of all playlists available in MPDs database
		 *
		 * @return List of MPDArtist objects
		 */
		public List<MPDFileEntry> GetPlaylists()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_SAVED_PLAYLISTS);
				try
				{
					List<MPDFileEntry> playlists = ParseMPDTracks("", "");
					playlists.Sort();
					return playlists;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Gets all tracks from MPD server. This could take a long time to process. Be warned.
		 *
		 * @return A list of all tracks in MPDTrack objects
		 */
		public List<MPDFileEntry> GetAllTracks()
		{
			lock (_lock)
			{
				//Log.w(TAG, "This command should not be used");
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALL_FILES);
				try
				{
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}


		/**
		 * Returns the list of tracks that are part of albumName
		 *
		 * @param albumName Album to get tracks from
		 * @return List of MPDTrack track objects
		 */
		public List<MPDFileEntry> GetAlbumTracks(String albumName, String mbid)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUM_TRACKS(albumName));
				try
				{
					List<MPDFileEntry> result = ParseMPDTracks("", mbid);
					result.Sort(MPDFileEntry.CompareByFileIndex);
					return result;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Returns the list of tracks that are part of albumName and from artistName
		 *
		 * @param albumName  Album name used as primary filter.
		 * @param artistName Artist to filter with. This is checked with Artist AND AlbumArtist tag.
		 * @param mbid       MusicBrainzID of the album to get tracks from. Necessary if one item with the
		 *                   same name exists multiple times.
		 * @return List of MPDTrack track objects
		 */
		public List<MPDFileEntry> GetArtistAlbumTracks(String albumName, String artistName, String mbid)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REQUEST_ALBUM_TRACKS(albumName));
				try
				{
					/* Filter tracks with artistName */
					List<MPDFileEntry> result = ParseMPDTracks(artistName, mbid);
					// Sort with disc & track number
					result.Sort(MPDFileEntry.CompareByFileIndex);
					return result;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the current playlist of the server
		 *
		 * @return List of MPDTrack items with all tracks of the current playlist
		 */
		public List<MPDFileEntry> GetCurrentPlaylist()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_CURRENT_PLAYLIST);
				try
				{
					/* Parse the return */
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the current playlist of the server with a window
		 *
		 * @return List of MPDTrack items with all tracks of the current playlist
		 */
		public List<MPDFileEntry> GetCurrentPlaylistWindow(int start, int end)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_CURRENT_PLAYLIST_WINDOW(start, end));
				try
				{
					/* Parse the return */
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the current playlist of the server
		 *
		 * @return List of MPDTrack items with all tracks of the current playlist
		 */
		public List<MPDFileEntry> GetSavedPlaylist(String playlistName)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_SAVED_PLAYLIST(playlistName));
				try
				{
					/* Parse the return */
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the files for a specific path with info
		 *
		 * @return List of MPDTrack items with all tracks of the current playlist
		 */
		public List<MPDFileEntry> GetFiles(String path)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_FILES_INFO(path));
				try
				{
					/* Parse the return */
					List<MPDFileEntry> retList = ParseMPDTracks("", "");
					retList.Sort();
					return retList;
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the files for a specific search term and type
		 *
		 * @param term The search term to use
		 * @param type The type of items to search
		 * @return List of MPDTrack items with all tracks matching the search
		 */
		public List<MPDFileEntry> GetSearchedFiles(String term, MPDCommands.MPD_SEARCH_TYPE type)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SEARCH_FILES(term, type));
				try
				{
					/* Parse the return */
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Searches a URL in the current playlist. If available the track is part of the returned list.
		 *
		 * @param url URL to search in the current playlist.
		 * @return List with one entry or none.
		 */
		public List<MPDFileEntry> GetPlaylistFindTrack(String url)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_PLAYLIST_FIND_URI(url));
				try
				{
					/* Parse the return */
					return ParseMPDTracks("", "");
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return null;
				}
			}
		}

		/**
		 * Requests the currentstatus package from the mpd server.
		 *
		 * @return The CurrentStatus object with all gathered information.
		 */
		public MPDCurrentStatus GetCurrentServerStatus()
		{
			lock (_lock)
			{
				MPDCurrentStatus status = new MPDCurrentStatus();

				/* Request status */
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_CURRENT_STATUS);

				/* Response line from MPD */
				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{

					String response = null;
					response = ReadLine(reader);

					while (!response.StartsWith("OK") && !response.StartsWith("ACK"))
					{
						if (response.StartsWith(MPDResponses.MPD_RESPONSE_VOLUME))
						{
							try
							{
								status.Volume = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_VOLUME.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_REPEAT))
						{
							try
							{
								status.Repeat = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_REPEAT.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_RANDOM))
						{
							try
							{
								status.Random = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_RANDOM.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_SINGLE))
						{
							try
							{
								status.SinglePlayback = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_SINGLE.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_CONSUME))
						{
							try
							{
								status.Consume = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_CONSUME.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_PLAYLIST_VERSION))
						{
							try
							{
								status.playlistVersion = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_PLAYLIST_VERSION.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_PLAYLIST_LENGTH))
						{
							try
							{
								status.playlistLength = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_PLAYLIST_LENGTH.Length));
							}
							catch (FormatException)
							{

							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_PLAYBACK_STATE))
						{
							String state = response.Substring(MPDResponses.MPD_RESPONSE_PLAYBACK_STATE.Length);

							if (state.Equals(MPDResponses.MPD_PLAYBACK_STATE_RESPONSE_PLAY))
							{
								status.playbackState = MPDCurrentStatus.MPD_PLAYBACK_STATE.MPD_PLAYING;
							}
							else if (state.Equals(MPDResponses.MPD_PLAYBACK_STATE_RESPONSE_PAUSE))
							{
								status.playbackState = MPDCurrentStatus.MPD_PLAYBACK_STATE.MPD_PAUSING;
							}
							else if (state.Equals(MPDResponses.MPD_PLAYBACK_STATE_RESPONSE_STOP))
							{
								status.playbackState = MPDCurrentStatus.MPD_PLAYBACK_STATE.MPD_STOPPED;
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_CURRENT_SONG_INDEX))
						{
							status.currentSongIndex = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_CURRENT_SONG_INDEX.Length));
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_NEXT_SONG_INDEX))
						{
							status.nextSongIndex = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_NEXT_SONG_INDEX.Length));
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_TIME_INFORMATION_OLD))
						{
							String timeInfo = response.Substring(MPDResponses.MPD_RESPONSE_TIME_INFORMATION_OLD.Length);

							String[] timeInfoSep = timeInfo.Split(':');
							if (timeInfoSep.Length == 2)
							{
								status.elapsedTime = int.Parse(timeInfoSep[0]);
								status.currentTrackLength = int.Parse(timeInfoSep[1]);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_ELAPSED_TIME))
						{
							try
							{
								status.elapsedTime = (int)Math.Round(float.Parse(response.Substring(MPDResponses.MPD_RESPONSE_ELAPSED_TIME.Length)));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_DURATION))
						{
							try
							{
								status.currentTrackLength = (int)Math.Round(float.Parse(response.Substring(MPDResponses.MPD_RESPONSE_DURATION.Length)));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_BITRATE))
						{
							try
							{
								status.bitrate = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_BITRATE.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_AUDIO_INFORMATION))
						{
							String audioInfo = response.Substring(MPDResponses.MPD_RESPONSE_AUDIO_INFORMATION.Length);

							String[] audioInfoSep = audioInfo.Split(':');
							if (audioInfoSep.Length == 3)
							{
								/* Extract the separate pieces */
								try
								{
									/* First is sampleRate */
									status.samplerate = int.Parse(audioInfoSep[0]);
									/* Second is bitresolution */
									status.bitDepth = audioInfoSep[1];
									/* Third is channel count */
									status.channelCount = int.Parse(audioInfoSep[2]);
								}
								catch (FormatException e)
								{
									Debug.WriteLine(e.StackTrace);
								}
							}
						}
						else if (response.StartsWith(MPDResponses.MPD_RESPONSE_UPDATING_DB))
						{
							try
							{
								status.updateDBJob = int.Parse(response.Substring(MPDResponses.MPD_RESPONSE_UPDATING_DB.Length));
							}
							catch (FormatException e)
							{
								Debug.WriteLine(e.StackTrace);
							}
						}

						response = ReadLine(reader);

					}
				}
				//reader.Dispose();
				StartIdleWait();

				return status;
			}
		}

		/**
		 * Requests the server statistics package from the mpd server.
		 *
		 * @return The CurrentStatus object with all gathered information.
		 */
		public MPDStatistics GetServerStatistics()
		{
			lock (_lock)
			{
				MPDStatistics stats = new MPDStatistics();

				/* Request status */
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_STATISTICS);

				/* Response line from MPD */
				String response = null;
				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{
					response = ReadLine(reader);
					try
					{
						while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
						{
							if (response.StartsWith(MPDResponses.MPD_STATS_UPTIME))
							{
								stats.serverUptime = int.Parse(response.Substring(MPDResponses.MPD_STATS_UPTIME.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_PLAYTIME))
							{
								stats.playDuration = int.Parse(response.Substring(MPDResponses.MPD_STATS_PLAYTIME.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_ARTISTS))
							{
								stats.artistsCount = int.Parse(response.Substring(MPDResponses.MPD_STATS_ARTISTS.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_ALBUMS))
							{
								stats.albumCount = int.Parse(response.Substring(MPDResponses.MPD_STATS_ALBUMS.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_SONGS))
							{
								stats.songCount = int.Parse(response.Substring(MPDResponses.MPD_STATS_SONGS.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_DB_PLAYTIME))
							{
								stats.allSongDuration = int.Parse(response.Substring(MPDResponses.MPD_STATS_DB_PLAYTIME.Length));
							}
							else if (response.StartsWith(MPDResponses.MPD_STATS_DB_LAST_UPDATE))
							{
								stats.lastDBUpdate = long.Parse(response.Substring(MPDResponses.MPD_STATS_DB_LAST_UPDATE.Length));
							}

							response = ReadLine(reader);

						}
					}
					catch (FormatException e)
					{
						Debug.WriteLine(e.StackTrace);
					}
					finally
					{
						//reader.Dispose();
					}
				}
				StartIdleWait();
				return stats;
			}
		}

		/**
		 * This will query the current song playing on the mpd server.
		 *
		 * @return MPDTrack entry for the song playing.
		 */
		public MPDTrack GetCurrentSong()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_CURRENT_SONG);

				// Reuse the parsing function for tracks here.
				List<MPDFileEntry> retList = null;
				try
				{
					retList = ParseMPDTracks("", "");
				}
				catch (IOException)
				{
					HandleSocketError();
					return null;
				}
				if (retList.Count == 1)
				{
					// If one element is in the list it is safe to assume that this element is
					// the current song. So casting is no problem.
					return (MPDTrack)retList[0];
				}
				else
				{
					return null;
				}
			}
		}


		/*
		 ***********************
		 *    Control commands *
		 ***********************
		 */

		/**
		 * Sends the pause commando to MPD.
		 *
		 * @param pause 1 if playback should be paused, 0 if resumed
		 * @return
		 */
		public bool Pause(bool pause)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_PAUSE(pause));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Jumps to the next song
		 *
		 * @return true if successful, false otherwise
		 */
		public bool NextSong()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_NEXT);

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Jumps to the previous song
		 *
		 * @return true if successful, false otherwise
		 */
		public bool PreviousSong()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_PREVIOUS);

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Stops playback
		 *
		 * @return true if successful, false otherwise
		 */
		public bool StopPlayback()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_STOP);

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Sets random to true or false
		 *
		 * @param random If random should be set (true) or not (false)
		 * @return True if server responed with ok
		 */
		public bool SetRandom(bool random)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SET_RANDOM(random));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Sets repeat to true or false
		 *
		 * @param repeat If repeat should be set (true) or not (false)
		 * @return True if server responed with ok
		 */
		public bool SetRepeat(bool repeat)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SET_REPEAT(repeat));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Sets single playback to enable (true) or disabled (false)
		 *
		 * @param single if single playback should be enabled or not.
		 * @return True if server responed with ok
		 */
		public bool SetSingle(bool single)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SET_SINGLE(single));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Sets if files should be removed after playback (consumed)
		 *
		 * @param consume True if yes and false if not.
		 * @return True if server responed with ok
		 */
		public bool SetConsume(bool consume)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SET_CONSUME(consume));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Plays the song with the index in the current playlist.
		 *
		 * @param index Index of the song that should be played.
		 * @return True if server responed with ok
		 */
		public bool PlaySongIndex(int index)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_PLAY_SONG_INDEX(index));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Seeks the currently playing song to a certain position
		 *
		 * @param seconds Position in seconds to which a seek is requested to.
		 * @return True if server responed with ok
		 */
		public bool SeekSeconds(int seconds)
		{
			lock (_lock)
			{
				MPDCurrentStatus status = null;
				status = GetCurrentServerStatus();


				SendMPDCommand(MPDCommands.MPD_COMMAND_SEEK_SECONDS(status.currentSongIndex, seconds));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Sets the volume of the mpd servers output. It is an absolute value between (0-100).
		 *
		 * @param volume Volume to set to the server.
		 * @return True if server responed with ok
		 */
		public bool SetVolume(int volume)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SET_VOLUME(volume));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/*
		 ***********************
		 *    Queue commands   *
		 ***********************
		 */

		/**
		 * This method adds songs in a bulk command list. Should be reasonably in performance this way.
		 *
		 * @param tracks List of MPDFileEntry objects to add to the current playlist.
		 * @return True if server responed with ok
		 */
		public bool AddTrackList(List<MPDFileEntry> tracks)
		{
			lock (_lock)
			{
				if (null == tracks)
				{
					return false;
				}
				StartCommandList();

				foreach (MPDFileEntry track in tracks)
				{
					if (track is MPDTrack)
					{
						SendMPDRAWCommand(MPDCommands.MPD_COMMAND_ADD_FILE(track.Path));
					}
				}

				EndCommandList();

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}
		/**
     * Adds all tracks from a certain album from artistname to the current playlist.
     *
     * @param albumname  Name of the album to add to the current playlist.
     * @param artistname Name of the artist of the album to add to the list. This
     *                   allows filtering of album tracks to a specified artist. Can also
     *                   be left empty then all tracks from the album will be added.
     * @return True if server responed with ok
     */
		public bool AddAlbumTracks(String albumname, String artistname, String mbid)
		{
			lock (_lock)
			{
				List<MPDFileEntry> tracks = GetArtistAlbumTracks(albumname, artistname, mbid);
				return AddTrackList(tracks);
			}
		}

		/**
		 * Adds all albums of an artist to the current playlist. Will first get a list of albums for the
		 * artist and then call addAlbumTracks for every album on this result.
		 *
		 * @param artistname Name of the artist to enqueue the albums from.
		 * @return True if server responed with ok
		 */
		public bool AddArtist(String artistname)
		{
			lock (_lock)
			{
				List<MPDAlbum> albums = GetArtistAlbums(artistname);
				if (null == albums)
				{
					return false;
				}

				bool success = true;
				foreach (MPDAlbum album in albums)
				{
					// This will add all tracks from album where artistname is either the artist or
					// the album artist.
					if (!(AddAlbumTracks(album.name, artistname, "")))
					{
						success = false;
					}
				}
				return success;
			}
		}

		/**
		 * Adds a single File/Directory to the current playlist.
		 *
		 * @param url URL of the file or directory! to add to the current playlist.
		 * @return True if server responed with ok
		 */
		public bool AddSong(String url)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_ADD_FILE(url));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * This method adds a song to a specified positiion in the current playlist.
		 * This allows GUI developers to implement a method like "add after current".
		 *
		 * @param url   URL to add to the playlist.
		 * @param index Index at which the item should be added.
		 * @return True if server responed with ok
		 */
		public bool AddSongatIndex(String url, int index)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_ADD_FILE_AT_INDEX(url, index));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Adds files to the playlist with a search term for a specific type
		 *
		 * @param term The search term to use
		 * @param type The type of items to search
		 * @return True if server responed with ok
		 */
		public bool AddSearchedFiles(String term, MPDCommands.MPD_SEARCH_TYPE type)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_ADD_SEARCH_FILES(term, type));
				try
				{
					/* Parse the return */
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
					return false;
				}
			}
		}

		/**
		 * Instructs the mpd server to clear its current playlist.
		 *
		 * @return True if server responed with ok
		 */
		public bool ClearPlaylist()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_CLEAR_PLAYLIST);
				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Instructs the mpd server to shuffle its current playlist.
		 *
		 * @return True if server responed with ok
		 */
		public bool ShufflePlaylist()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SHUFFLE_PLAYLIST);
				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Instructs the mpd server to remove one item from the current playlist at index.
		 *
		 * @param index Position of the item to remove from current playlist.
		 * @return True if server responed with ok
		 */
		public bool RemoveIndex(int index)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REMOVE_SONG_FROM_CURRENT_PLAYLIST(index));
				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Instructs the mpd server to remove an range of songs from current playlist.
		 *
		 * @param start Start of songs to remoge
		 * @param end End of the range
		 * @return True if server responed with ok
		 */
		public bool RemoveRange(int start, int end)
		{
			lock (_lock)
			{
				// Check capabilities if removal with one command is possible
				if (mServerCapabilities.hasCurrentPlaylistRemoveRange())
				{
					SendMPDCommand(MPDCommands.MPD_COMMAND_REMOVE_RANGE_FROM_CURRENT_PLAYLIST(start, end + 1));
				}
				else
				{
					// Create commandlist instead
					StartCommandList();
					for (int i = start; i <= end; i++)
					{
						SendMPDRAWCommand(MPDCommands.MPD_COMMAND_REMOVE_SONG_FROM_CURRENT_PLAYLIST(start));
					}
					EndCommandList();
				}


				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Moves one item from an index in the current playlist to an new index. This allows to move
		 * tracks for example after the current to priotize songs.
		 *
		 * @param from Item to move from.
		 * @param to   Position to enter item
		 * @return
		 */
		public bool MoveSongFromTo(int from, int to)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_MOVE_SONG_FROM_INDEX_TO_INDEX(from, to));
				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Saves the current playlist as a new playlist with a name.
		 *
		 * @param name Name of the playlist to save to.
		 * @return True if server responed with ok
		 */
		public bool SavePlaylist(String name)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_SAVE_PLAYLIST(name));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Adds a song to the saved playlist
		 *
		 * @param playlistName Name of the playlist to add the url to.
		 * @param url          URL to add to the saved playlist
		 * @return True if server responed with ok
		 */
		public bool AddSongToPlaylist(String playlistName, String url)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_ADD_TRACK_TO_PLAYLIST(playlistName, url));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Removes a song from a saved playlist
		 *
		 * @param playlistName Name of the playlist of which the song should be removed from
		 * @param position     Index of the song to remove from the lits
		 * @return
		 */
		public bool RemoveSongFromPlaylist(String playlistName, int position)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REMOVE_TRACK_FROM_PLAYLIST(playlistName, position));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Removes a saved playlist from the servers database.
		 *
		 * @param name Name of the playlist to remove.
		 * @return True if server responed with ok
		 */
		public bool RemovePlaylist(String name)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_REMOVE_PLAYLIST(name));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Loads a saved playlist (added after the last song) to the current playlist.
		 *
		 * @param name Of the playlist to add to.
		 * @return True if server responed with ok
		 */
		public bool LoadPlaylist(String name)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_LOAD_PLAYLIST(name));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * Private parsing method for MPDs output lists.
		 *
		 * @return A list of MPDOutput objects with name,active,id values if successful. Otherwise empty list.
		 * @throws IOException
		 */
		private List<MPDOutput> ParseMPDOutputs()
		{
			List<MPDOutput> outputList = new List<MPDOutput>();
			// Parse outputs
			String outputName = null;
			bool outputActive = false;
			int outputId = -1;

			if (!IsConnected())
			{
				return null;
			}

			/* Response line from MPD */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				String response = ReadLine(reader);
				while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
				{
					if (response.StartsWith(MPDResponses.MPD_OUTPUT_ID))
					{
						if (null != outputName)
						{
							MPDOutput tempOutput = new MPDOutput(outputName, outputActive, outputId);
							outputList.Add(tempOutput);
						}
						outputId = int.Parse(response.Substring(MPDResponses.MPD_OUTPUT_ID.Length));
					}
					else if (response.StartsWith(MPDResponses.MPD_OUTPUT_NAME))
					{
						outputName = response.Substring(MPDResponses.MPD_OUTPUT_NAME.Length);
					}
					else if (response.StartsWith(MPDResponses.MPD_OUTPUT_ACTIVE))
					{
						String activeRespsonse = response.Substring(MPDResponses.MPD_OUTPUT_ACTIVE.Length);
						if (activeRespsonse.Equals("1"))
						{
							outputActive = true;
						}
						else
						{
							outputActive = false;
						}
					}
					response = ReadLine(reader);
				}
				//reader.Dispose();
				// Add remaining output to list
				if (null != outputName)
				{
					MPDOutput tempOutput = new MPDOutput(outputName, outputActive, outputId);
					outputList.Add(tempOutput);
				}
			}
			return outputList;

		}

		/**
		 * Private parsing method for MPDs command list
		 *
		 * @return A list of Strings of commands that are allowed on the server
		 * @throws IOException
		 */
		private List<String> ParseMPDCommands()
		{
			List<String> commandList = new List<string>();
			// Parse outputs
			String commandName = null;
			if (!IsConnected())
			{
				return commandList;
			}

			/* Response line from MPD */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{


				String response = ReadLine(reader);
				while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
				{
					if (response.StartsWith(MPDResponses.MPD_COMMAND))
					{
						commandName = response.Substring(MPDResponses.MPD_COMMAND.Length);
						commandList.Add(commandName);
					}
					response = ReadLine(reader);
				}
				//reader.Dispose();
			}
			Debug.WriteLine("Command list length: " + commandList.Count);
			return commandList;

		}

		/**
		 * Parses the response of MPDs supported tag types
		 *
		 * @return List of tags supported by the connected MPD host
		 * @throws IOException
		 */
		private List<String> ParseMPDTagTypes()
		{
			List<String> tagList = new List<string>();
			// Parse outputs
			String tagName = null;
			if (!IsConnected())
			{
				return tagList;
			}

			/* Response line from MPD */
			using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
			{
				String response = ReadLine(reader);
			while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
			{
				if (response.StartsWith(MPDResponses.MPD_TAGTYPE))
				{
					tagName = response.Substring(MPDResponses.MPD_TAGTYPE.Length);
					tagList.Add(tagName);
				}
				response = ReadLine(reader);
			}
			}
			//reader.Dispose();
			return tagList;

		}

		/**
		 * Returns the list of MPDOutputs to the outside callers.
		 *
		 * @return List of MPDOutput objects or null in case of error.
		 */
		public List<MPDOutput> GetOutputs()
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_GET_OUTPUTS);

				try
				{
					return ParseMPDOutputs();
				}
				catch (IOException)
				{
					HandleSocketError();
				}
				return null;
			}
		}

		/**
		 * Toggles the state of the output with the id.
		 *
		 * @param id Id of the output to toggle (active/deactive)
		 * @return True if server responed with ok
		 */
		public bool ToggleOutput(int id)
		{
			lock (_lock)
			{
				SendMPDCommand(MPDCommands.MPD_COMMAND_TOGGLE_OUTPUT(id));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException)
				{
					HandleSocketError();
				}
				return false;
			}
		}

		/**
		 * Instructs to update the database of the mpd server.
		 *
		 * @param path Path to update
		 * @return True if server responed with ok
		 */
		public bool UpdateDatabase(String path)
		{
			lock (_lock)
			{
				// Update root directory
				SendMPDCommand(MPDCommands.MPD_COMMAND_UPDATE_DATABASE(path));

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * This method should only be used by the idling mechanism.
		 * It buffers the read line so that the deidle method can check if deidling was successful.
		 * To guarantee predictable execution order, the buffer is secured by a semaphore. This ensures,
		 * that the read of this waiting thread is always finished before the other handler thread tries
		 * to read it.
		 *
		 * @return
		 */
		private String WaitForIdleResponse()
		{
			if (pSocket.Connected)
			{

				Debug.WriteLine("Waiting for input from server");
				// Set thread to sleep, because there should be no line available to read.
				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{
					String response = ReadLine(reader);

					return response;
				}
			}
			return "";
		}





		/**
		 * This will start the timeout to set the connection tto the idle state after use.
		 */
		private void StartIdleWait()
		{
			/**
			 * Check if a timer was running and then remove it.
			 * This will reset the timeout.
			 */
			if (null != mIdleWait)
			{
				idleWaitCancelTokenSource.Cancel();
				mIdleWait = null;
			}
			// Start the new timer with a new Idle Task.
			mIdleWait = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(IDLE_WAIT_TIME);
				StartIdleing();
			}, idleWaitCancelTokenSource.Token);

			Debug.WriteLine("IdleWait scheduled");
		}

		/**
		 * This will stop a potential running timeout task.
		 */
		private void StopIdleWait()
		{
			lock (_lock)
			{
				if (null != mIdleWait)
				{
					idleWaitCancelTokenSource.Cancel();
					mIdleWait = null;
				}
				Debug.WriteLine("IdleWait terminated");
			}
		}



		/**
		 * Central method to read a line from the sockets reader
		 *
		 * @return The read string. null if no data is available.
		 */
		private String ReadLine(StreamReader reader)
		{
			if (pSocket.Connected)
			{
				try
				{
					String line = reader.ReadLine();
					//Debug.WriteLine("Read line: " + line);
					return line;
				}
				catch (IOException)
				{
					HandleSocketError();
				}
			}
			return null;
		}

		/**
		 * Central method to write a line to the sockets writer. Socket will be flushed afterwards
		 * to ensure that the string is sent.
		 *
		 * @param line String to write to the socket.
		 */
		private void WriteLine(String line)
		{
			//TODO
			var writer = pSocket.GetStream();

			if (writer != null)
			{
				byte[] content = Encoding.UTF8.GetBytes($"{line} \n");
				writer.Write(content, 0, content.Length);
				writer.Flush();

				Debug.WriteLine("Write line: " + line);
			}
		}

		private void PrintDebug(String debug)
		{
			if (!DEBUG_ENABLED)
			{
				return;
			}

			//Log.v(TAG, mID + ':' + Thread.currentThread().getId() + ':' + "Idle:" + pMPDConnectionIdle + ':' + debug);
		}

		/**
		 * This is called if an parse list args error occurs during the parsing
		 * of {@link MPDAlbum} or {@link MPDArtist} objects. This probably indicates
		 * that this client is connected to Mopidy so we enable a workaround and reconnect
		 * to force the GUI to reload the contents.
		 */
		private void EnableMopidyWorkaround()
		{
			// Enable the workaround in the capabilities object
			mServerCapabilities.enableMopidyWorkaround();

			// Reconnect to server
			DisconnectFromServer();
			ConnectToServer();
		}

		/**
		 * M.Schleinkofer
		 * sends given string as raw command.
		 *
		 * @param rawCommand Command as String for sending to the server.
		 * @return True if server responded with ok
		 */
		public bool SendRaw(String rawCommand)
		{
			lock (_lock)
			{
				SendMPDCommand(rawCommand);

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		/**
		 * M.Schleinkofer
		 * returns the MPD Files with stickers searched before.
		 * @param filterCommand Command String which filters for Sticker; e.g. "sticker find song "mood" = "chill"";
		 * @return List of URL Strings of Files with stickers which were previously searched for
		 */
		public List<String> GetStickers(String filterCommand)
		{
			lock (_lock)
			{
				//throw new Resources.NotFoundException("Not implemented yet!");
				List<String> retList = new List<String>();

				if (!IsConnected())
				{
					return retList;
				}

				SendMPDCommand(filterCommand);


				if (pSocket == null)
				{
					return retList;
				}

				/* Response line from MPD */
				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{
					String responseFile = ReadLine(reader);
					while (IsConnected() && responseFile != null && !responseFile.StartsWith("OK") && !responseFile.StartsWith("ACK"))
					{
						if (responseFile.StartsWith(MPDResponses.MPD_RESPONSE_FILE))
						{
							String tmpTrack = responseFile.Substring(MPDResponses.MPD_RESPONSE_FILE.Length);
							if (null != tmpTrack)
							{
								retList.Add(tmpTrack);
							}
						}
						responseFile = ReadLine(reader);
					}
					Debug.WriteLine("Track List has " + retList.Count + " elements!");
				}
				//reader.Dispose();
				return retList;
			}

		}

		/** M.Schleinkofer
		 * This method adds songs in a bulk command list. Should be reasonably in performance this way.
		 *
		 * @param tracks List of URIs to set as the current playlist.
		 * @return True if server responded with ok
		 */
		public bool PlayTrackList(List<String> tracks)
		{
			lock (_lock)
			{
				if (null == tracks)
				{
					return false;
				}
				StartCommandList();

				foreach (String track in tracks)
				{
					if (track is String)
					{
						//using sendMPDRAWCommand because there will not be a response to check
						SendMPDRAWCommand(MPDCommands.MPD_COMMAND_ADD_FILE(track));
					}
				}


				EndCommandList();

				/* Return the response value of MPD */
				try
				{
					return CheckResponse();
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}

		public int GetRating(String trackUri)
		{
			lock (_lock)
			{
				int retVal = -1;
				if (!IsConnected())
				{
					return -1;
				}
				String cmd = "sticker get song \"" + trackUri + "\" \"rating\"";
				SendMPDCommand(cmd);

				using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
				{
					String response = ReadLine(reader);
					while (IsConnected() && response != null && !response.StartsWith("OK") && !response.StartsWith("ACK"))
					{
						if (response.StartsWith(MPDResponses.MPD_RESPONSE_STICKER))
						{
							String[] respParts = response.Split(new string[] { "[ =]" }, StringSplitOptions.None);
							if (3 <= respParts.Length)
							{
								retVal = int.Parse(respParts[2]);
							}
						}
						response = ReadLine(reader);
					}
				}
				//reader.Dispose();

				return retVal;
			}
		}

		public void IdleTask()
		{
			/* Try to read here. This should block this separate thread because
				 readLine() inside waitForIdleResponse is blocking.
				 If the response was not "OK" it means idling was stopped by us.
				 If the response starts with "changed" we know, that the MPD state was altered from somewhere
				 else and we need to update our status.
			 */
			lock (idleWaitLock)
			{
				bool externalDeIdle = false;

				// This will block this thread until the server has some data available to read again.
				String response = WaitForIdleResponse();

				Debug.WriteLine("Idle over with response: " + response);

				// This happens when disconnected
				if (null == response || response.Equals(""))
				{
					Debug.WriteLine("Probably disconnected during idling");
					// First handle the disconnect, then allow further action
					HandleSocketError();

					return;
				}
				/**
				 * At this position idling is over. Check if we were the reason to deidle or if the
				 * server initiated the deidling. If it was done by the server we will trigger
				 * the idle again.
				 */
				if (response.StartsWith("changed"))
				{
					Debug.WriteLine("Externally deidled!");
					externalDeIdle = true;
					using (StreamReader reader = new StreamReader(pSocket.GetStream(), Encoding.UTF8, true, 2048, true))
					{

						try
						{
							while (!reader.EndOfStream)
							{
								response = ReadLine(reader);
								if (response.StartsWith("OK"))
								{
									Debug.WriteLine("Deidled with status ok");
								}
								else if (response.StartsWith("ACK"))
								{
									Debug.WriteLine("Server response error: " + response);
								}
							}
						}
						catch (IOException)
						{
							HandleSocketError();
						}
					}
				}
				else
				{
					Debug.WriteLine("Deidled on purpose");
				}
				// Set the connection as non-idle again.
				pMPDConnectionIdle = false;

				/* TODO FIND OUT WHY
				// Reset the timeout again
				try
				{
					if (pSocket != null)
					{
						pSocket.setSoTimeout(SOCKET_TIMEOUT);
					}
				}
				catch (SocketException e)
				{
					handleSocketError();
				}
				*/
				// Notify a possible listener for deidling.
				OnDeidle?.Invoke();
				Debug.WriteLine("Idling over");

				// Start the idle clock again, but only if we were deidled from the server. Otherwise we let the
				// active command deidle when finished.
				if (externalDeIdle)
				{
					StartIdleWait();
				}
			}
		}
	}

}
