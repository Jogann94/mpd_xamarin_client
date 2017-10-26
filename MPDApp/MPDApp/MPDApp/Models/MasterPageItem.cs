using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MPDApp.Models
{
	public class MasterPageItem
	{
		public string Title { get; set; }
		public string Icon { get; set; }
		public Type TargetType { get; set; }
		public Color BackgroundColor { get; set; }
		public Color TextColor { get; set; }
		public string IconNameWithoutColor { get; set; }

		private static Color selectedBackgroundColor = Color.FromHex("#292c30");
		private static Color unselectedBackgroundColor = Color.FromHex("#191e1c");
		private static Color selectedTextColor = Color.FromHex("#0288D1");
		private static Color unselectedTextColor = Color.White;

		public MasterPageItem(string title, string iconName, Type targetType, bool selected)
		{
			Title = title;
			TargetType = targetType;
			IconNameWithoutColor = iconName;

			if(selected)
			{
				Icon = iconName + "_blue.png";
				BackgroundColor = selectedBackgroundColor;
				TextColor = selectedTextColor;
			}
			else
			{
				Icon = iconName + "_white.png";
				BackgroundColor = unselectedBackgroundColor;
				TextColor = unselectedTextColor;
			}
		}
	}
}
