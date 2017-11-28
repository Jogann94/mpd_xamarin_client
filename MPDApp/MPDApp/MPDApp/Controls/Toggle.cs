using Xamarin.Forms;

namespace MPDApp.Controls
{
	public class Toggle : View
	{
		
		public static readonly BindableProperty IsCheckedProperty =
			BindableProperty.Create("IsActive", typeof(bool), typeof(Toggle), false);

		public static readonly BindableProperty IsClickableProperty =
			BindableProperty.Create("IsClickable", typeof(bool), typeof(Toggle), true);

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public bool IsClickable
		{
			get { return (bool)GetValue(IsClickableProperty); }
			set { SetValue(IsClickableProperty, value); }
		}
	}
}