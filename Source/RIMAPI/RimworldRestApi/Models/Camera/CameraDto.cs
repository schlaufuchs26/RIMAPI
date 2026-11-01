using Newtonsoft.Json;

namespace RIMAPI.Models.Camera
{
    public class CameraScreenshotRequestDto
    {
        [JsonProperty("format")]
        public string Format { get; set; } = "jpeg";

        [JsonProperty("quality")]
        public int Quality { get; set; } = 75;

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("hide_ui")]
        public bool HideUI { get; set; } = true;
    }


    public class NativeScreenshotRequestDto
    {
        public string FileName { get; set; } // Optional: Game auto-generates if empty
        public float? CenterX { get; set; }
        public float? CenterZ { get; set; }
        public float? ZoomLevel { get; set; } // Typically 10 to 60

        // Without the explicit name the resolver splits the acronym, so the key
        // would be hide_u_i and a caller sending hide_ui would keep the default.
        [JsonProperty("hide_ui")]
        public bool HideUI { get; set; } = true;
    }

    public class CameraScreenshotResponseDto
    {
        public ImageData Image { get; set; }
        public ImageMetadata Metadata { get; set; }

        // The cells the picture covers (fox ticket #4025). A caller that
        // compares the map art with the screen maps a cell to a pixel with
        // these numbers instead of reading the save's camera fields.
        public CameraViewDto Camera { get; set; }

        public GameContext GameContext { get; set; }
    }

    public class CameraViewDto
    {
        // The camera's root position: the cell at the picture's centre.
        public float RootX { get; set; }
        public float RootZ { get; set; }

        // Half the picture's height in cells; the width follows its aspect.
        public float RootSize { get; set; }

        // The visible cells, rounded outward and cut to the map. HasCells is
        // false when the camera looks past the map, so the numbers name the
        // camera without inventing cells.
        public int X1 { get; set; }
        public int Z1 { get; set; }
        public int X2 { get; set; }
        public int Z2 { get; set; }
        public bool HasCells { get; set; }
    }

    public class ImageData
    {
        public string DataUri { get; set; }
    }

    public class ImageMetadata
    {
        public string Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int SizeBytes { get; set; }
    }

    public class GameContext
    {
        public int CurrentTick { get; set; }
    }
}
