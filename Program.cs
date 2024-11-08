using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace Jarvis
{
	class Program
	{
		public static string[] batchs = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Commands"));
		public static bool repeat = true;
		public static bool Continue = true;
		private static void Main()
		{
			using (SpeechRecognitionEngine speechRecognition = new())
			{

				GrammarBuilder grammarBuilder = new("Jarvis");
				Choices choices = new();
				choices.Add(new string[] {"exit one hundred percent", "start", "stop", "shut up"});
				foreach (string batch in batchs)
				{
					choices.Add(batch.Split('\\')[^1].Split('.')[0]);
				}
				grammarBuilder.Append(choices);
				speechRecognition.SetInputToDefaultAudioDevice();
				speechRecognition.LoadGrammar(new Grammar(grammarBuilder));
				speechRecognition.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Listen);
				while(repeat){speechRecognition.Recognize();}
			}
		}

		private static void Listen(object sender, SpeechRecognizedEventArgs e)
		{
			using (SpeechSynthesizer speech = new())
			{
				string text = e.Result.Text.Split("Jarvis ")[1];
				Console.WriteLine(text);
				if(text == "start" && !Continue)
				{
					Continue = true;
					speech.Speak("Yes sir I will start");
					return;
				}
				else if(text == "start")
				{
					return;
				}
				else if((text == "stop"|| text == "shut up") && Continue)
				{
					Continue = false;
					speech.Speak((new Random().Next(1, 5) == 2) ? "Yes sir I will " + text : "Would you ever consider not being so rude sir?");
					return;
				}

				if (Continue)
				{
					if (text == "exit one hundred percent")
					{
						repeat = false;
						speech.Speak("Yes sir I will shutdown");
						return;
					}

					foreach (string batch in batchs)
					{
						if (text == batch.Split('\\')[^1].Split('.')[0])
						{
							Process.Start(new ProcessStartInfo(batch) { UseShellExecute = false });
							speech.Speak("Yes sir I will run" + text + "dot bat");
							return;
						}
					}
				}
			}
		}
	}
}
