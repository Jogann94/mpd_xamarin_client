using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace MPDApp.Controls
{
	public class ImageButton : Image
	{

		public static readonly BindableProperty CommandProperty = 
			BindableProperty.Create("Command", typeof(object), typeof(ImageButton), null);

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public static readonly BindableProperty CommandParameterProperty = 
			BindableProperty.Create("CommandParameter", typeof(object), typeof(ImageButton), null);

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		private ICommand TransitionCommand
		{
			get
			{
				return new Command(async () =>
				{
					this.AnchorX = 0.48;
					this.AnchorY = 0.48;
					await this.ScaleTo(0.8, 50, Easing.Linear);
					await Task.Delay(100);
					await this.ScaleTo(1, 50, Easing.Linear);
					if (Command != null)
					{
						Command.Execute(CommandParameter);
					}
				});
			}
		}

		public ImageButton()
		{
			Initialize();
		}


		public void Initialize()
		{
			GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = TransitionCommand
			});
		}

	}
}
