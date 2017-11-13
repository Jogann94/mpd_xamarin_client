using System; 

namespace MPDApp.DependencyServices
{
	public interface ISpeechHelper
	{
		void RecordSpeachToText();
		void TextToSpeach(string text);

		event Action<string> Recorded;
	}
}

