using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using com.aa.tvshows.Helper;
using com.aa.tvshows.Models;

namespace com.aa.tvshows.Fragments
{
    /// <summary>Live search: results appear as you type, as a poster grid that keeps loading as you scroll.</summary>
    public class SearchFragment : Fragment
    {
        const int DebounceMs = 400;
        const int MinQueryLength = 2;

        EditText input;
        View clear;
        TextView hint;
        ProgressBar progress;
        RecyclerView results;
        PosterGrid grid;
        EndlessScroll endless;

        CancellationTokenSource debounce;
        string query = string.Empty;
        int nextPage = 2;
        bool hasMore;
        bool loadingMore;
        int version;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_search, container, false);
            WindowInsetsHelper.Apply(view.FindViewById<View>(Resource.Id.search_root), top: true);

            input = view.FindViewById<EditText>(Resource.Id.search_input);
            clear = view.FindViewById<View>(Resource.Id.search_clear);
            hint = view.FindViewById<TextView>(Resource.Id.search_hint);
            progress = view.FindViewById<ProgressBar>(Resource.Id.search_progress);
            results = view.FindViewById<RecyclerView>(Resource.Id.search_results);

            grid = new PosterGrid(results, Context);
            grid.Adapter.ItemClick += card => { HideKeyboard(); AppView.HandleItemShowEpisodeClick(card, Context); };
            endless = new EndlessScroll(grid.Layout);
            endless.LoadMoreTask += (s, e) => _ = LoadMoreAsync();
            results.AddOnScrollListener(endless);
            // dragging the results away from the keyboard dismisses it
            results.AddOnScrollListener(new DragDismissKeyboardListener(HideKeyboard));

            input.TextChanged += (s, e) =>
            {
                clear.Visibility = input.Text.Length > 0 ? ViewStates.Visible : ViewStates.Gone;
                ScheduleSearch(input.Text.Trim(), DebounceMs);
            };
            input.EditorAction += (s, e) =>
            {
                if (e.ActionId == ImeAction.Search)
                {
                    HideKeyboard();
                    ScheduleSearch(input.Text.Trim(), 0);
                }
                e.Handled = true;
            };
            clear.Click += (s, e) =>
            {
                input.Text = string.Empty;
                input.RequestFocus();
            };

            ShowHint(GetString(Resource.String.search_prompt));
            return view;
        }

        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);
            if (hidden)
            {
                HideKeyboard();
                return;
            }
            input?.RequestFocus();
            if (!UiMode.IsTv(Context)) ShowKeyboard();
        }

        public override void OnDestroyView()
        {
            version++;
            debounce?.Cancel();
            base.OnDestroyView();
        }

        void ScheduleSearch(string text, int delayMs)
        {
            debounce?.Cancel();
            var cts = debounce = new CancellationTokenSource();
            Task.Delay(delayMs, cts.Token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                Activity?.RunOnUiThread(() =>
                {
                    if (View != null && !cts.IsCancellationRequested) _ = SearchAsync(text);
                });
            });
        }

        async Task SearchAsync(string text)
        {
            if (text == query && grid.Adapter.ItemCount > 0) return;
            query = text;
            var current = ++version;
            grid.Adapter.Clear();
            endless.ResetState();
            hasMore = false;
            nextPage = 2;

            if (text.Length < MinQueryLength)
            {
                progress.Visibility = ViewStates.Gone;
                ShowHint(GetString(Resource.String.search_prompt));
                return;
            }

            hint.Visibility = ViewStates.Gone;
            progress.Visibility = ViewStates.Visible;
            CardPage page = null;
            try
            {
                page = await TMDbClient.SearchShows(text, 1);
            }
            catch (Exception)
            {
            }
            if (current != version || View == null) return;

            progress.Visibility = ViewStates.Gone;
            if (page == null)
            {
                ShowHint(GetString(Resource.String.internet_error_header));
                return;
            }
            if (page.Items.Count == 0)
            {
                ShowHint(GetString(Resource.String.search_no_results).Replace("%1$s", text));
                return;
            }
            grid.Adapter.Replace(page.Items);
            hasMore = page.HasMore;
        }

        async Task LoadMoreAsync()
        {
            if (!hasMore || loadingMore || string.IsNullOrEmpty(query)) return;

            loadingMore = true;
            var current = version;
            try
            {
                var page = await TMDbClient.SearchShows(query, nextPage);
                if (current != version || View == null) return;
                if (page != null)
                {
                    grid.Adapter.Add(page.Items);
                    hasMore = page.HasMore;
                    nextPage++;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                loadingMore = false;
            }
        }

        void ShowHint(string text)
        {
            hint.Text = text;
            hint.Visibility = ViewStates.Visible;
        }

        void ShowKeyboard()
        {
            input.PostDelayed(() =>
            {
                var imm = (InputMethodManager)Context?.GetSystemService(Context.InputMethodService);
                imm?.ShowSoftInput(input, ShowFlags.Implicit);
            }, 150);
        }

        void HideKeyboard()
        {
            if (input == null) return;
            var imm = (InputMethodManager)Context?.GetSystemService(Context.InputMethodService);
            imm?.HideSoftInputFromWindow(input.WindowToken, HideSoftInputFlags.None);
        }
    }

    public class DragDismissKeyboardListener : RecyclerView.OnScrollListener
    {
        readonly Action onDragStart;

        public DragDismissKeyboardListener(Action onDragStart)
        {
            this.onDragStart = onDragStart;
        }

        public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
        {
            if (newState == RecyclerView.ScrollStateDragging) onDragStart?.Invoke();
        }
    }
}
