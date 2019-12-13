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

namespace com.aa.tvshows.Helper
{
    public class Error
    {
        public Error ErrorInstance { get => GetInstance(); }

        private Error GetInstance()
        {
            return new Error();
        }

        public void SetInstance()
        {

        }

        public void ShowErrorTip(string message)
        {

        }

        public void ShowErrorSnack(string message)
        {

        }
    }
}