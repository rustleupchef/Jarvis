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
		private static bool repeat = true;
		private static bool Continue = true;
		private static dynamic recognizer;
		private static dynamic source;
		private static ChatHistory chatHistory = new();
		private static IChatCompletionService chatService;
		private static dynamic model;
		private static bool isSpeaking = false;
		private static bool isOllama = true;
		private static Kernel kernel;

		#pragma warning disable SKEXP0070
		internal static void Main(string[] args)
		{
			Runtime.PythonDLL = "C:\\Users\\ravin\\AppData\\Local\\Programs\\Python\\Python311\\python311.dll";
			PythonEngine.Initialize();
			using (Py.GIL())
			{
				//Setting up speech recognition variables
				dynamic sr = Py.Import("speech_recognition");
				recognizer = sr.Recognizer();
				source = sr.Microphone(0).__enter__();

				// setting up gemini related variables
				if (args.Length > 0 && args[0] == "gemini")
				{
					isOllama = false;
					dynamic genai = Py.Import("google.generativeai");
					genai.configure(api_key: API.Key);
					model = genai.GenerativeModel(model_name: "gemini-1.5-flash");
				}

				Console.WriteLine(isOllama ? "Using Ollama" : "Using Gemini");

				//running first test, so that theirs no delay when you actually start using it
				Console.WriteLine("Setting Up");
				Console.WriteLine("Adjusting for ambient audio...");
				recognizer.adjust_for_ambient_noise(source: source, duration: 0.2);
				Console.WriteLine("Say something random to test");
				dynamic result = recognizer.recognize_whisper(recognizer.listen(source, 5, 2));
				string input = (string)result;
				input = input.ToLower();
				Console.WriteLine($"Recorded: {input}");
				if (isOllama)
				{
					kernel = Kernel.CreateBuilder().AddOllamaChatCompletion(
					model: "smollm",
					endpoint: new Uri("http://localhost:11434")).Build();
					chatService = kernel.GetRequiredService<IChatCompletionService>();
					chatHistory.Add(new ChatMessageContent(AuthorRole.User, input));
					string response = string.Empty;
					foreach (var stream in chatService.GetChatMessageContentsAsync(chatHistory, kernel: kernel).Result)
					{
						Console.WriteLine(stream.Content);
						response += stream.Content;
					}
					chatHistory.Clear();
				}
				else
				{
					dynamic response = model.generate_content(contents: input, stream: true);
					foreach (dynamic chunk in response)
					{
						string output = (string)chunk.text;
						Console.WriteLine(output);
					}
				}

				// constantly listenting to all keywords
				using (SpeechRecognitionEngine speechRecognition = new())
				{
					GrammarBuilder grammarBuilder = new("Jarvis");
					Choices choices = new();
					choices.Add(new string[] { "exit one hundred percent", "get out", "can we talk", "start", "stop", "shut up" });
					string[] batchs = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Commands"));
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
			// check if the ai is speaking so it doesn't interrupt it
			if (isSpeaking) return;
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

				if (!Continue) return;
				switch (text)
				{
					case "exit one hundred percent":
						repeat = false;
						speech.Speak("Yes sir I will shutdown");
						return;
					case "get out":
						repeat = false;
						speech.Speak("Yes sir I will shutdown");
						return;
					case "can we talk":
						speech.Speak("Sure we can. What do you want to talk about?");
						isSpeaking = true;
						using (Py.GIL())
						{
							while (true)
							{
								Console.WriteLine("Adjusting for ambient audio...");
								recognizer.adjust_for_ambient_noise(source: source, duration: 0.2);
								Console.WriteLine("Recording");
								dynamic result = recognizer.recognize_whisper(recognizer.listen(source, 5, 30));
								string input = (string)result;
								input = input.ToLower();
								Console.WriteLine($"Recorded: {input}");
								if (isOllama)
								{
									chatHistory.Add(new ChatMessageContent(AuthorRole.User, input));
									string response = string.Empty;
									foreach (var stream in chatService.GetChatMessageContentsAsync(chatHistory, kernel: kernel).Result)
									{
										Console.WriteLine(stream.Content);
										speech.Speak(stream.Content);
										response += stream.Content;
									}
									chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));
								}
								else
								{
									dynamic response = model.generate_content(contents: input, stream: true);
									foreach (dynamic chunk in response)
									{
										string output = (string)chunk.text;
										Console.WriteLine(output);
										speech.Speak(output);
									}
								}
								if (input.Contains("bye") || input.Contains("stop")) break;

							}
						}
						isSpeaking = false;
						return;
					default:
						// making sure file wasn't deleted during runtime
						Process.Start(new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), "Commands", $"{text}.bat")) { UseShellExecute = false});
						speech.Speak("Yes sir I will run" + text + "dot bat");
						return;
				}
			}
		}
	}
}