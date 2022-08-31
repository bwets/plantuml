using bwets.PlantUML.Structurizr.Definitions;
using Structurizr;

namespace bwets.PlantUML.Structurizr.ModelExtensions;

public static class ElementExtensions
{
    public static string? GetLink(this Element element)
    {
        return element.Properties?.TryGetValue(Properties.Link, out var value) == true ? value : null;
    }

    public static void SetLink(this Element container, string sprite)
    {
        container.Properties[Properties.Link] = sprite;
    }

    public static string? GetSprite(this Element element)
    {
        return element.Properties?.TryGetValue(Properties.Sprite, out var value) == true ? value : null;
    }

    public static void SetSprite(this Element element, string sprite)
    {
        element.Properties[Properties.Sprite] = sprite;
    }
}