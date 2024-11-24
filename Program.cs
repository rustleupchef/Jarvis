using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Python.Runtime;

namespace Jarvis
{
	class Program
	{
		private static string[] batchs = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Commands"));
		private static bool repeat = true;
		private static bool Continue = true;
		private static dynamic recognizer;
		private static dynamic source;
		private static ChatHistory chatHistory = new();
		private static IChatCompletionService chatService;
		private static bool isSpeaking = false;

		#pragma warning disable SKEXP0070
		internal static void Main()
		{
			Runtime.PythonDLL = "C:\\Users\\ravin\\AppData\\Local\\Programs\\Python\\Python311\\python311.dll";
			PythonEngine.Initialize();
			using (Py.GIL())
			{
				dynamic sr = Py.Import("speech_recognition");
				recognizer = sr.Recognizer();
				source = sr.Microphone(0).__enter__();
				Kernel kernel = Kernel.CreateBuilder().AddOllamaChatCompletion(
					model: "granite-code",
					endpoint: new Uri("http://localhost:11434")).Build();
				chatService = kernel.GetRequiredService<IChatCompletionService>();

				using (SpeechRecognitionEngine speechRecognition = new())
				{
					GrammarBuilder grammarBuilder = new("Jarvis");
					Choices choices = new();
					choices.Add(new string[] { "exit one hundred percent", "can we talk", "start", "stop", "shut up" });
					foreach (string batch in batchs)
					{
						choices.Add(batch.Split('\\')[^1].Split('.')[0]);
					}
					grammarBuilder.Append(choices);
					speechRecognition.SetInputToDefaultAudioDevice();
					speechRecognition.LoadGrammar(new Grammar(grammarBuilder));
					speechRecognition.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Listen);
					while (repeat) { speechRecognition.Recognize(); }
				}
			}
			PythonEngine.Shutdown();
		}

		private static void Listen(object sender, SpeechRecognizedEventArgs e)
		{
			if (!isSpeaking)
			{
				using (SpeechSynthesizer speech = new())
				{
					string text = e.Result.Text.Split("Jarvis ")[1];
					Console.WriteLine(text);
					if (text == "start")
					{
						if (!Continue)
						{
							Continue = true;
							speech.Speak("Yes sir I will start");
						}
						return;
					}
					else if ((text == "stop" || text == "shut up") && Continue)
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

						if (text == "can we talk")
						{
							isSpeaking = true;
							using (Py.GIL())
							{
								while(true)
								{
									Console.WriteLine("Adjusting for ambient audio...");
									recognizer.adjust_for_ambient_noise(source: source, duration: 0.2);
									Console.WriteLine("Recording");
									dynamic result = recognizer.recognize_whisper(recognizer.listen(source, 5, 30));
									string input = (string) result;
									input = input.ToLower();
									Console.WriteLine($"Recorded: {input}");
									chatHistory.Add(new ChatMessageContent(AuthorRole.User, input));
									string response = string.Empty;
									foreach (var stream in chatService.GetChatMessageContentsAsync(chatHistory).Result)
									{
										speech.Speak(stream.Content);
										Console.WriteLine(stream.Content);
										response += stream.Content;
									}
									if (input.Contains("bye") || input.Contains("stop")) break;
									chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));
								}
							}
							isSpeaking = false;
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
}