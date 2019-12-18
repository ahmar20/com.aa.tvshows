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
using AndroidX.AppCompat.Widget;

namespace com.aa.tvshows.Helper
{
    public class SimpleListAdapter<T> : BaseAdapter<T>
    {
        private List<T> items;
        private Context context;


        public SimpleListAdapter(Context context, List<T> items)
        {
            this.items = items;
            this.context = context;
        }

        public override T this[int position] => items.ElementAtOrDefault(position);

        public override int Count => items == null ? 0 : items.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = LayoutInflater.From(context).Inflate(Resource.Layout.search_suggestions_list, parent, false);
            }
            
            if (this[position] is SearchSuggestionsData item)
            {
                var titleTV = convertView.FindViewById<AppCompatTextView>(Resource.Id.search_suggestion_text);
                titleTV.Text = item.Title;
            }
            return convertView;
        }
    }
}