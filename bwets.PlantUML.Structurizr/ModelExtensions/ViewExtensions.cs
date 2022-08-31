using Structurizr;

namespace bwets.PlantUML.Structurizr.ModelExtensions;

public static class ViewExtensions
{
    public static SystemLandscapeView Include(this SystemLandscapeView view, SoftwareSystem system)
    {
        view.Add(system);
        return view;
    }

    public static SystemLandscapeView Include(this SystemLandscapeView view, params SoftwareSystem[] systems)
    {
        foreach (var system in systems)
        {
            view.Add(system);
        }

        return view;
    }
    public static SystemContextView Include(this SystemContextView view, params SoftwareSystem[] systems)
    {
        foreach (var system in systems)
        {
            view.Add(system);
        }

        return view;
    }

    public static ContainerView Include(this ContainerView view, params SoftwareSystem[] systems)
    {
        foreach (var system in systems)
        {
            view.Add(system);
        }

        return view;
    }
}