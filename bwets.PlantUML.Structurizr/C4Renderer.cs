using System.Diagnostics;

namespace bwets.PlantUML.Structurizr
{
    public class PlantUMLRenderer
    {

        public void RenderFolder(string path)
        {
            var files = Directory.GetFiles(path, "*.puml");
            foreach (var file in files)
            {
                RenderFile(file);
            }
        }


        public void RenderFile(string file)
        {
            var args = new ProcessStartInfo("java.exe", $"-jar plantuml-1.2022.6.jar -tpng {file} -v");
            var process = Process.Start(args);
            process.WaitForExit();
        }
        

    }
}
