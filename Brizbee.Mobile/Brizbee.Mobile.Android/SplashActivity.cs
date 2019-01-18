using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Brizbee.Mobile.Droid
{
    [Activity(Label = "BRIZBEE", Icon = "@mipmap/icon", MainLauncher = false, NoHistory = true, Theme = "@style/Theme.Splash",
       ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle bunsavedInstanceStatedle)
        {
            base.OnCreate(bunsavedInstanceStatedle);

            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}