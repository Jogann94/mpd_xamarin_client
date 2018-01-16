using System.Text;
using Xamarin.Forms;
using MPDApp.Controls;
using MPDApp.UWP.CustomRenderer;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;

[assembly: ExportRenderer(typeof(Toggle), typeof(ToggleRenderer))]
namespace MPDApp.UWP.CustomRenderer
{
	public class ToggleRenderer : ViewRenderer<Toggle, RadioButton>
	{
		private RadioButton radioButton;

		protected override void OnElementChanged(ElementChangedEventArgs<Toggle> e)
		{
			base.OnElementChanged(e);

			if (Control == null)
			{
				radioButton = new RadioButton();
				
				SetNativeControl(radioButton);
			}
			if (e.OldElement != null)
			{
			}
			if (e.NewElement != null)
			{
				Control.IsChecked = e.NewElement.IsChecked;
				Control.IsEnabled = e.NewElement.IsClickable;
			}
		}

		private void TogglePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsChecked")
			{
				Control.IsChecked = Element.IsChecked;
			}
		}

	}
}