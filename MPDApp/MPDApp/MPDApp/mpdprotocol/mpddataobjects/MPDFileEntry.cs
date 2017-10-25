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

namespace MPDProtocol.MPDDataobjects
{

	public abstract class MPDFileEntry : MPDGenericItem, IComparable<MPDFileEntry>
	{
		public string Path { get; set; }
		public string LastModified { get; set; }

		protected MPDFileEntry(String path)
		{
			this.Path = path;
		}

		public override bool Equals(object obj)
		{

			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			var other = obj as MPDFileEntry;
			return Path.Equals(Path);
		}

		public override int GetHashCode()
		{
			return Path.GetHashCode();
		}

		/// <summary>
		/// Compare Hierarchy: Directory, Files, Playlist 
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(MPDFileEntry other)
		{
			if (other == null)
			{
				return -1;
			}

			if (this is MPDDirectory)
			{
				if (other is MPDDirectory)
				{
					return ((MPDDirectory)this).CompareTo((MPDDirectory)other);
				}
				else if (other is MPDPlaylist || other is MPDTrack)
				{
					return -1;
				}
			}
			else if (this is MPDTrack)
			{
				if (other is MPDDirectory)
				{
					return 1;
				}
				else if (other is MPDPlaylist)
				{
					return -1;
				}
				else if (other is MPDTrack)
				{
					return ((MPDTrack)this).CompareTo((MPDTrack)other);
				}
			}
			else if (this is MPDPlaylist)
			{
				if (other is MPDPlaylist)
				{
					return ((MPDPlaylist)this).CompareTo((MPDPlaylist)other);
				}
				else if (other is MPDDirectory || other is MPDTrack)
				{
					return 1;
				}
			}

			return -1;
		}

		public abstract string getSectionTitle();

		public static int CompareByFileIndex(MPDFileEntry o1, MPDFileEntry o2)
		{
			if (o1 == null && o2 == null)
			{
				return 0;
			}
			else if (o1 == null)
			{
				return 1;
			}
			else if (o2 == null)
			{
				return -1;
			}

			if (o1 is MPDDirectory)
			{
				if (o2 is MPDDirectory)
				{
					return ((MPDDirectory)o1).CompareTo((MPDDirectory)o2);
				}
				else if (o2 is MPDPlaylist || o2 is MPDTrack)
				{
					return -1;
				}
			}
			else if (o1 is MPDTrack)
			{
				if (o2 is MPDDirectory)
				{
					return 1;
				}
				else if (o2 is MPDPlaylist)
				{
					return -1;
				}
				else if (o2 is MPDTrack)
				{
					return ((MPDTrack)o1).IndexCompare((MPDTrack)o2);
				}
			}
			else if (o1 is MPDPlaylist)
			{
				if (o2 is MPDPlaylist)
				{
					return ((MPDPlaylist)o1).CompareTo((MPDPlaylist)o2);
				}
				else if (o2 is MPDDirectory || o2 is MPDTrack)
				{
					return 1;
				}
			}

			return -1;
		}
		
	}


}
