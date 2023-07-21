using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.ResponseModels.ModelResponseModels;
using System.Net.NetworkInformation;

namespace Transcriber
{
    internal class Program
    {
        const string Address = "openai.com";

        static async Task Main()
        {
            while (true)
            {
                await MainAsync();

                WriteInfo("\n\nDo you want to run again(Y/n)?: ");
                string? choice = Console.ReadLine();

                if (choice?.ToLower() == "n")
                {
                    break;
                }
                else
                {
                    Console.Clear();
                }
            }
        }

        static async Task MainAsync()
        {
            if (!CheckOpenAIConnection())
            {
                return;
            }

            OpenAIService openAIService = await FetchAPI();

            List<FileInfo> files = FetchFiles();

            WriteInfo("\nContinue(Y/n)?: ");

            string? answer = Console.ReadLine();

            if (answer?.ToLower() == "n")
            {
                return;
            }

            await TranscribeAll(files, openAIService);
        }

        static bool CheckOpenAIConnection()
        {
            WriteInfo("Checking connection to openai.com...");

            try
            {
                using Ping ping = new();

                const int Timeout = 3000;

                PingReply openAIReply = ping.Send(Address, Timeout);

                if (openAIReply.Status == IPStatus.Success)
                {
                    WriteSuccess("Connection OK");
                    return true;
                }
                else
                {
                    WriteError("Connection FAIL");
                    return false;
                }
            }
            catch
            {
                WriteError($"Connection FAIL: {ex.Message}");
                return false;
            }
        }

        static async Task TranscribeAll(List<FileInfo> files, OpenAIService openAIService)
        {
            WriteInfo("Transcribing files...");

            foreach (FileInfo file in files)
            {
                await Transcribe(file, openAIService);
            }

            WriteSuccess("Done transcribing all the files!\n");
        }

        static async Task Transcribe(FileInfo file, OpenAIService openAIService)
        {
            string fileName = file.Name;

            WriteInfo($"Fetching {file.Name}...");

            Stream fileStream = new FileStream(file.FullName, FileMode.Open);

            WriteSuccess("Done fetching!\n");

            WriteInfo($"Transcribing {file.Name}...");

            AudioCreateTranscriptionResponse audioResult = await openAIService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest()
            {
                FileName = fileName,
                FileStream = fileStream,
                Model = Models.WhisperV1,
                ResponseFormat = StaticValues.AudioStatics.ResponseFormat.Text
            });
            
            fileStream.Close();

            if (audioResult.Successful)
            {
                WriteSuccess("Done transcribing!\n");

                DirectoryInfo? directoryInfo = file.Directory;

                if (directoryInfo == null)
                {
                    WriteError("Unknown error!");
                    return;
                }

                DirectoryInfo? successDir;

                DirectoryInfo[]? dirs = directoryInfo?.GetDirectories("output");

                if(dirs == null)
                {
                    Console.WriteLine("Creating output folder...");

                    successDir = directoryInfo?.CreateSubdirectory("output");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Created {successDir?.FullName}\n");
                    Console.ResetColor();
                }
                else if (dirs.Any())
                {
                    Console.WriteLine("Output directory already exists");

                    successDir = dirs.First();
                }
                else
                {
                    Console.WriteLine("Creating output folder...");

                    successDir = directoryInfo?.CreateSubdirectory("output");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Created {successDir?.FullName}\n");
                    Console.ResetColor();
                }

                FileInfo fileInfo = new($"{successDir?.FullName}\\{file.Name}.txt");

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }

                WriteInfo($"Writing to {fileInfo.FullName}...");

                File.WriteAllText(fileInfo.FullName, audioResult.Text);

                WriteSuccess("Done writing!\n");
            }
            else
            {
                if (audioResult.Error == null)
                {
                    WriteError("Unknown error!\n");
                    return;
                }

                WriteError($"{audioResult.Error.Code}: {audioResult.Error.Message}\n");
            }
        }

        static List<FileInfo> FetchFiles()
        {
            string? folderPath;

            do
            {
                WriteInfo("\nGive folder path to fetch files from: ");

                folderPath = Console.ReadLine();

                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    WriteError("Invalid path!");
                    continue;
                }

                break;
            } while (true);

            List<FileInfo> files = new();

            DirectoryInfo directory = new(folderPath);

            WriteInfo("Going through the files...");

            foreach (FileInfo file in directory.GetFiles())
            {
                switch (file.Extension)
                {
                    case ".mp3":
                    case ".mp4":
                    case ".mpeg":
                    case ".mpga":
                    case ".m4a":
                    case "wav":
                    case "webm":
                        double fileSizeInMegabytes = file.Length / (1024 * 1024) ;
                        
                        if (fileSizeInMegabytes > 25)
                        {
                            WriteError($"Rejected: {file.Name} | file size is above 25 MB");
                        }
                        else
                        {
                            WriteSuccess($"Accepted: {file.Name}");
                            files.Add(file);
                        }
                        break;

                    default:
                        WriteError($"Rejected: {file.Name} | invalid file type");
                        break;
                }
            }

            return files;
        }

        static async Task<OpenAIService> FetchAPI()
        {
            OpenAIService? openAIService;

            string? apiKey;

            do
            {
                WriteInfo("\nProvide OpenAI API key: ");

                apiKey = Console.ReadLine();

                WriteInfo("Checking validity of the API key...");

                if (string.IsNullOrEmpty(apiKey))
                {
                    WriteError("Invalid API key!");
                    continue;
                }

                openAIService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = apiKey
                });

                ModelListResponse? modelListResponse;

                try
                {
                    modelListResponse = await openAIService.ListModel();
                }
                catch
                {
                    WriteError("Invalid API key!");
                    continue;
                }

                

                if (modelListResponse.Successful)
                {
                    break;
                }
                else
                {
                    if (modelListResponse.Error == null)
                    {
                        WriteError("Unknown Error!");
                        break;
                    }

                    WriteError($"{modelListResponse.Error.Code}: {modelListResponse.Error.Message}");
                    continue;
                }

            }while (true);

        // Helper methods for colored console output
        static void WriteSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        static void WriteInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
