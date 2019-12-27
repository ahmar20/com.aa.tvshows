using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;

namespace com.aa.tvshows.Helper
{
    public class LoadingViewDialog
    {
        public static LoadingViewDialog Instance { get; } = new LoadingViewDialog();

        AlertDialog Dialog { get; set; }
        AlertDialog.Builder Builder { get; set; }
        public event EventHandler OnCancelled = delegate { };

        public LoadingViewDialog()
        {
        }



        public void Show(Context context)
        {
            if (Dialog != null && Dialog.IsShowing) return;
            Builder = new AlertDialog.Builder(context).SetView(LayoutInflater.From(context).Inflate(Resource.Layout.loading_dialog, null, false));
            Dialog = Builder.Create();
            Dialog.SetCancelable(false);
            Dialog.SetCanceledOnTouchOutside(false);
            Dialog.DismissEvent += Dialog_DismissEvent;
            Dialog.CancelEvent += Dialog_DismissEvent;
            Dialog.Show();
        }

        private void Dialog_DismissEvent(object sender, EventArgs e)
        {
            OnCancelled?.Invoke(sender, e);
        }

        public void Hide()
        {
            if (Dialog == null) return;

            if (Dialog.IsShowing)
                Dialog.Cancel();
        }
    }
}