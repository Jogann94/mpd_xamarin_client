using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ServerOutputPage : ContentPage
	{
		private List<MPDOutput> outputList;

		public List<MPDOutput> OutputList
		{
			get { return outputList; }
			set { outputList = value; OnPropertyChanged("OutputList"); }
		}

		public ServerOutputPage()
		{
			InitializeComponent();
			BindingContext = this;

			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				Task.Factory.StartNew(() =>
				{
					var outputs = con.GetOutputs();
					Device.BeginInvokeOnMainThread(() => { OutputList = outputs; });
				});
			}

		}

		public async void OutputItem_Tapped(object sender, ItemTappedEventArgs e)
		{
			var con = MPDConnection.GetInstance();
		
			if (con.IsConnected())
			{
				OutputList = await Task.Factory.StartNew(() =>
				{
					if (e.Item is MPDOutput output)
					{
						con.ToggleOutput(output.OutputId);
					}
				}).ContinueWith((t) => { return con.GetOutputs(); }); 
			}
		}
	}
}