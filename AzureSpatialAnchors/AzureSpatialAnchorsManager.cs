using Google.AR.Core;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureSpatialAnchors
{
    internal class AzureSpatialAnchorsManager
    {
        private readonly CloudSpatialAnchorSession spatialAnchorsSession;

        private TrackingState lastTrackingState = TrackingState.Stopped;

        private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;

        public bool CanCreateAnchor => this.CreateScanningProgressValue >= 1;

        public float CreateScanningProgressValue { get; set; } = 0;

        public bool LocatingAnchors => this.spatialAnchorsSession.ActiveWatchers.Count > 0;

        public bool IsArTracking => this.lastTrackingState == TrackingState.Tracking;

        public bool Running { get; private set; }

        public event EventHandler<AnchorLocatedEventArgs> OnAnchorLocated;

        public event EventHandler<LocateAnchorsCompletedEventArgs> OnLocateAnchorsCompleted;

        public event EventHandler<LogDebugEventArgs> OnLogDebug;

        public event EventHandler<SessionErrorEventArgs> OnSessionError;

        public event EventHandler<SessionUpdatedEventArgs> OnSessionUpdated;

        static AzureSpatialAnchorsManager()
        {
            CloudServices.Initialize(Android.App.Application.Context);
        }

        public AzureSpatialAnchorsManager(Session arCoreSession)
        {
            this.spatialAnchorsSession = new CloudSpatialAnchorSession();
            this.spatialAnchorsSession.Configuration.AccountKey = AccountDetails.SpatialAnchorsAccountKey;
            this.spatialAnchorsSession.Configuration.AccountId = AccountDetails.SpatialAnchorsAccountId;
            this.spatialAnchorsSession.Session = arCoreSession;
            this.spatialAnchorsSession.LogDebug += this.SpatialCloudSession_LogDebug;
            this.spatialAnchorsSession.Error += this.SpatialAnchorsSession_Error;
            this.spatialAnchorsSession.AnchorLocated += this.SpatialAnchorsSession_AnchorLocated;
            this.spatialAnchorsSession.LocateAnchorsCompleted += this.SpatialAnchorsSession_LocateAnchorsCompleted;
            this.spatialAnchorsSession.SessionUpdated += this.SpatialAnchorsSession_SessionUpdated;
        }

        public CloudSpatialAnchorWatcher StartLocating(AnchorLocateCriteria locateCriteria)
        {
            // Only 1 active watcher at a time is permitted.
            this.StopLocating();

            return this.spatialAnchorsSession.CreateWatcher(locateCriteria);
        }

        public Task DeleteAnchorAsync(CloudSpatialAnchor anchor)
        {
            return this.spatialAnchorsSession.DeleteAnchorAsync(anchor).GetAsync();
        }

        public void ResetSession(bool resumeIfRunning = false)
        {
            bool running = this.Running;

            this.StopLocating();
            this.StopSession();
            this.spatialAnchorsSession.Reset();

            if (resumeIfRunning && running)
            {
                this.StartSession();
            }
        }

        public async Task<CloudSpatialAnchor> CreateAnchorAsync(CloudSpatialAnchor newCloudAnchor)
        {
            if (newCloudAnchor == null)
            {
                throw new ArgumentNullException(nameof(newCloudAnchor));
            }

            if (newCloudAnchor.LocalAnchor == null || !string.IsNullOrEmpty(newCloudAnchor.Identifier))
            {
                throw new ArgumentException("The specified cloud anchor cannot be saved.", nameof(newCloudAnchor));
            }

            if (!this.CanCreateAnchor)
            {
                throw new ArgumentException("Not ready to create. Need more data.");
            }

            try
            {
                // TODO: result is always null...
                Java.Lang.Object result = await this.spatialAnchorsSession.CreateAnchorAsync(newCloudAnchor).GetAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return newCloudAnchor;
        }

        public void StartSession()
        {
            if (!this.Running)
            {
                this.spatialAnchorsSession.Start();
                this.Running = true;
            }
        }

        public void StopLocating()
        {
            CloudSpatialAnchorWatcher watcher = this.spatialAnchorsSession.ActiveWatchers.FirstOrDefault();

            // Only 1 active watcher at a time is permitted.
            watcher?.Stop();
            watcher?.Dispose();
        }

        public void StopSession()
        {
            this.StopLocating();
            if (this.Running)
            {
                this.spatialAnchorsSession.Stop();
                this.Running = false;
            }
        }

        public void Update(Frame frame)
        {
            if (frame.Camera.TrackingState != this.lastTrackingState
                || frame.Camera.TrackingFailureReason != this.lastTrackingFailureReason)
            {
                this.lastTrackingState = frame.Camera.TrackingState;
                this.lastTrackingFailureReason = frame.Camera.TrackingFailureReason;
                System.Diagnostics.Debug.WriteLine($"Tracker state changed: {this.lastTrackingState}, {this.lastTrackingFailureReason}.");
            }

            Task.Run(() => this.spatialAnchorsSession.ProcessFrame(frame));
        }

        private void SpatialAnchorsSession_AnchorLocated(object sender, AnchorLocatedEventArgs e)
        {
            this.OnAnchorLocated?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_Error(object sender, SessionErrorEventArgs e)
        {
            if (e == null || e.P0 == null)
            {
                return;
            }

            string message = $"{e.P0.ErrorCode}: {e.P0.ErrorMessage}";
            System.Diagnostics.Debug.WriteLine(message);

            this.OnSessionError?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs e)
        {
            this.OnLocateAnchorsCompleted?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_SessionUpdated(object sender, SessionUpdatedEventArgs e)
        {
            float createScanProgress = Math.Min(e.P0.Status.RecommendedForCreateProgress, 1);

            System.Diagnostics.Debug.WriteLine($"Create scan progress: {createScanProgress:0%}");

            this.CreateScanningProgressValue = createScanProgress;

            this.OnSessionUpdated?.Invoke(sender, e);
        }

        private void SpatialCloudSession_LogDebug(object sender, LogDebugEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.P0.Message))
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine(e.P0.Message);

            this.OnLogDebug?.Invoke(sender, e);
        }

        public async Task<string> SendAnchorIdAsync(string anchorId)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                StringContent content = new StringContent(anchorId);
                HttpResponseMessage response = await client.PostAsync(AccountDetails.AnchorSharingServiceUrl, content);

                response.EnsureSuccessStatusCode();

                string anchorNumber = await response.Content.ReadAsStringAsync();

                return anchorNumber;
            }
        }

        public async Task<string> RetrieveAnchorIdAsync(string anchorNumber)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    HttpResponseMessage httpResponse = await client.GetAsync($"{AccountDetails.AnchorSharingServiceUrl}/{anchorNumber}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        string anchorId = await httpResponse.Content.ReadAsStringAsync();

                        return anchorId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return "Not Found";
        }

        public async Task<string> RetrieveLastAnchorId()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    HttpResponseMessage httpResponse = await client.GetAsync($"{AccountDetails.AnchorSharingServiceUrl}/last").ConfigureAwait(false);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        string anchorId = await httpResponse.Content.ReadAsStringAsync();

                        return anchorId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return "Not Found";
        }
    }
}