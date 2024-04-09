using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VideoTimeLine
{
    internal class VerticalScrollBehavior : Behavior<ScrollViewer>
    {
        static readonly List<ScrollViewer> scrollers = new();

        protected override void OnAttached()
        {
            base.OnAttached();
            scrollers.Add(AssociatedObject);
            AssociatedObject.ScrollChanged += ScrollChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (scrollers.Remove(AssociatedObject))
            {
                AssociatedObject.ScrollChanged -= ScrollChanged;
            }
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            for(int cnt = 0;  cnt < scrollers.Count; cnt++)
            {
                if (scrollers[cnt] == sender) continue;
                Debug.WriteLine(cnt+((ScrollViewer)sender).Name);
                scrollers[cnt].ScrollToVerticalOffset(e.VerticalOffset);
            }
        }
    }

    internal class UpdateAction : TriggerAction<TextBox>
    {
        static readonly Dictionary<string, List<BindingExpression>> BindingCollection = new();

        BindingExpression? binding = null;

        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(nameof(Key), typeof(string), typeof(UpdateAction), new UIPropertyMetadata(""));


        protected override void OnAttached()
        {
            base.OnAttached();
            if (!BindingCollection.ContainsKey(Key))
            {
                BindingCollection.Add(Key, new());
            }
            if (AssociatedObject.GetBindingExpression(TextBox.TextProperty) is BindingExpression be)
            {
                binding = be;
                BindingCollection[Key].Add(be);
            }
            this.AssociatedObject.Unloaded += (s, e) =>
            {
                if (binding is not null)
                {
                    BindingCollection[Key].Remove(binding);
                    binding = null;
                }
            };
            // AssociatedObject.AddHandler(Binding.SourceUpdatedEvent, new MouseButtonEventHandler(ReleaseAtUp), true);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (binding is not null)
            {
                BindingCollection[Key].Remove(binding);
                binding = null;
            }
        }

        protected override void Invoke(object parameter)
        {
            foreach (BindingExpression bind in BindingCollection[Key])
            {
                if (bind == binding) continue;
                bind.ValidateWithoutUpdate();
            }
        }
    }
}
