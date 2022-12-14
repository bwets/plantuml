using bwets.PlantUML.Structurizr.Definitions;
using Structurizr;

namespace bwets.PlantUML.Structurizr.ModelExtensions
{
    public static class RelationshipExtensions
    {
        public static string GetDirection(this Relationship relationship)
        {
            string value = DirectionValues.NotSet;
            return relationship.Properties?.TryGetValue(Properties.Direction, out value) == true ? value : value;
        }

        /// <summary>
        /// Set a (default) direction of the relation which should be used in all C4PlantUML views
        /// </summary>
        /// <param name="relationship"></param>
        /// <param name="direction">one of <see cref="DirectionValues"/></param>
        public static void SetDirection(this Relationship relationship, string direction)
        {
            if (string.IsNullOrWhiteSpace(direction)) // direction DirectionValues.NotSet
                relationship.Properties.Remove(Properties.Direction);
            else
                relationship.Properties[Properties.Direction] = direction;
        }
    }
}