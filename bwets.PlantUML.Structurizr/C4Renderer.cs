using System.Diagnostics;

namespace bwets.PlantUML.Structurizr
{
    public class PlantUMLRenderer
    {
        
        private const string Version = "1.2022.7";

        public async Task EnsureDependencies()
        {
            var filename = Path.Combine(AppContext.BaseDirectory, JarFilename);
            if (!File.Exists(filename))
            {
                var url = $"https://github.com/plantuml/plantuml/releases/download/v{Version}/{JarFilename}";
                var client = new HttpClient();
                var response = await client.GetAsync(url);
                await using var fileStream = new FileStream(filename, FileMode.CreateNew);
                await response.Content.CopyToAsync(fileStream);
            }
            
            
            
        }

        public async Task RenderFolder(string path)
        {
            var files = Directory.GetFiles(path, "*.puml");
            foreach (var file in files)
            {
                await RenderFile(file);
            }
        }

        private string JarFilename => $"plantuml-{Version}.jar";

        public async Task RenderFile(string file)
        {
            
            var args = new ProcessStartInfo("java.exe", $"-jar {JarFilename} -tpng {file} -v");
            var process = Process.Start(args);
            await process.WaitForExitAsync();
        }
        

    }
}
