using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI
{
    /// <summary>
    /// The static controller and state manager for the RIMAPI REST server.
    /// <para>While inheriting from GameComponent to support save data (ExposeData),
    /// this class primarily functions as a static singleton to manage the server's lifecycle
    /// globally (Main Menu + In-Game).</para>
    /// </summary>
    public class RIMAPI_GameComponent : GameComponent
    {
        /// <summary>
        /// The active instance of the API server.
        /// </summary>
        private static ApiServer _apiServer;

        /// <summary>
        /// Flag indicating if the server has been successfully started.
        /// </summary>
        private static bool _serverInitialized;

        /// <summary>
        /// Thread synchronization lock for server initialization and shutdown operations.
        /// </summary>
        private static readonly object _serverLock = new object();

        /// <summary>
        /// Internal tick counter used to trigger data cache refreshes.
        /// </summary>
        private static int _staticTickCounter;

        /// <summary>
        /// Colonists this mod drafted for an arrest order. The game only offers
        /// Arrest while the colonist is drafted, so the order drafts him; the
        /// draft is dropped here once the arrest job is neither current nor
        /// queued, which is the moment a player would undraft by hand
        /// (ticket #3137).
        /// </summary>
        private static readonly List<Pawn> _pendingUndrafts = new List<Pawn>();

        public RIMAPI_GameComponent(Game game) : base() { }

        /// <summary>
        /// Initializes and starts the HTTP listener and API services.
        /// <para>This method is thread-safe and checks for existing instances before starting.
        /// It loads settings, registers the SSE service, and initializes extensions.</para>
        /// </summary>
        public static void StartServer()
        {
            lock (_serverLock)
            {
                if (_serverInitialized)
                {
                    LogApi.Info("Server already initialized, skipping...");
                    return;
                }

                try
                {
                    LogApi.IsLogging = RIMAPI_Mod.Settings.EnableLogging;
                    _apiServer?.Dispose();

                    _apiServer = new ApiServer(RIMAPI_Mod.Settings);
                    RIMAPI_Mod.RegisterSseService(_apiServer.SseService);
                    _apiServer.Start();
                    _serverInitialized = true;

                    LogApi.Info($"REST API Server started on port {RIMAPI_Mod.Settings.serverPort}");

                    var extensions = _apiServer.GetExtensions();
                    if (extensions.Count > 0)
                        LogApi.Info($"API Server loaded {extensions.Count} extensions");
                }
                catch (Exception ex)
                {
                    LogApi.Error($"Failed to start API server - {ex.Message}");
                    _serverInitialized = false;
                }
            }
        }

        /// <summary>
        /// Processes pending actions on the Unity Main Thread.
        /// <para>This includes handling queued HTTP requests, processing SSE broadcasts,
        /// and refreshing the data cache at the configured tick interval.</para>
        /// </summary>
        public static void ProcessServerQueues()
        {
            if (!_serverInitialized || _apiServer == null)
                return;

            // 1. Process Requests
            _apiServer.ProcessQueuedRequests();

            // 2. Process Broadcasts
            _apiServer.ProcessBroadcastQueue();

            // 3. Handle Refresh Logic (Previously in GameComponentTick)
            // We do this here so it works even at the Main Menu
            _staticTickCounter++;
            if (_staticTickCounter >= RIMAPI_Mod.Settings.refreshIntervalTicks)
            {
                _staticTickCounter = 0;
                _apiServer.RefreshDataCache();
            }

            // 4. Drop the drafts this mod set for arrest orders.
            ProcessPendingUndrafts();
        }

        /// <summary>
        /// Undrafts a colonist once his arrest job is over. The arrest order
        /// drafts him because the game offers Arrest only while drafted; leaving
        /// him drafted would keep him out of the colony's work for good.
        /// </summary>
        public static void UndraftWhenFree(Pawn pawn)
        {
            if (pawn == null || _pendingUndrafts.Contains(pawn)) return;
            _pendingUndrafts.Add(pawn);
        }

        private static void ProcessPendingUndrafts()
        {
            for (int i = _pendingUndrafts.Count - 1; i >= 0; i--)
            {
                Pawn pawn = _pendingUndrafts[i];
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.jobs == null)
                {
                    _pendingUndrafts.RemoveAt(i);
                    continue;
                }
                if (ArrestJobOutstanding(pawn)) continue;
                if (pawn.Drafted) pawn.drafter.Drafted = false;
                _pendingUndrafts.RemoveAt(i);
            }
        }

        private static bool ArrestJobOutstanding(Pawn pawn)
        {
            if (pawn.jobs.curJob != null && pawn.jobs.curJob.def == JobDefOf.Arrest) return true;
            if (pawn.jobs.jobQueue == null) return false;
            foreach (QueuedJob queued in pawn.jobs.jobQueue)
            {
                if (queued.job != null && queued.job.def == JobDefOf.Arrest) return true;
            }
            return false;
        }

        /// <summary>
        /// Safely shuts down the API server and disposes of resources.
        /// </summary>
        public static void Shutdown()
        {
            lock (_serverLock)
            {
                if (_serverInitialized)
                {
                    Log.Message("Shutting down API server...");
                    _apiServer?.Dispose();
                    _apiServer = null;
                    _serverInitialized = false;
                }
            }
        }

        /// <summary>
        /// Restarts the API server by calling Shutdown() followed by StartServer().
        /// </summary>
        public static void RestartServer()
        {
            Log.Message("Restarting API server...");
            Shutdown();
            StartServer();
        }

        /// <summary>
        /// Checks if the API server is currently initialized and running.
        /// </summary>
        /// <returns>True if the server is active; otherwise, false.</returns>
        public static bool IsServerRunning()
        {
            return _serverInitialized && _apiServer != null;
        }

        /// <summary>
        /// Registers a new extension with the running API server.
        /// </summary>
        /// <param name="extension">The extension instance to register.</param>
        public static void RegisterExtension(IRimApiExtension extension)
        {
            if (_serverInitialized && _apiServer != null)
                _apiServer.RegisterExtension(extension);
            else
                LogApi.Warning($"Cannot register extension {extension.ExtensionName} - server not initialized");
        }

        /// <summary>
        /// Retrieves a registered extension by its ID.
        /// </summary>
        /// <param name="extensionId">The unique identifier of the extension.</param>
        /// <returns>The extension instance if found; otherwise, null.</returns>
        public static IRimApiExtension GetExtension(string extensionId)
        {
            return _serverInitialized ? _apiServer?.GetExtension(extensionId) : null;
        }

        /// <summary>
        /// Saves or loads data associated with this game component.
        /// <para>Note: This method is only called when a save game is loaded or saved.
        /// It does not run at the Main Menu.</para>
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // Keep this if you need to save mod settings into the save file,
            // otherwise it can be empty.
        }
    }
}