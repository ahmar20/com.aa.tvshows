using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace com.aa.tvshows.Helper
{

    //credits: David Medenjak
    public class HeaderDecoration : RecyclerView.ItemDecoration
    {

        private TextView mLayout;
        private int mLocation;
        int mHeight;
        public HeaderDecoration(RecyclerView parent, String text, int location)
        {
            mLayout = (TextView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.home_header_item, parent, false);

            mHeight = (int) parent.Context.Resources.GetDimension(Resource.Dimension.header_height);
          
            mLayout.Measure(View.MeasureSpec.MakeMeasureSpec(parent.MeasuredWidth, 0),
                    View.MeasureSpec.MakeMeasureSpec(mHeight, 0));
            mLocation = location;

            mLayout.Text = text;

        }



        public override void OnDraw(Canvas c, RecyclerView parent, RecyclerView.State state)
        {
            base.OnDraw(c, parent, state);
            //  layout basically just gets drawn on the reserved space on top of the first view
            for (int i = 0; (i < parent.ChildCount); i++)
            {
                View view = parent.GetChildAt(i);
                if ((parent.GetChildAdapterPosition(view) == mLocation))
                {
                    mLayout.Layout(parent.Left, view.Top, parent.Right, view.Top+ mHeight);

                    c.Save();
                    int height = mHeight;
                    int top = (view.Top - height);
                    c.Translate(0, top);
                    mLayout.Draw(c);
                    c.Restore();
                    break;
                }

            }

        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {

//3 is the number of columns, therefore +1 and +2 
            if ( parent.GetChildAdapterPosition(view) == mLocation
                || parent.GetChildAdapterPosition(view) == mLocation + 1
                || parent.GetChildAdapterPosition(view) == mLocation + 2
                )
            {
                outRect.Set(0, mHeight, 0, 0);
            }
            else
            {
                outRect.SetEmpty();
            }

        }
    }
}