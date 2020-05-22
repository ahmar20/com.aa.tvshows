using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Newtonsoft.Json;

namespace com.aa.tvshows.Helper
{
    public class ContextMenuDialog
    {
        public static ContextMenuDialog Instance { get; } = new ContextMenuDialog();

        AlertDialog Dialog { get; set; }
        AlertDialog.Builder Builder { get; set; }

        public ContextMenuDialog()
        {
        }

        public void ShowContextDialog(AppCompatActivity context, List<StreamingUri> links)
        {
            if (Dialog != null && Dialog.IsShowing) return;

            var parent = LayoutInflater.From(context).Inflate(Resource.Layout.context_dialog, null, false);
            Builder = new AlertDialog.Builder(context).SetView(parent);
            Dialog = Builder.Create();
            Dialog.SetCancelable(true);
            Dialog.SetCanceledOnTouchOutside(false);

            Dialog.DismissEvent += Dialog_DismissEvent;
            Dialog.CancelEvent += Dialog_DismissEvent;

            var copyBtn = parent.FindViewById<Button>(Resource.Id.link_copy_btn);
            copyBtn.Click += delegate
            {
                var clipBoard = (ClipboardManager)context.GetSystemService(Android.App.Service.ClipboardService);
                var clip = ClipData.NewPlainText("video link", links.FirstOrDefault()?.StreamingUrl.OriginalString);
                clipBoard.PrimaryClip = clip;
                Error.Instance.ShowErrorTip("Link successfully copied to clipboard", context);
            };
            var viewBtn = parent.FindViewById<Button>(Resource.Id.watch_video_btn);
            viewBtn.Click += delegate
            {
                if (StorageData.GetUseExternalMediaPlayerSetting())
                {
                    Intent intent = new Intent(Intent.ActionView);
                    intent.SetDataAndType(Android.Net.Uri.Parse(links.FirstOrDefault()?.StreamingUrl.OriginalString), "video/*");
                    context.StartActivity(Intent.CreateChooser(intent, "Open video using"));
                }
                else
                {
                    var intent = new Intent(context, typeof(PlayerActivity));
                    intent.PutExtra("mediaStreams", JsonConvert.SerializeObject(links));
                    context.StartActivity(intent);
                }
            };
            var saveBtn = parent.FindViewById<Button>(Resource.Id.download_video_btn);
            saveBtn.Click += delegate
            {
                // implement
                Error.Instance.ShowErrorTip("This feature is not implemented yet.", context);
            };

            Dialog.Show();
        }

        private void Dialog_DismissEvent(object sender, EventArgs e)
        {
            
        }
    }
}