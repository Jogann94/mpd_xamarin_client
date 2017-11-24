using Xamarin.Forms;

namespace MPDApp.Controls
{
	public class Toggle : View
	{
		
		public static readonly BindableProperty IsCheckedProperty =
			BindableProperty.Create("IsActive", typeof(bool), typeof(Toggle), false);

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}
		
	}
}