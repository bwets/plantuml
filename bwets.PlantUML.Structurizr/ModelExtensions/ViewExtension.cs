using Structurizr;

namespace bwets.PlantUML.Structurizr.ModelExtensions;

public static class ViewExtension
{
    public static SystemLandscapeView  Include(this SystemLandscapeView view, SoftwareSystem system)
    {
        view.Add(system);
        return view;
    }
}