using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class SettlementDto
    {
        [ParameterDescription("The settlement's world tile id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        public int TileId { get; set; }
        [ParameterDescription("The faction that holds the settlement, as the game names it.")]
        public FactionDto Faction { get; set; }
    }

    public class SiteDto
    {
        [ParameterDescription("The site's world tile id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        public int TileId { get; set; }
        [ParameterDescription("The faction that holds the site.")]
        public FactionDto Faction { get; set; }
    }

    public class TileDto
    {
        [ParameterDescription("The world tile's numeric id, as the world grid numbers them.")]
        public int Id { get; set; }
        [ParameterDescription("The biome's name, e.g. temperate forest.")]
        public string Biome { get; set; }
        [ParameterDescription("How high the tile lies, in metres above sea level.")]
        public float Elevation { get; set; }
        [ParameterDescription("The tile's average temperature, in Celsius.")]
        public float Temperature { get; set; }
        [ParameterDescription("How much rain the tile gets per year, in millimetres.")]
        public float Rainfall { get; set; }
        [ParameterDescription("How hilly the tile is: flat, small hills or mountains.")]
        public string Hilliness { get; set; }
        [ParameterDescription("The roads crossing the tile, as the game names them.")]
        public List<string> Roads { get; set; }
        [ParameterDescription("The rivers crossing the tile, as the game names them.")]
        public List<string> Rivers { get; set; }
        [ParameterDescription("The tile's latitude, -90 to 90 degrees.")]
        public float Lat { get; set; }
        [ParameterDescription("The tile's longitude, -180 to 180 degrees.")]
        public float Lon { get; set; }
        [ParameterDescription("Whether the tile carries pollution.")]
        public bool IsPolluted { get; set; }
        [ParameterDescription("How much pollution the tile carries, 0 to 1.")]
        public float Pollution { get; set; }
    }

    public class TileDetailsDto : TileDto
    {
        [ParameterDescription("The tile's time zone offset, in hours from UTC.")]
        public string TimeZone { get; set; }
        [ParameterDescription("How much food a caravan can forage here, 0 to 1.")]
        public float Forageability { get; set; }
        [ParameterDescription("How many days a year plants grow here.")]
        public string GrowingPeriod { get; set; }
        [ParameterDescription("How much slower travel over the tile is, 0 for open ground.")]
        public float MovementDifficulty { get; set; }
        [ParameterDescription("The stone kinds the tile's map holds.")]
        public List<string> StoneTypes { get; set; }
    }

    public class CoordinatesDto
    {
        [ParameterDescription("The latitude, in degrees north of the equator.")]
        public float Lat { get; set; }
        [ParameterDescription("The longitude, in degrees east of Greenwich.")]
        public float Lon { get; set; }
    }
}
