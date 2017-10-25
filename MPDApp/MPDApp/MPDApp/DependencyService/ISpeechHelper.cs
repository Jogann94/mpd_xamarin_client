using System; 

namespace MPDApp.Speech
{
	public interface ISpeechHelper
	{
		void RecordSpeachToText();
		void TextToSpeach(string text);

		event Action<string> Recorded;
	}
}

