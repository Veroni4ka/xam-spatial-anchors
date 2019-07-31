using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Math;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;
using Xamarin.Essentials;

namespace AzureSpatialAnchors
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.activity_main);
            Button sharingDemoButton = this.FindViewById<Button>(Resource.Id.arCreateShared);
            sharingDemoButton.Click += this.OnCreateSharedClick;

        }

        private void OnCreateSharedClick(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(AzureSpatialAnchorsCreateActivity));
            this.StartActivity(intent);
        }

        //private void UpdateAnchor()
        //{
        //        if (this.tapExecuted)
        //        {
        //            return;
        //        }

        //        this.tapExecuted = true;

        //    AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
        //    visual.SetColor(readyColor);
        //    visual.Render(this.arFragment);
        //}

        //private void OnTapArPlaneListener(HitResult hitResult)
        //{

        //    AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
        //    visual.Render(this.arFragment, failedColor);

        //    MaterialFactory.MakeOpaqueWithColor(this, new Color(Android.Graphics.Color.Red)).GetAsync().ContinueWith(materialTask => failedColor = (Material)materialTask.Result);
        //    MainThread.BeginInvokeOnMainThread(() =>
        //        {
        //            this.anchorNode.Renderable = null;
        //            this.nodeRenderable = ShapeFactory.MakeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), failedColor);
        //            this.anchorNode.Renderable = this.nodeRenderable;
        //        });
        //}

        //private Anchor CreateAnchor(HitResult hitResult)
        //{
        //    AnchorVisual visual = new AnchorVisual(hitResult.CreateAnchor());
        //    visual.SetColor(readyColor);
        //    //visual.Render(this.arFragment);
        //    //this.anchorVisuals[string.Empty] = visual;

        //    this.RunOnUiThread(() =>
        //    {
        //        this.scanProgressText.Visibility = ViewStates.Visible;
        //        if (this.enoughDataForSaving)
        //        {
        //            this.statusText.Text = "Ready to save";
        //            this.actionButton.Text = "Save cloud anchor";
        //            this.actionButton.Visibility = ViewStates.Visible;
        //        }
        //        else
        //        {
        //            this.statusText.Text = "Move around the anchor";
        //        }
        //    });

        //    return visual.LocalAnchor;
        //}

        

	}
}

