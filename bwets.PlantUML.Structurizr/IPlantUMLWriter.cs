using Structurizr;

namespace bwets.PlantUML.Structurizr
{
    public interface IPlantUMLWriter
    {
        void Write(string path);
        void Write(View view, string path);
    }
}