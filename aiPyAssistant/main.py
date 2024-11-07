import google.generativeai as ai
import speech_recognition as sr
from gtts import gTTS
import os
recognizer = sr.Recognizer()
text = ""
print("Go!")
with sr.Microphone(0) as source:
    try:
        text = recognizer.recognize_google(recognizer.listen(source), language='en-US')
    except sr.UnknownValueError:
        pass
    except sr.RequestError:
        pass
ai.configure(api_key="AIzaSyCj1J9baxohYHJ3n_wStq8c8QC8OEeX8HI")
model = ai.GenerativeModel(model_name="gemini-1.5-flash")
gTTS(model.generate_content(text).text, lang='en', slow=False).save('x.wav')
os.system('ffplay.exe -nodisp -autoexit x.wav')
