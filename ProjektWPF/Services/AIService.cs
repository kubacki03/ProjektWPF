using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using OpenAI.Audio;
using OpenAI.Chat;
using ProjektWPF.Models;
using ProjektWPF.Models.ProjektWPF.Models;

namespace ProjektWPF.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za integrację z AI, obsługujący konwersję mowy na tekst,
    /// generowanie odpowiedzi przez model językowy oraz syntezę mowy
    /// </summary>
    public class AIService
    {
        /// <summary>
        /// Konwertuje nagrany plik audio na tekst, generuje odpowiedź przez AI
        /// i odtwarza odpowiedź syntetyzowanym głosem
        /// </summary>
        /// <returns>Słownik zawierający wpisy użytkownika i odpowiedzi bota</returns>
        public async Task<Dictionary<string, string>> SpeechToText()
        {
            // Konfiguracja opcji transkrypcji audio
            var audioOptions = new AudioTranscriptionOptions()
            {
                ResponseFormat = AudioTranscriptionFormat.Srt // Format napisów
            };

            // Inicjalizacja klienta Whisper do transkrypcji mowy
            var audioClient = new AudioClient(
                "whisper-1", // Model Whisper do konwersji mowy na tekst
                Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") // Klucz API z zmiennych środowiskowych
            );

            // Wykonanie transkrypcji nagranego pliku audio
            var response = await audioClient.TranscribeAudioAsync("recorded_audio.wav", audioOptions);

            // Inicjalizacja syntezatora mowy
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                // Konfiguracja parametrów syntezy mowy
                synthesizer.Rate = 0;  // Prędkość mówienia (0 - domyślna)

                // Pobranie transkrybowanego tekstu z odpowiedzi
                string text = response.Value.Text;

                // Generowanie odpowiedzi przez model językowy
                string answer = await SimpleChat(text);

                // Odtwarzanie wygenerowanej odpowiedzi
                synthesizer.Speak(answer);

                // Przygotowanie wyniku w formie słownika
                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("User", text);   // Tekst użytkownika
                result.Add("Bot", answer);   // Odpowiedź bota

                return result;
            }
        }

        /// <summary>
        /// Wysyła zapytanie do modelu językowego GPT-4 i zwraca wygenerowaną odpowiedź
        /// </summary>
        /// <param name="text">Tekst wejściowy użytkownika</param>
        /// <returns>Wygenerowana odpowiedź tekstowa</returns>
        public async Task<string> SimpleChat(string text)
        {
            // Inicjalizacja klienta czatu z modelem GPT-4
            ChatClient client = new(
                model: "gpt-4o", // Używany model językowy
                apiKey: Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") // Klucz API
            );

            // Generowanie odpowiedzi na podstawie tekstu wejściowego
            ChatCompletion completion = client.CompleteChat(text);

            // Zwracanie pierwszej wygenerowanej odpowiedzi
            return completion.Content[0].Text;
        }
    }
}