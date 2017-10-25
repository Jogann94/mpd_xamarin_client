using System;
using MPDProtocol;
namespace MPDProtocol.MPDDataobjects
{
	public class MPDAlbum : IComparable<MPDAlbum>
	{
		public string name { get; private set; }

		private string _musicbrainzID;
		public string musicbrainzID
		{
			get { return _musicbrainzID; }
			set { if (value != null) _musicbrainzID = value; }
		}

		private string _artistName;
		public string artistName
		{
			get { return _artistName; }
			set { if (value != null) _artistName = value; }
		}

		private DateTime _date;
		public DateTime date
		{
			get { return _date; }
			set { if (value != null) _date = value; }
		}

		private object _lock = new object();
		private bool imageFetching;

		public MPDAlbum(String name)
		{
			this.name = name;
			_musicbrainzID = "";
			_artistName = "";
			_date = new DateTime();
		}

		/* Getters */
		/*
		protected MPDAlbum(Parcel in) {
				name = in.readString();
				musicbrainzID = in.readString();
				artistName = in.readString();
				imageFetching = in.readByte() != 0;
				date = (Date)in.readSerializable();
		}

		public static final Creator<MPDAlbum> CREATOR = new Creator<MPDAlbum>() {
				@Override
				public MPDAlbum createFromParcel(Parcel in) {
						return new MPDAlbum(in);
				}

				@Override
				public MPDAlbum[] newArray(int size) {
						return new MPDAlbum[size];
				}
		};
		*/

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			MPDAlbum album = obj as MPDAlbum;
			if ((object)album == null)
			{
				return false;
			}

			bool result = name.Equals(album.name)
										&& artistName.Equals(artistName)
										&& _musicbrainzID.Equals(album._musicbrainzID)
										&& _date.Equals(album._date);

			return result;
		}

		public override int GetHashCode()
		{
			int hash = 13;
			hash = (hash * 7) + (!Object.ReferenceEquals(null, name) ? name.GetHashCode() : 0);
			hash = (hash * 7) + (!Object.ReferenceEquals(null, artistName) ? artistName.GetHashCode() : 0);
			hash = (hash * 7) + (!Object.ReferenceEquals(null, _musicbrainzID) ? _musicbrainzID.GetHashCode() : 0);
			hash = (hash * 7) + (!Object.ReferenceEquals(null, _date) ? _date.GetHashCode() : 0);
			return hash;
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
		public void writeToParcel(Parcel dest, int flags) {
				dest.writeString(mName);
				dest.writeString(mMBID);
				dest.writeString(mArtistName);
				dest.writeByte((byte) (mImageFetching ? 1 : 0));
				dest.writeSerializable(mDate);
		}
		*/

		public int CompareTo(MPDAlbum other)
		{
			if (other.Equals(this))
			{
				return 0;
			}

			return string.Compare(name.ToLower(), other.name.ToLower());
		}

	}
}