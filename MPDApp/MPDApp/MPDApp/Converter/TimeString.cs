using System;
using System.Collections.Generic;
using System.Text;

namespace MPDApp.Converter
{
	public class TimeString
	{
		public static string GenerateFromSeconds(int seconds)
		{
			int remainingSeconds = seconds % 60;
			int minutes = (seconds / 60) % 60;
			int hours = (seconds / 60 / 60);
			if (hours > 0)
			{
				return String.Format("{0:00}:{1:00}:{2:00}", hours, minutes, remainingSeconds);
			}
			else
			{
				return String.Format("{0:00}:{1:00}", minutes, remainingSeconds);
			}
		}
	}
}
