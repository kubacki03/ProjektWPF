using System.IO;
using System.Speech.Synthesis;
using OpenAI.Audio;
using OpenAI.Chat;

namespace ProjektWPF.Services
{
    public class AIService
    {
        private const string TranscriptionModel = "whisper-1";
        private const string ChatModel = "gpt-4o";

        private readonly string _apiKey;

        public AIService(string? apiKey = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPEN_AI_API_KEY")
                ?? throw new InvalidOperationException("Brak zmiennej środowiskowej OPEN_AI_API_KEY.");
        }

        public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
        {
            var client = new AudioClient(TranscriptionModel, _apiKey);

            await using var stream = File.OpenRead(audioFilePath);
            var response = await client.TranscribeAudioAsync(stream, Path.GetFileName(audioFilePath), cancellationToken: cancellationToken);

            return response.Value.Text;
        }

        public async Task<string> ChatAsync(string text, CancellationToken cancellationToken = default)
        {
            var client = new ChatClient(ChatModel, _apiKey);

            ChatCompletion completion = await client.CompleteChatAsync(
                new ChatMessage[] { ChatMessage.CreateUserMessage(text) },
                cancellationToken: cancellationToken);

            return completion.Content[0].Text;
        }

        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            using var synthesizer = new SpeechSynthesizer { Rate = 0 };
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            synthesizer.SpeakCompleted += (_, e) =>
            {
                if (e.Cancelled)
                {
                    completion.TrySetCanceled(cancellationToken);
                }
                else if (e.Error != null)
                {
                    completion.TrySetException(e.Error);
                }
                else
                {
                    completion.TrySetResult();
                }
            };

            using (cancellationToken.Register(() => synthesizer.SpeakAsyncCancelAll()))
            {
                synthesizer.SpeakAsync(text);
                await completion.Task;
            }
        }
    }
}
