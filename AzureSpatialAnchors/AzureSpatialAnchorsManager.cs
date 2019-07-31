using System;
using Google.AR.Core;
using Microsoft.Azure.SpatialAnchors;

namespace AzureSpatialAnchors
{
    internal class AzureSpatialAnchorsManager
    {
        private CloudSpatialAnchorSession cloudSession;
        private Session session;

        public AzureSpatialAnchorsManager(Session session)
        {
            this.session = session;
        }

        public bool CanCreateAnchor => this.CreateScanningProgressValue >= 1;
        public float CreateScanningProgressValue { get; set; } = 0;
        public event EventHandler<AnchorLocatedEventArgs> OnAnchorLocated;

        public event EventHandler<LocateAnchorsCompletedEventArgs> OnLocateAnchorsCompleted;

        public event EventHandler<LogDebugEventArgs> OnLogDebug;

        public event EventHandler<SessionErrorEventArgs> OnSessionError;

        public event EventHandler<SessionUpdatedEventArgs> OnSessionUpdated;

        private void initializeSession(Session arCoreSession)
        {
            if (this.cloudSession != null)
            {
                this.cloudSession.Close();
            }
            this.cloudSession = new CloudSpatialAnchorSession();
            this.cloudSession.Configuration.AccountKey = AccountDetails.SpatialAnchorsAccountKey;
            this.cloudSession.Configuration.AccountId = AccountDetails.SpatialAnchorsAccountId;
            this.cloudSession.Session = arCoreSession;
            this.cloudSession.LogDebug += this.SpatialCloudSession_LogDebug;
            this.cloudSession.Error += this.SpatialAnchorsSession_Error;
            this.cloudSession.AnchorLocated += this.SpatialAnchorsSession_AnchorLocated;
            this.cloudSession.LocateAnchorsCompleted += this.SpatialAnchorsSession_LocateAnchorsCompleted;
            this.cloudSession.SessionUpdated += this.SpatialAnchorsSession_SessionUpdated;
        }

        private void SpatialAnchorsSession_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs e)
        {
            this.OnLocateAnchorsCompleted?.Invoke(sender, e);
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

        private void SpatialCloudSession_LogDebug(object sender, LogDebugEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SpatialAnchorsSession_SessionUpdated(object sender, SessionUpdatedEventArgs e)
        {
            float createScanProgress = Math.Min(e.P0.Status.RecommendedForCreateProgress, 1);

            System.Diagnostics.Debug.WriteLine($"Create scan progress: {createScanProgress:0%}");

            this.CreateScanningProgressValue = createScanProgress;

            this.OnSessionUpdated?.Invoke(sender, e);
        }
        internal void Update(Frame arFrame)
        {
            throw new NotImplementedException();
        }
    }
}