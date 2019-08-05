using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Util;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Config = Google.AR.Core.Config;

namespace AzureSpatialAnchors
{
    [Activity(Label = "AzureSpatialAnchorsCreateActivity")]
    public class AzureSpatialAnchorsCreateActivity : AppCompatActivity
    {
        private CloudSpatialAnchorSession cloudSession;
        private static Material failedColor;
        private static Material foundColor;
        private readonly object renderLock = new object();
        private readonly object progressLock = new object();

        private static Material readyColor;

        private static Material savedColor;
        private TextView scanProgressText;
        private readonly ConcurrentDictionary<string, AnchorVisual> anchorVisuals = new ConcurrentDictionary<string, AnchorVisual>();

        private TrackingState lastTrackingState = TrackingState.Stopped;
        private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;

        private ArFragment arFragment;

        private Button exitButton;
        private string feedbackText;
        private bool enoughDataForSaving;

        private ArSceneView sceneView;

        private TextView textView;
        private AzureSpatialAnchorsManager cloudAnchorManager;
        private string anchorID;

        private void initializeSession()
        {
            this.textView.Text = "Scan your environment and place an anchor";
            this.DestroySession();

            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);

            this.cloudAnchorManager.OnSessionUpdated += (_, sessionUpdateArgs) =>
            {
                float progress = sessionUpdateArgs.P0.Status.RecommendedForCreateProgress;
                this.enoughDataForSaving = progress >= 1.0;
                lock (this.progressLock)
                {
                        this.RunOnUiThread(() =>
                        {
                            this.scanProgressText.Text = $"Scan progress is {Math.Min(1.0f, progress):0%}";
                        });

                        if (this.enoughDataForSaving)
                        {
                            // Enable the save button
                            this.RunOnUiThread(() =>
                            {
                                this.textView.Text = "Ready to save";
                            });
                        }
                }
            };

            this.cloudAnchorManager.StartSession();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.SetContentView(Resource.Layout.create_anchors);

            this.arFragment = (ArFragment)this.SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            this.arFragment.TapArPlane += (sender, args) => this.OnTapArPlaneListener(args.HitResult, args.Plane, args.MotionEvent);

            this.sceneView = this.arFragment.ArSceneView;
            Scene scene = this.sceneView.Scene;
            scene.Update += (_, args) =>
            {
                // Pass frames to Spatial Anchors for processing.
                this.cloudAnchorManager?.Update(this.sceneView.ArFrame);
            };
            this.exitButton = (Button)this.FindViewById(Resource.Id.backButton);
            this.exitButton.Click += this.OnExitDemoClicked;
            this.textView = (TextView)this.FindViewById(Resource.Id.statusText);
            this.textView.Visibility = ViewStates.Visible;
            this.scanProgressText = (TextView)this.FindViewById(Resource.Id.scanProgressText);

            // Initialize the colors.
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Red)).GetAsync().ContinueWith(materialTask => failedColor = (Material)materialTask.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Green)).GetAsync().ContinueWith(materialTask => savedColor = (Material)materialTask.Result);
            MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Yellow)).GetAsync().ContinueWith(materialTask =>
            {
                readyColor = (Material)materialTask.Result;
                foundColor = readyColor;
            });
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

                this.initializeSession();
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

        private void OnTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            this.CreateAnchor(hitResult);
        }

        private Anchor CreateAnchor(HitResult hitResult)
        {
            AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
            visual.SetColor(readyColor);
            visual.Render(this.arFragment);
            this.anchorVisuals[string.Empty] = visual;

            this.RunOnUiThread(() =>
            {
                this.scanProgressText.Visibility = ViewStates.Visible;
                if (this.enoughDataForSaving)
                {
                    if (visual == null)
                    {
                        return;
                    }

                    if (!this.enoughDataForSaving)
                    {
                        return;
                    }

                    this.RunOnUiThread(() => this.exitButton.Visibility = ViewStates.Gone);

                    this.SetupLocalCloudAnchor(visual);

                    Task.Run(async () =>
                    {
                        try
                        {
                            CloudSpatialAnchor result = await this.cloudAnchorManager.CreateAnchorAsync(visual.CloudAnchor);
                            this.AnchorSaveSuccess(result);
                        }
                        catch (CloudSpatialException ex)
                        {
                            this.AnchorSaveFailed($"{ex.Message}, {ex.ErrorCode}");
                        }
                        catch (Exception ex)
                        {
                            this.AnchorSaveFailed(ex.Message);
                        }
                    });

                    lock (this.progressLock)
                    {
                        this.RunOnUiThread(() =>
                        {
                            this.scanProgressText.Visibility = ViewStates.Gone;
                            this.scanProgressText.Text = string.Empty;
                            this.textView.Text = "Saving cloud anchor...";
                        });
                    }
                }
                else
                {
                    this.textView.Text = "Move around the anchor";
                }
            });
            this.exitButton.Visibility = ViewStates.Visible;
            return visual.LocalAnchor;
        }

        private void AnchorSaveFailed(string message)
        {
            this.RunOnUiThread(() => this.textView.Text = message);
            AnchorVisual visual = this.anchorVisuals[string.Empty];
            visual.SetColor(failedColor);
        }
        private void AnchorSaveSuccess(CloudSpatialAnchor result)
        {
            this.anchorID = result.Identifier;
            Log.Debug("ASADemo:", "created anchor: " + this.anchorID);

            AnchorVisual visual = this.anchorVisuals[string.Empty];
            visual.SetColor(savedColor);
            this.anchorVisuals[this.anchorID] = visual;
            this.anchorVisuals.TryRemove(string.Empty, out _);
            var anchorNum = this.cloudAnchorManager.SendAnchorIdAsync(this.anchorID).Result;

            this.RunOnUiThread(() =>
            {
                this.textView.Text = String.Format("Created a cloud anchor with ID={0}", anchorNum);
            });

        }

        private void SetupLocalCloudAnchor(AnchorVisual visual)
        {
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor
            {
                LocalAnchor = visual.LocalAnchor
            };
            cloudAnchor.AppProperties.Add("Label", "Congrats! You found a spatial anchor");
            visual.CloudAnchor = cloudAnchor;

            Date now = new Date();
            Calendar cal = Calendar.Instance;
            cal.Time = now;
            cal.Add(CalendarField.Date, 7);
            Date oneWeekFromNow = cal.Time;
            cloudAnchor.Expiration = oneWeekFromNow;
        }

        private void OnExitDemoClicked(object sender, EventArgs e)
        {
            lock (this.renderLock)
            {
                this.DestroySession();

                this.Finish();
            }
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