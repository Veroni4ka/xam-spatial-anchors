﻿using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;

namespace AzureSpatialAnchors
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.activity_main);
            Button createSharedButton = this.FindViewById<Button>(Resource.Id.arCreateShared);
            createSharedButton.Click += this.OnCreateSharedClick;
            Button LocateSharedButton = this.FindViewById<Button>(Resource.Id.arLocateShared);
            LocateSharedButton.Click += this.OnLocateSharedClick;
        }

        private void OnCreateSharedClick(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(AzureSpatialAnchorsCreateActivity));
            this.StartActivity(intent);
        }

        private void OnLocateSharedClick(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(AzureSpatialAnchorsLocateActivity));
            this.StartActivity(intent);
        }

	}
}

