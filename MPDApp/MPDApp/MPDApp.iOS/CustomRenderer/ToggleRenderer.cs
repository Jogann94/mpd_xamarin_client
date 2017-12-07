using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using System.ComponentModel;
using MPDApp.Controls;
using MPDApp.iOS.CustomRenderer;
using UIKit;

[assembly: ExportRenderer(typeof(Toggle), typeof(ToggleRenderer))]
namespace MPDApp.iOS.CustomRenderer
{
	public class ToggleRenderer : ViewRenderer<Toggle, UISwitch>
	{
		private UISwitch radioSwitch;

		protected override void OnElementChanged(ElementChangedEventArgs<Toggle> e)
		{
			base.OnElementChanged(e);

			if (Control == null)
			{
				radioSwitch = new UISwitch();
				SetNativeControl(radioSwitch);
			}
			if (e.OldElement != null)
			{
			}
			if (e.NewElement != null)
			{
				Control.On = e.NewElement.IsChecked;
				Control.Enabled = e.NewElement.IsClickable;
			}
		}
	}
}
