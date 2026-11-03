using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class StreamStatusDto
    {
        [ParameterDescription("Whether the camera stream is running.")]
        public bool IsStreaming { get; set; }
        [ParameterDescription("The stream's frame size, frame rate and JPEG quality.")]
        public StreamConfigDto Config { get; set; }
    }

    public class StreamConfigDto
    {
        [ParameterDescription("The port the stream listens on.")]
        public int Port { get; set; }
        [ParameterDescription("The address the stream is served at.")]
        public string Address { get; set; }
        [ParameterDescription("How many pixels wide one frame is.")]
        public int FrameWidth { get; set; }
        [ParameterDescription("How many pixels tall one frame is.")]
        public int FrameHeight { get; set; }
        [ParameterDescription("How many frames per second the stream aims for.")]
        public int TargetFps { get; set; }
        [ParameterDescription("The JPEG quality, 1 to 100.")]
        public int JpegQuality { get; set; }
    }
}
