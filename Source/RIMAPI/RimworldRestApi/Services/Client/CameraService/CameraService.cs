using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using RIMAPI.CameraStreamer;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Camera;
using UnityEngine;
using Verse;

namespace RIMAPI.Services
{
    public class CameraService : ICameraService
    {
        public CameraService() { }

        public ApiResult ChangeZoom(int zoom)
        {
            try
            {
                Find.CameraDriver.SetRootPosAndSize(
                    Find.CameraDriver.MapPosition.ToVector3(),
                    zoom
                );
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        private static void SetScreenshotMode(bool state)
        {
            if (Find.UIRoot == null || Find.UIRoot.screenshotMode == null) return;
            var handler = Find.UIRoot.screenshotMode;

            handler.Active = state;
        }

        private static void TakeNativeScreenshotDirect(string fileName)
        {
            string folderPath = GenFilePaths.ScreenshotFolderPath;

            DirectoryInfo dir = new DirectoryInfo(folderPath);
            if (!dir.Exists)
            {
                dir.Create();
            }

            string fullPath = Path.Combine(folderPath, fileName + ".png");
            ScreenCapture.CaptureScreenshot(fullPath);
        }

        public ApiResult<string> TakeNativeScreenshot(NativeScreenshotRequestDto request)
        {
            if (Find.CurrentMap == null)
            {
                return ApiResult<string>.Fail("Cannot take a screenshot. No map is currently loaded.");
            }

            // Start a coroutine to handle the asynchronous screenshot delay
            Find.CameraDriver.StartCoroutine(NativeScreenshotRoutine(request));

            string savedName = string.IsNullOrEmpty(request.FileName) ? "Auto-generated" : request.FileName;
            return ApiResult<string>.Ok($"Native screenshot '{savedName}' queued successfully.");
        }

        private IEnumerator NativeScreenshotRoutine(NativeScreenshotRequestDto request)
        {
            bool originalUIState = false;

            // 1. Move the camera instantly
            if (request.CenterX.HasValue && request.CenterZ.HasValue && request.ZoomLevel.HasValue)
            {
                Vector3 targetPos = new Vector3(request.CenterX.Value, 0, request.CenterZ.Value);
                Find.CameraDriver.SetRootPosAndSize(targetPos, request.ZoomLevel.Value);

                // Yield for one frame to let the game engine render the new camera position
                yield return new WaitForEndOfFrame();
            }

            // 2. Hide UI
            if (request.HideUI && Find.UIRoot != null)
            {
                originalUIState = Find.UIRoot.screenshotMode.Active;
                SetScreenshotMode(true);
            }

            // Trigger the native screenshot tool
            string fileName = string.IsNullOrEmpty(request.FileName) ? $"RIMAPI_{DateTime.Now.Ticks}" : request.FileName;
            TakeNativeScreenshotDirect(fileName);

            // WAIT for the frame to finish rendering so the screenshot actually captures!
            yield return new WaitForEndOfFrame();

            // 5. Restore UI
            if (request.HideUI && Find.UIRoot != null)
            {
                SetScreenshotMode(originalUIState);
            }
        }

        public ApiResult MoveToPosition(int x, int y)
        {
            try
            {
                IntVec3 position = new IntVec3(x, 0, y);
                Find.CameraDriver.JumpToCurrentMapLoc(position);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        public ApiResult JumpToPawn(int pawnId)
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null)
                    return ApiResult.Fail("No map is currently active.");

                var pawn = FindSpawnedPawn(map, pawnId);
                if (pawn == null)
                    return ApiResult.Fail($"Pawn {pawnId} not found on the current map.");

                Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        private static Pawn FindSpawnedPawn(Map map, int pawnId)
        {
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.thingIDNumber == pawnId)
                    return pawn;
            }
            return null;
        }
        private static CameraScreenshotResponseDto CaptureScreenshotAsDto(CameraScreenshotRequestDto request)
        {
            request = request ?? new CameraScreenshotRequestDto();

            string format = (request.Format ?? "jpeg").ToLower();
            int quality = Mathf.Clamp(request.Quality, 1, 100);

            int targetWidth = request.Width ?? Screen.width;
            int targetHeight = request.Height ?? Screen.height;

            targetWidth = Mathf.Clamp(targetWidth, 16, 7680);
            targetHeight = Mathf.Clamp(targetHeight, 16, 4320);

            RenderTexture tempRT = RenderTexture.GetTemporary(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
            RenderTexture flippedRT = null;
            Texture2D texture = null;

            try
            {
                bool needsFlip = false;

                // 1. SYNCHRONOUS UI HIDE (Direct Camera Render)
                if (request.HideUI && Find.Camera != null)
                {
                    // Hijack the main world camera. 
                    // This natively renders right-side-up, so we DO NOT need to flip it later.
                    RenderTexture oldTarget = Find.Camera.targetTexture;
                    Find.Camera.targetTexture = tempRT;
                    Find.Camera.Render();
                    Find.Camera.targetTexture = oldTarget;

                    needsFlip = false;
                }
                else
                {
                    // Capture the screen buffer (includes UI).
                    // This grabs the raw display memory, which usually needs flipping.
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);

                    needsFlip = true;
                }

                // 2. GPU Blit (Conditionally flip based on the render method)
                flippedRT = RenderTexture.GetTemporary(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);

                if (needsFlip)
                {
                    // Invert the Y-axis for ScreenCapture
                    Graphics.Blit(tempRT, flippedRT, new Vector2(1, -1), new Vector2(0, 1));
                }
                else
                {
                    // Copy exactly as-is for the Camera Render
                    Graphics.Blit(tempRT, flippedRT);
                }

                // 3. Read the pixels from the processed texture
                texture = new Texture2D(flippedRT.width, flippedRT.height, TextureFormat.RGB24, false);
                RenderTexture.active = flippedRT;
                texture.ReadPixels(new Rect(0, 0, flippedRT.width, flippedRT.height), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                // 4. Encode based on requested format
                byte[] imageBytes;
                string actualFormat;

                if (format == "png")
                {
                    imageBytes = texture.EncodeToPNG();
                    actualFormat = "png";
                }
                else
                {
                    imageBytes = texture.EncodeToJPG(quality);
                    actualFormat = "jpeg";
                }

                string base64String = Convert.ToBase64String(imageBytes);

                var dto = new CameraScreenshotResponseDto
                {
                    Image = new ImageData
                    {
                        DataUri = $"data:image/{actualFormat};base64,{base64String}"
                    },
                    Metadata = new ImageMetadata
                    {
                        Format = actualFormat,
                        Width = flippedRT.width,
                        Height = flippedRT.height,
                        SizeBytes = imageBytes.Length
                    },
                    Camera = CameraViewOf(flippedRT.width, flippedRT.height),
                    GameContext = new GameContext
                    {
                        CurrentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0
                    }
                };

                return dto;
            }
            finally
            {
                // 5. Cleanup memory
                if (tempRT != null) RenderTexture.ReleaseTemporary(tempRT);
                if (flippedRT != null) RenderTexture.ReleaseTemporary(flippedRT);
                if (texture != null) UnityEngine.Object.Destroy(texture);
            }
        }

        public ApiResult<CameraScreenshotResponseDto> MakeScreenshot(CameraScreenshotRequestDto request)
        {
            try
            {
                var dto = CaptureScreenshotAsDto(request);
                return ApiResult<CameraScreenshotResponseDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                Log.Error($"[RIMAPI] Failed to capture screenshot: {ex}");
                return ApiResult<CameraScreenshotResponseDto>.Fail(ex.Message);
            }
        }

        public ApiResult<StreamStatusDto> GetStreamStatus(ICameraStream stream)
        {
            var result = stream.GetStatus();
            return ApiResult<StreamStatusDto>.Ok(result);
        }

        public ApiResult SetupStream(ICameraStream stream, StreamConfigDto config)
        {
            try
            {
                stream.SetConfig(config);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        public ApiResult StartStream(ICameraStream stream)
        {
            try
            {
                stream.StartStreaming();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        public ApiResult StopStream(ICameraStream stream)
        {
            try
            {
                stream.StopStreaming();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Ok();
        }

        public Task<ApiResult<CameraScreenshotResponseDto>> MakeScreenshotAsync(CameraScreenshotRequestDto request)
        {
            var tcs = new TaskCompletionSource<ApiResult<CameraScreenshotResponseDto>>();

            if (Find.CurrentMap == null || Find.CameraDriver == null)
            {
                tcs.TrySetResult(ApiResult<CameraScreenshotResponseDto>.Fail("Cannot take a screenshot. No map is currently loaded."));
                return tcs.Task;
            }

            // Start the Coroutine, passing the request and the TaskCompletionSource
            Find.CameraDriver.StartCoroutine(CaptureScreenshotCoroutine(request, tcs));

            // The HTTP Controller will await this task, pausing until TrySetResult is called below!
            return tcs.Task;
        }

        private IEnumerator CaptureScreenshotCoroutine(CameraScreenshotRequestDto request, TaskCompletionSource<ApiResult<CameraScreenshotResponseDto>> tcs)
        {
            request = request ?? new CameraScreenshotRequestDto();

            string format = (request.Format ?? "jpeg").ToLower();
            int quality = Mathf.Clamp(request.Quality, 1, 100);

            int targetWidth = request.Width ?? Screen.width;
            int targetHeight = request.Height ?? Screen.height;

            targetWidth = Mathf.Clamp(targetWidth, 16, 7680);
            targetHeight = Mathf.Clamp(targetHeight, 16, 4320);

            // The picture comes from the game's own camera, not from the screen
            // buffer (fox ticket #4025): a debug slot keeps its window behind
            // or minimized, and a screen read there captures whatever the
            // window last showed. Camera.Render() into a render texture works
            // without a visible window, and it never contains the UI, so no
            // frame has to be waited for and no screenshot mode switched.
            var camera = Find.Camera;
            if (camera == null)
            {
                tcs.TrySetResult(ApiResult<CameraScreenshotResponseDto>.Fail("Cannot take a screenshot. The game camera is not up."));
                yield break;
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = null;

            try
            {
                RenderTexture previousTarget = camera.targetTexture;
                camera.targetTexture = renderTexture;
                camera.Render();
                camera.targetTexture = previousTarget;

                RenderTexture.active = renderTexture;
                texture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                byte[] imageBytes;
                string actualFormat;
                if (format == "png")
                {
                    imageBytes = texture.EncodeToPNG();
                    actualFormat = "png";
                }
                else
                {
                    imageBytes = texture.EncodeToJPG(quality);
                    actualFormat = "jpeg";
                }

                var dto = new CameraScreenshotResponseDto
                {
                    Image = new ImageData
                    {
                        DataUri = $"data:image/{actualFormat};base64,{Convert.ToBase64String(imageBytes)}"
                    },
                    Metadata = new ImageMetadata
                    {
                        Format = actualFormat,
                        Width = targetWidth,
                        Height = targetHeight,
                        SizeBytes = imageBytes.Length
                    },
                    Camera = CameraViewOf(targetWidth, targetHeight),
                    GameContext = new GameContext
                    {
                        CurrentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0
                    }
                };

                tcs.TrySetResult(ApiResult<CameraScreenshotResponseDto>.Ok(dto));
            }
            catch (Exception ex)
            {
                Log.Error($"[RIMAPI] Failed to capture DTO screenshot: {ex}");
                tcs.TrySetResult(ApiResult<CameraScreenshotResponseDto>.Fail(ex.Message));
            }
            finally
            {
                RenderTexture.active = null;
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        // CameraViewOf names the cells the picture covers (fox ticket #4025):
        // a check that compares the map art with the screen needs the camera's
        // position and zoom next to the pixels, or it has to read the save.
        private static CameraViewDto CameraViewOf(int width, int height)
        {
            var driver = Find.CameraDriver;
            var camera = Find.Camera;
            if (driver == null || camera == null)
                return null;

            var map = Find.CurrentMap;
            // The camera's own transform is the view's centre; CameraDriver
            // keeps its copy private, and MapPosition truncates it to a cell,
            // which would put the rectangle half a cell off.
            var root = camera.transform.position;
            var view = CameraViewRules.Visible(
                root.x,
                root.z,
                driver.RootSize,
                width,
                height,
                map != null ? map.Size.x : 0,
                map != null ? map.Size.z : 0
            );

            return new CameraViewDto
            {
                RootX = view.RootX,
                RootZ = view.RootZ,
                RootSize = view.RootSize,
                X1 = view.X1,
                Z1 = view.Z1,
                X2 = view.X2,
                Z2 = view.Z2,
                HasCells = view.HasCells
            };
        }
    }
}
