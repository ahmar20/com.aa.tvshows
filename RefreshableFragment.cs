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
using AndroidX.Interpolator.View.Animation;
using Java.Lang;

namespace com.aa.tvshows
{
    public abstract class RefreshableFragment : AndroidX.Fragment.App.Fragment
    {

        public abstract void refresh();
    }
    
}