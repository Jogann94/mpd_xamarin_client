using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using MPDApp.Controls;
using MPDApp.Droid.CustomRenderer;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;

[assembly: ExportRenderer(typeof(Toggle), typeof(ToggleRenderer))]
namespace MPDApp.Droid.CustomRenderer
{
	public class ToggleRenderer : ViewRenderer<Toggle, RadioButton>
	{
		private RadioButton radioButton;
		
		public ToggleRenderer(Context context) : base(context)
		{	}

		protected override void OnElementChanged(ElementChangedEventArgs<Toggle> e)
		{
			base.OnElementChanged(e);

			if (Control == null)
			{
				radioButton = new RadioButton(Context);
				SetNativeControl(radioButton);
			}
			if(e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= TogglePropertyChanged;
			}
			if (e.NewElement != null)
			{
				e.NewElement.PropertyChanged -= TogglePropertyChanged;
				Control.Checked = e.NewElement.IsChecked;
				Control.Clickable = e.NewElement.IsClickable;
			}
		}

		private void TogglePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsChecked")
			{
				Control.Checked = Element.IsChecked;
			}
		}


	}
}