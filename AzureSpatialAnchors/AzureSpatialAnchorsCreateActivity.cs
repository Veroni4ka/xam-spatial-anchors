using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AzureSpatialAnchors
{
    [Activity(Label = "AzureSpatialAnchorsCreateActivity")]
    public class AzureSpatialAnchorsCreateActivity : AppCompatActivity
    {
        private CloudSpatialAnchorSession cloudSession;
        private static Material failedColor;
        private static Material foundColor;

        private static Material readyColor;

        private static Material savedColor;

        private readonly ConcurrentDictionary<string, AnchorVisual> anchorVisuals = new ConcurrentDictionary<string, AnchorVisual>();

        private readonly object renderLock = new object();
        private TrackingState lastTrackingState = TrackingState.Stopped;
        private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;

        private string anchorId = string.Empty;

        private EditText anchorNumInput;

        private ArFragment arFragment;

        private Button createButton;

        private TextView editTextInfo;

        private Button exitButton;

        private string feedbackText;

        private Button locateButton;

        private ArSceneView sceneView;

        private TextView textView;

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
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.SetContentView(Resource.Layout.create_anchors);

            this.arFragment = (ArFragment)this.SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            this.arFragment.TapArPlane += (sender, args) => this.OnTapArPlaneListener(args.HitResult, args.Plane, args.MotionEvent);

            this.sceneView = this.arFragment.ArSceneView;

            this.exitButton = (Button)this.FindViewById(Resource.Id.backButton);
            this.exitButton.Click += this.OnExitDemoClicked;
            this.textView = (TextView)this.FindViewById(Resource.Id.statusText);
            this.textView.Visibility = ViewStates.Visible;
            this.textView.Text = "Scan your environment and place an anchor";
            this.DestroySession();

            initializeSession(this.sceneView.Session);
            cloudSession.Start();

            Scene scene = this.sceneView.Scene;
            scene.Update += (_, args) =>
            {
                if (this.sceneView.ArFrame.Camera.TrackingState != this.lastTrackingState
                || this.sceneView.ArFrame.Camera.TrackingFailureReason != this.lastTrackingFailureReason)
                {
                    this.lastTrackingState = this.sceneView.ArFrame.Camera.TrackingState;
                    this.lastTrackingFailureReason = this.sceneView.ArFrame.Camera.TrackingFailureReason;
                    System.Diagnostics.Debug.WriteLine($"Tracker state changed: {this.lastTrackingState}, {this.lastTrackingFailureReason}.");
                }

                Task.Run(() => this.cloudSession.ProcessFrame(this.sceneView.ArFrame));
            };

            // Initialize the colors.
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Red)).GetAsync().ContinueWith(materialTask => failedColor = (Material)materialTask.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Green)).GetAsync().ContinueWith(materialTask => savedColor = (Material)materialTask.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Yellow)).GetAsync().ContinueWith(materialTask =>
            {
                readyColor = (Material)materialTask.Result;
                foundColor = readyColor;
            });

            
        }

        private void OnTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            throw new NotImplementedException();
        }

        private void OnExitDemoClicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        
        private void TransitionToSaving(AnchorVisual visual)
        {
            Log.Debug("ASADemo:", "transition to saving");
            Log.Debug("ASADemo", "creating anchor");
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            visual.CloudAnchor = cloudAnchor;
            cloudAnchor.LocalAnchor = visual.LocalAnchor;

            this.cloudSession.CreateAnchorAsync(cloudAnchor);
            Task.Run(() =>
                {
                    try
                    {
                        CloudSpatialAnchor anchor = cloudAnchor;

                        string anchorId = anchor.Identifier;
                        Log.Debug("ASADemo:", "created anchor: " + anchorId);
                        visual.SetColor(savedColor);
                        this.anchorVisuals[anchorId] = visual;
                        this.anchorVisuals.TryRemove(string.Empty, out _);

                        Log.Debug("ASADemo", "recording anchor with web service");
                        Log.Debug("ASADemo", "anchorId: " + anchorId);
                    }
                    catch (CloudSpatialException ex)
                    {
                        this.CreateAnchorExceptionCompletion($"{ex.Message}, {ex.ErrorCode}");
                    }
                    catch (Exception ex)
                    {
                        this.CreateAnchorExceptionCompletion(ex.Message);
                        visual.SetColor(failedColor);
                    }
                });
        }

        private void CreateAnchorExceptionCompletion(string message)
        {
            this.textView.Text = message;
            this.cloudSession.Stop();
            this.cloudSession = null;
        }
        private void DestroySession()
        {
            if (this.cloudSession != null)
            {
                this.cloudSession.Stop();
                this.cloudSession = null;
            }

        }
    }
}