using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;

namespace com.aa.tvshows.Helper
{
    public class StreamableLinksAdapter : BaseAdapter
    {
        private List<StreamingUri> links;
        
        public StreamableLinksAdapter(List<StreamingUri> links)
        {
            this.links = links;
        }

        public override int Count => links.Count;

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_suggestions_list, parent, false);
            }

            if (links[position] is StreamingUri item)
            {
                var titleTV = convertView.FindViewById<AppCompatTextView>(Resource.Id.search_suggestion_text);
                titleTV.Text = string.Format("{0}: {1} - {2}", position + 1, item.StreamingQuality, item.StreamingUrl.Host);
            }
            return convertView;
        }
    }
}