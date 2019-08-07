using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AzureSpatialAnchors
{
    class AccountDetails
    {
        /// <summary>
        /// The Azure Spatial Anchors account identifier.
        /// </summary>
        /// <remarks>
        /// Set this to your account id found in the Azure Portal.
        /// </remarks>
        public const string SpatialAnchorsAccountId = "";

        /// <summary>
        /// The Azure Spatial Anchors account key.
        /// Set this to your account id found in the Azure Portal.
        /// </summary>
        /// <remarks>
        /// Set this to your account key found in the Azure Portal.
        /// </remarks>
        public const string SpatialAnchorsAccountKey = "";

        /// <summary>
        /// The full URL endpoint of the anchor sharing service.
        /// </summary>
        /// <remarks>
        /// Set this to your URL created when publishing your anchor sharing service in the Sharing sample.
        /// It should end in '/api/anchors'.
        /// </remarks>
        public const string AnchorSharingServiceUrl = "";
    }
}