using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ImageDto
    {
        [ParameterDescription("Whether the upload worked, as the mod reports it.")]
        public string Result { get; set; }
        [ParameterDescription("The image as base64 text.")]
        public string ImageBase64 { get; set; }
    }

    public class ImageUploadRequest
    {
        public string Name { get; set; }
        [ParameterDescription("The image to upload, as base64 text.")]
        public string Image { get; set; }
        [ParameterDescription("Which direction the texture faces.")]
        public string Direction { get; set; }
        [ParameterDescription("Where the texture sits on its cell.")]
        public string Offset { get; set; }
        [ParameterDescription("How large the texture is drawn.")]
        public string Scale { get; set; }
        [ParameterDescription("Which kind of thing the texture belongs to.")]
        public string ThingType { get; set; }
        [ParameterDescription("Whether the uploaded texture stacks on its cell.")]
        public string IsStackable { get; set; }
        [ParameterDescription("The mask texture the upload uses.")]
        public string MaskImage { get; set; }
        [ParameterDescription("Whether the upload reuses an existing index.")]
        public int UpdateItemIndex { get; set; }
    }

    public class FactionIconImageDto
    {
        [ParameterDescription("The icon as base64 text.")]
        public ImageDto Image { get; set; }
        [ParameterDescription("The colour the icon is tinted with.")]
        public string Color { get; set; }
    }
}
