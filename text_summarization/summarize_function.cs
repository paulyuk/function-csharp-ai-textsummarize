using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AI_Functions
{
    public class summarize_function
    {
        private readonly ILogger _logger;

        // must export and set these Env vars with your AI Cognitive Language resource values
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(Environment.GetEnvironmentVariable("AI_SECRET") ?? "SETENVVAR!");
        private static readonly Uri endpoint = new Uri(Environment.GetEnvironmentVariable("AI_URL") ?? "SETENVVAR!");

        public summarize_function(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<summarize_function>();
        }

        [Function("summarize_function")]
        [BlobOutput("test-samples-output/{name}-output.txt")]
        public static async Task<string> Run(
            [BlobTrigger("test-samples-trigger/{name}")] string myTriggerItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("summarize_function");
            logger.LogInformation($"Triggered Item = {myTriggerItem}");

            var client = new TextAnalyticsClient(endpoint, credentials);

            // analyze document text using Azure Cognitive Language Services
            var summarizedText = await AISummarizeText(client, myTriggerItem, logger);
            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);

            // Blob Output
            return summarizedText;
        }
        static async Task<string> AISummarizeText(TextAnalyticsClient client, string document, ILogger logger)
        {

            string summarizedText = "";

            // Prepare analyze operation input. You can add multiple documents to this list and perform the same
            // operation to all of them.
            var batchInput = new List<string>
            {
                document
            };

            TextAnalyticsActions actions = new TextAnalyticsActions()
            {
                ExtractiveSummarizeActions = new List<ExtractiveSummarizeAction>() { new ExtractiveSummarizeAction() }
            };

            // Start analysis process.
            ExtractiveSummarizeOperation operation = client.ExtractiveSummarize(WaitUntil.Completed, batchInput);
        
            // View operation status.
            summarizedText += $"AnalyzeActions operation has completed" + Newline();
            summarizedText += $"Created On   : {operation.CreatedOn}" + Newline();
            summarizedText += $"Expires On   : {operation.ExpiresOn}" + Newline();
            summarizedText += $"Id           : {operation.Id}" + Newline();
            summarizedText += $"Status       : {operation.Status}" + Newline();

            // View operation results.
            await foreach (ExtractiveSummarizeResultCollection documentsInPage in operation.Value)
            {
                Console.WriteLine($"Extractive Summarize, version: \"{documentsInPage.ModelVersion}\"");
                Console.WriteLine();

                foreach (ExtractiveSummarizeResult documentResult in documentsInPage)
                {
                    if (documentResult.HasError)
                    {
                        Console.WriteLine($"  Error!");
                        Console.WriteLine($"  Document error code: {documentResult.Error.ErrorCode}");
                        Console.WriteLine($"  Message: {documentResult.Error.Message}");
                        continue;
                    }

                    Console.WriteLine($"  Extracted {documentResult.Sentences.Count} sentence(s):");
                    Console.WriteLine();

                    foreach (ExtractiveSummarySentence sentence in documentResult.Sentences)
                    {
                        Console.WriteLine($"  Sentence: {sentence.Text}");
                        Console.WriteLine($"  Rank Score: {sentence.RankScore}");
                        Console.WriteLine($"  Offset: {sentence.Offset}");
                        Console.WriteLine($"  Length: {sentence.Length}");
                        Console.WriteLine();

                        summarizedText += $"  Sentence: {sentence.Text}" + Newline();
                    }
                }
            }

            logger.LogInformation(Newline() + "*****Summary*****" + Newline() + summarizedText);
            return summarizedText;
        }

        static string Newline()
        {
            return "\r\n";
        }

    }


}
