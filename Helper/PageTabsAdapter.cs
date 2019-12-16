using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.ViewPager.Widget;
using Java.Lang;

namespace com.aa.tvshows.Helper
{
    public class PageTabsAdapter : FragmentStatePagerAdapter
    {
        private List<TitleFragment> Fragments { get; set; }

        public PageTabsAdapter(FragmentManager manager) : base(manager, BehaviorResumeOnlyCurrentFragment) => Fragments = new List<TitleFragment>();

        public override int Count => Fragments == null ? 0 : Fragments.Count;

        public override Fragment GetItem(int position)
        {
            return Fragments[position].Fragmnet;
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            return new Java.Lang.String(Fragments[position].Title);
        }

        public bool AddTab(params TitleFragment[] fragments)
        {
            if (Fragments == null || fragments == null) return false;

            Fragments.AddRange(fragments);
            NotifyDataSetChanged();
            return true;
        }
        public TitleFragment GetTab(int position)
        {
            if (Fragments == null) return null;

            return Fragments[position];
        }

        public Fragments.MainTabs GetTabFragment(int position)
        {
            if (Fragments == null) return null;

            return Fragments[position].Fragmnet as Fragments.MainTabs;
        }

        public bool RemoveTab(TitleFragment fragment)
        {
            if (Fragments == null || fragment == null) return false;

            Fragments.Remove(fragment);
            NotifyDataSetChanged();
            return true;
        }

        public bool RemoveTab(int position)
        {
            if (Fragments == null || position < 0 || position >= Fragments.Count) return false;

            Fragments.RemoveAt(position);
            NotifyDataSetChanged();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}