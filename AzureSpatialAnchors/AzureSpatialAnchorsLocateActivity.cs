using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;

namespace AzureSpatialAnchors
{
    [Activity(Label = "AzureSpatialAnchorsLocateActivity")]
    public class AzureSpatialAnchorsLocateActivity : AppCompatActivity
    {
        private CloudSpatialAnchorSession cloudSession;
        private static Material foundColor;
        private readonly object renderLock = new object();
        private readonly object progressLock = new object();

        private static Material readyColor;

        private TrackingState lastTrackingState = TrackingState.Stopped;
        private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;

        private ArFragment arFragment;

        private Button exitButton;
        private Button locateButton;

        private ArSceneView sceneView;

        private TextView textView;
        private EditText anchorNumInput;
        private AzureSpatialAnchorsManager cloudAnchorManager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.SetContentView(Resource.Layout.locate_anchors);

            this.arFragment = (ArFragment)this.SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);

            this.sceneView = this.arFragment.ArSceneView;
            Scene scene = this.sceneView.Scene;
            scene.Update += (_, args) =>
            {
                // Pass frames to Spatial Anchors for processing.
                this.cloudAnchorManager?.Update(this.sceneView.ArFrame);
            };
            this.exitButton = (Button)this.FindViewById(Resource.Id.backButton);
            this.exitButton.Click += this.OnExitDemoClicked;
            this.locateButton = (Button)this.FindViewById(Resource.Id.locateButton);
            this.locateButton.Click += this.OnLocateButtonClicked;
            this.textView = (TextView)this.FindViewById(Resource.Id.statusText);
            this.textView.Visibility = ViewStates.Visible;
            this.anchorNumInput = (EditText)this.FindViewById(Resource.Id.anchorNumText);

            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Purple)).GetAsync().ContinueWith(materialTask =>
            {
                readyColor = (Material)materialTask.Result;
                foundColor = readyColor;
            });

        }

        private void OnExitDemoClicked(object sender, EventArgs e)
        {
            lock (this.renderLock)
            {
                this.DestroySession();

                this.Finish();
            }
        }
        private void DestroySession()
        {
            if (this.cloudSession != null)
            {
                this.cloudSession.Stop();
                this.cloudSession = null;
            }

        }
        protected override void OnResume()
        {
            base.OnResume();

            if (this.sceneView != null && this.sceneView.Session is null)
            {
                SetupSessionForSceneView(this, this.sceneView);
            }

            if (string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountId) || AccountDetails.SpatialAnchorsAccountId == "Set me"
                    || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountKey) || AccountDetails.SpatialAnchorsAccountKey == "Set me")
            {
                Toast.MakeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java\"", ToastLength.Long)
                        .Show();

                this.Finish();
            }
            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);
            this.cloudAnchorManager.StartSession();
            string num = cloudAnchorManager.RetrieveLastAnchorId().Result;
            LocateAnchor(num);

            this.RunOnUiThread(() =>
            {
                this.textView.Text = "Look for anchor";
            });
        }

        private void LocateAnchor(string anchorId)
        {
            if (!string.IsNullOrEmpty(anchorId))
            {
                Task.Run(async () =>
                {
                    

                    if (anchorId != "Not Found")
                    {
                        this.AnchorLookedUp(anchorId);
                    }
                    else
                    {
                        this.RunOnUiThread(() =>
                        {
                            this.textView.Text = "Anchor number not found or has expired.";
                        });
                    }
                });
            }
            
        }

        private void AnchorLookedUp(string anchorId)
        {
            this.DestroySession();

            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);
            this.cloudAnchorManager.OnAnchorLocated += (sender, args) =>
                this.RunOnUiThread(() =>
                {
                    CloudSpatialAnchor anchor = args.Args.Anchor;
                    LocateAnchorStatus status = args.Args.Status;

                    if (status == LocateAnchorStatus.AlreadyTracked || status == LocateAnchorStatus.Located)
                    {
                        AnchorVisual foundVisual = new AnchorVisual(anchor.LocalAnchor)
                        {
                            CloudAnchor = anchor
                        };
                        foundVisual.AnchorNode.SetParent(this.arFragment.ArSceneView.Scene);
                        string cloudAnchorIdentifier = foundVisual.CloudAnchor.Identifier;
                        foundVisual.SetColor(foundColor);
                        foundVisual.Render(this.arFragment);
                        this.textView.Text = foundVisual.CloudAnchor.AppProperties["Label"];
                    }
                });

            this.cloudAnchorManager.StartSession();
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            criteria.SetIdentifiers(new string[] { anchorId });
            this.cloudAnchorManager.StartLocating(criteria);
        }

        private void SetupSessionForSceneView(Context context, ArSceneView sceneView)
        {
            try
            {
                Session session = new Session(context);
                Config config = new Config(session);
                config.SetUpdateMode(Config.UpdateMode.LatestCameraImage);
                session.Configure(config);
                sceneView.SetupSession(session);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("ASADemo: ", ex.ToString());
            }

        }

        public void OnLocateButtonClicked(object sender, EventArgs args)
        {
            this.textView.Text = "Enter an anchor number and press locate";
            string inputVal = this.anchorNumInput.Text;
            var anchorId = this.cloudAnchorManager.RetrieveAnchorIdAsync(inputVal).Result;
            LocateAnchor(anchorId);
        }
    }
}