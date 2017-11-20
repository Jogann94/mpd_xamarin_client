using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDProtocol.MPDDataobjects
{
	public class MPDArtist : MPDGenericItem, IComparable<MPDArtist>
	{
		public string ArtistName { get; set; }
		private bool imageFetching;

		private List<string> musicbrainzIDs;

		private object _lock = new object();

		public MPDArtist(String name)
		{
			ArtistName = name;
			musicbrainzIDs = new List<string>();
		}

		public int getMusicbrainzIDCount()
		{
			return musicbrainzIDs.Count;
		}

		public String getMusicbrainzID(int index)
		{
			return musicbrainzIDs[index];
		}

		public void addMusicbrainzID(String mbid)
		{
			musicbrainzIDs.Add(mbid);
		}

		public string getSectionTitle()
		{
			return ArtistName;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			MPDArtist other = obj as MPDArtist;
			if ((object)other == null)
			{
				return false;
			}

			if (!EqualNameAndIDCount(other))
			{
				return false;
			}


			return EqualIDs(other);
		}

		private bool EqualNameAndIDCount(MPDArtist other)
		{
			return ArtistName.Equals(other.ArtistName) && musicbrainzIDs.Count != other.musicbrainzIDs.Count;
		}

		private bool EqualIDs(MPDArtist other)
		{
			for (int i = 0; i < musicbrainzIDs.Count; i++)
			{
				if (!musicbrainzIDs[i].Equals(other.musicbrainzIDs[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
			int hash = 13;
			hash = (hash * 7) + (!Object.ReferenceEquals(null, ArtistName) ? ArtistName.GetHashCode() : 0);
			hash = (hash * 7) + (!Object.ReferenceEquals(null, musicbrainzIDs) ? musicbrainzIDs.GetHashCode() : 0);

			foreach (var id in musicbrainzIDs)
			{
				hash = (hash * 7) + (!Object.ReferenceEquals(null, id) ? id.GetHashCode() : 0);
			}

			return hash;
		}
		public int CompareTo(MPDArtist other)
		{
			if (other.Equals(this))
			{
				return 0;
			}

			if (other.ArtistName.ToLower().Equals(ArtistName.ToLower()))
			{
				if ((other.musicbrainzIDs.Count > musicbrainzIDs.Count) || other.musicbrainzIDs.Count == 1)
				{
					return -1;
				}
				else if ((other.musicbrainzIDs.Count < musicbrainzIDs.Count) || musicbrainzIDs.Count == 1)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}

			return String.Compare(ArtistName.ToLower(), other.ArtistName.ToLower());
		}

		public void setFetching(bool fetching)
		{
			lock (_lock)
			{
				imageFetching = fetching;
			}
		}

		public bool getFetching()
		{
			lock (_lock)
			{
				return imageFetching;
			}
		}

		/*
		@Override
		public int describeContents()
		{
				return 0;
		}
		*/
	}
}
