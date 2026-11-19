using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Helper
{
    /// <summary>Genre chips (phone) / genre list (TV) on the Genres screen.</summary>
    public class GenreAdapter : RecyclerView.Adapter
    {
        readonly List<TmdbGenre> genres;
        int selected = -1;

        public event Action<int> ItemClick;

        public GenreAdapter(List<TmdbGenre> genres)
        {
            this.genres = genres;
        }

        public override int ItemCount => genres.Count;

        public void SetSelected(int position)
        {
            var previous = selected;
            selected = position;
            if (previous >= 0) NotifyItemChanged(previous);
            if (selected >= 0) NotifyItemChanged(selected);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_genre_chip, parent, false);
            return new GenreHolder(view, position => ItemClick?.Invoke(position));
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is GenreHolder genre)
            {
                genre.Name.Text = genres[position].Name;
                genre.ItemView.Selected = position == selected;
            }
        }
    }

    public class GenreHolder : RecyclerView.ViewHolder
    {
        public TextView Name { get; }

        public GenreHolder(View itemView, Action<int> click) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.genre_name);
            itemView.Click += (s, e) =>
            {
                var position = AdapterPosition;
                if (position >= 0) click(position);
            };
        }
    }
}
