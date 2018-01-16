using System;
using System.IO;
using MPDApp.DependencyServices;
using MPDApp.Droid;
using Xamarin.Forms;
using Windows.Storage;

[assembly: Dependency(typeof(FileHelper))]
namespace MPDApp.Droid
{
	public class FileHelper : IFileHelper
	{
		public string GetLocalFilePath(string filename)
		{
			return Path.Combine(ApplicationData.Current.LocalFolder.Path, filename);
		}
	}
}