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
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using SQLite;
using Xamarin.Forms;

namespace MPDApp.ProfileManagement
{

	public class MPDServerProfile : MPDGenericItem
	{
		[PrimaryKey, AutoIncrement]
		public int ID { get; set; }

		[NotNull]
		public string ProfileName { get; set; }

		public bool IsActiveProfile { get; set; }

		[NotNull]
		public string Hostname { get; set; }
		[NotNull]
		public string Password { get; set; }

		public int Port { get; set; }

		public string StreamingURL { get; set; }
		public bool StreamingEnabled { get; set; }

		//M.Schleinkofer 03.06.2017
		public string ratingHostname { get; set; }
		public int ratingPort { get; set; }
		public bool RatingEnabled { get; set; }

		public long created;

		[Ignore]
		public Color LabelColor
		{
			get
			{
				if(IsActiveProfile)
				{
					return Color.FromHex("#0288D1");
				}
				else
				{
					return Color.White;
				}
			}
		}

		public MPDServerProfile() { }

		public MPDServerProfile(string profileName, bool autoConnect)
		{
			this.ProfileName = profileName;
			IsActiveProfile = autoConnect;

			created = (long)(DateTime.Now - DateTime.MinValue).TotalMilliseconds;

			/* Just set the default mpd port here */
			Port = 6600;
			ratingPort = 6603;
		}

		public MPDServerProfile(string profileName, bool autoConnect, long creationDate)
		{
			this.ProfileName = profileName;
			IsActiveProfile = autoConnect;

			created = (long)(DateTime.Now - DateTime.MinValue).TotalMilliseconds;

			/* Just set the default mpd port here */
			Port = 6600;
			created = creationDate;
			ratingPort = 6603;
		}

		public MPDServerProfile Copy()
		{
			return MemberwiseClone() as MPDServerProfile;
		}

		public override string ToString()
		{
			string retstring = "";

			retstring += "Profilename: " + ProfileName + "\n";
			retstring += "Profile autoconnect: " + IsActiveProfile + "\n";
			retstring += "Hostname: " + Hostname + "\n";
			retstring += "Password: " + Password + "\n";
			retstring += "Port: " + Port + "\n";
			retstring += "Created: " + created + "\n";
			
			return retstring;
		}

		public override bool Equals(object obj)
		{
			var other = obj as MPDServerProfile;
			if (other == null)
			{
				return false;
			}

			return ProfileName.Equals(other.ProfileName)
						 && Hostname.Equals(other.Hostname);
		}

		public override int GetHashCode()
		{
			int hash = 13;
			hash = (hash * 7) + (!Object.ReferenceEquals(null, ProfileName) ? ProfileName.GetHashCode() : 0);
			hash = (hash * 7) + (!Object.ReferenceEquals(null, Hostname) ? Hostname.GetHashCode() : 0);

			return hash;
		}

		public string getSectionTitle()
		{
			return ProfileName;
		}

		public int describeContents()
		{
			return 0;
		}
	}
}