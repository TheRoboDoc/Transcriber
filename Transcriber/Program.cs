using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.ResponseModels.ModelResponseModels;

namespace Transcriber
{
    internal class Program
    {
        static void Main()
        {
            while (true)
            {
                MainAsync().GetAwaiter().GetResult();

                Console.Write("\n\nDo you want to run again(Y/n)?: ");
                string? choice = Console.ReadLine();

                if (choice == "n")
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
            OpenAIService openAIService = await FetchAPI();

            List<FileInfo> files = FetchFiles();

            bool proceed = true;

            Console.Write("\nContinue(Y/n)?: ");

            string? answer = Console.ReadLine();

            if (answer == "n")
            {
                proceed = false;
            }

            if (proceed == false)
            {
                return;
            }

            await TranscribeAll(files, openAIService);
        }

        static async Task TranscribeAll(List<FileInfo> files, OpenAIService openAIService)
        {
            Console.WriteLine("Transcribing files...");

            foreach (FileInfo file in files)
            {
                await Transcribe(file, openAIService);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done transcribing all the files!\n");
            Console.ResetColor();
        }

        static async Task Transcribe(FileInfo file, OpenAIService openAIService)
        {
            string fileName = file.Name;

            Console.WriteLine($"Fetching {file.Name}...");

            Stream fileStream = new FileStream(file.FullName, FileMode.Open);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done fetching!\n");
            Console.ResetColor();

            Console.WriteLine($"Transcribing {file.Name}...");

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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Done transcribing!\n");
                Console.ResetColor();

                DirectoryInfo? directoryInfo = file.Directory;

                if (directoryInfo == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Uknown error!");
                    Console.ResetColor();

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

                Console.WriteLine($"Writing to {fileInfo.FullName}...");

                File.WriteAllText(fileInfo.FullName, audioResult.Text);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Done writing!\n");
                Console.ResetColor();
            }
            else
            {
                if (audioResult.Error == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unknown error!\n");
                    Console.ResetColor();

                    return;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{audioResult.Error.Code}: {audioResult.Error.Message}\n");
                Console.ResetColor();
            }
        }

        static List<FileInfo> FetchFiles()
        {
            string? folderPath;

            do
            {
                Console.Write("\nGive folder path to fetch files from: ");

                folderPath = Console.ReadLine();

                if (string.IsNullOrEmpty(folderPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid path!");
                    Console.ResetColor();

                    continue;
                }

                if (!Directory.Exists(folderPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid path!");
                    Console.ResetColor();

                    continue;
                }

                break;
            } while (true);

            List<FileInfo> files = new();

            DirectoryInfo directory = new(folderPath);

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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Rejected: {file.Name} | file size is above 25 MB");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Accpeted: {file.Name}");
                            Console.ResetColor();

                            files.Add(file);
                        }
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Rejected: {file.Name} | invalid file type");
                        Console.ResetColor();

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
                Console.Write("\nProvide OpenAI API key: ");

                apiKey = Console.ReadLine();

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid API key!");
                    Console.ResetColor();

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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid API key!");
                    Console.ResetColor();

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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Uknown Error!");
                        Console.ResetColor();

                        break;
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{modelListResponse.Error.Code}: {modelListResponse.Error.Message}");
                    Console.ResetColor();

                    continue;
                }

            }while (true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("API key accepted");
            Console.ResetColor();

            return openAIService;
        }
    }
}