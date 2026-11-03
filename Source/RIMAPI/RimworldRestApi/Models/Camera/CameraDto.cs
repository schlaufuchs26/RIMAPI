using Newtonsoft.Json;
using RIMAPI.Core;

namespace RIMAPI.Models.Camera
{
    public class CameraScreenshotRequestDto
    {
        [JsonProperty("format")]
        public string Format { get; set; } = "jpeg";

        [JsonProperty("quality")]
        public int Quality { get; set; } = 75;

        [JsonProperty("width")]
        [ParameterDescription("How many pixels wide the screenshot is.")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        [ParameterDescription("How many pixels tall the screenshot is.")]
        public int? Height { get; set; }

        [JsonProperty("hide_ui")]
        [ParameterDescription("Whether the UI is left out of the shot.")]
        public bool HideUI { get; set; } = true;
    }


    public class NativeScreenshotRequestDto
    {
        [ParameterDescription("The file name to write the shot under, in the save folder.")]
        public string FileName { get; set; } // Optional: Game auto-generates if empty
        [ParameterDescription("The cell the camera centres on, x.")]
        public float? CenterX { get; set; }
        [ParameterDescription("The cell the camera centres on, z.")]
        public float? CenterZ { get; set; }
        [ParameterDescription("How far the camera zooms in, 1 is the game's default.")]
        public float? ZoomLevel { get; set; } // Typically 10 to 60

        // Without the explicit name the resolver splits the acronym, so the key
        // would be hide_u_i and a caller sending hide_ui would keep the default.
        [JsonProperty("hide_ui")]
        [ParameterDescription("Whether the UI is left out of the shot.")]
        public bool HideUI { get; set; } = true;
    }

    public class CameraScreenshotResponseDto
    {
        [ParameterDescription("The image itself, as a data URI.")]
        public ImageData Image { get; set; }
        [ParameterDescription("The image's size and byte count.")]
        public ImageMetadata Metadata { get; set; }

        // The cells the picture covers (fox ticket #4025). A caller that
        // compares the map art with the screen maps a cell to a pixel with
        // these numbers instead of reading the save's camera fields.
        [ParameterDescription("The cells the picture covers (fox ticket #4025).")]
        public CameraViewDto Camera { get; set; }

        [ParameterDescription("The game tick the shot was taken on.")]
        public GameContext GameContext { get; set; }
    }

    public class CameraViewDto
    {
        // The camera's root position: the cell at the picture's centre.
        [ParameterDescription("The camera's root position: the cell at the picture's centre.")]
        public float RootX { get; set; }
        [ParameterDescription("The top edge of the visible cells, z.")]
        public float RootZ { get; set; }

        // Half the picture's height in cells; the width follows its aspect.
        [ParameterDescription("Half the picture's height in cells; the width follows its aspect.")]
        public float RootSize { get; set; }

        // The visible cells, rounded outward and cut to the map. HasCells is
        // false when the camera looks past the map, so the numbers name the
        // camera without inventing cells.
        [ParameterDescription("The visible cells, rounded outward and cut to the map.")]
        public int X1 { get; set; }
        [ParameterDescription("The first visible row's z.")]
        public int Z1 { get; set; }
        [ParameterDescription("The visible rectangle's far corner x.")]
        public int X2 { get; set; }
        [ParameterDescription("The visible rectangle's far corner z.")]
        public int Z2 { get; set; }
        [ParameterDescription("Whether the camera looks at map cells at all.")]
        public bool HasCells { get; set; }
    }

    public class ImageData
    {
        [ParameterDescription("The image encoded as a data URI.")]
        public string DataUri { get; set; }
    }

    public class ImageMetadata
    {
        public string Format { get; set; }
        [ParameterDescription("How many pixels wide the image is.")]
        public int Width { get; set; }
        [ParameterDescription("How many pixels tall the image is.")]
        public int Height { get; set; }
        [ParameterDescription("How many bytes the image takes.")]
        public int SizeBytes { get; set; }
    }

    public class GameContext
    {
        [ParameterDescription("The game tick the answer was built on.")]
        public int CurrentTick { get; set; }
    }
}
