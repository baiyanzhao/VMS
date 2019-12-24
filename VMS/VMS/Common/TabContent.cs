﻿// TabContent.cs, version 1.2
// The code in this file is Copyright (c) Ivan Krivyakov
// See http://www.ikriv.com/legal.php for more information
//
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace IKriv.Windows.Controls.Behaviors
{
    /// <summary>
    /// Attached properties for persistent tab control
    /// </summary>
    /// <remarks>By default WPF TabControl bound to an ItemsSource destroys visual state of invisible tabs. 
    /// Set ikriv:TabContent.IsCached="True" to preserve visual state of each tab.
    /// </remarks>
    public static class TabContent
    {
        public static bool GetIsCached(DependencyObject o)
        {
            return (bool)o?.GetValue(IsCachedProperty);
        }

        public static void SetIsCached(DependencyObject o, bool value)
        {
            o?.SetValue(IsCachedProperty, value);
        }

        /// <summary>
        /// Controls whether tab content is cached or not
        /// </summary>
        /// <remarks>When TabContent.IsCached is true, visual state of each tab is preserved (cached), even when the tab is hidden</remarks>
        public static readonly DependencyProperty IsCachedProperty =
            DependencyProperty.RegisterAttached("IsCached", typeof(bool), typeof(TabContent), new UIPropertyMetadata(false, OnIsCachedChanged));


        public static DataTemplate GetTemplate(DependencyObject o)
        {
            return (DataTemplate)o?.GetValue(TemplateProperty);
        }

        public static void SetTemplate(DependencyObject o, DataTemplate value)
        {
            o?.SetValue(TemplateProperty, value);
        }

        /// <summary>
        /// Used instead of TabControl.ContentTemplate for cached tabs
        /// </summary>
        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.RegisterAttached("Template", typeof(DataTemplate), typeof(TabContent), new UIPropertyMetadata(null));


        public static DataTemplateSelector GetTemplateSelector(DependencyObject o)
        {
            return (DataTemplateSelector)o?.GetValue(TemplateSelectorProperty);
        }

        public static void SetTemplateSelector(DependencyObject o, DataTemplateSelector value)
        {
            o?.SetValue(TemplateSelectorProperty, value);
        }

        /// <summary>
        /// Used instead of TabControl.ContentTemplateSelector for cached tabs
        /// </summary>
        public static readonly DependencyProperty TemplateSelectorProperty =
            DependencyProperty.RegisterAttached("TemplateSelector", typeof(DataTemplateSelector), typeof(TabContent), new UIPropertyMetadata(null));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TabControl GetInternalTabControl(DependencyObject o)
        {
            return (TabControl)o?.GetValue(InternalTabControlProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalTabControl(DependencyObject o, TabControl value)
        {
            o?.SetValue(InternalTabControlProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalTabControl.  This enables animation, styling, binding, etc...
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty InternalTabControlProperty =
            DependencyProperty.RegisterAttached("InternalTabControl", typeof(TabControl), typeof(TabContent), new UIPropertyMetadata(null, OnInternalTabControlChanged));


        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ContentControl GetInternalCachedContent(DependencyObject o)
        {
            return (ContentControl)o?.GetValue(InternalCachedContentProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalCachedContent(DependencyObject o, ContentControl value)
        {
            o?.SetValue(InternalCachedContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalCachedContent.  This enables animation, styling, binding, etc...
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DependencyProperty InternalCachedContentProperty =
            DependencyProperty.RegisterAttached("InternalCachedContent", typeof(ContentControl), typeof(TabContent), new UIPropertyMetadata(null));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object GetInternalContentManager(DependencyObject o)
        {
            return (object)o?.GetValue(InternalContentManagerProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetInternalContentManager(DependencyObject o, object value)
        {
            o?.SetValue(InternalContentManagerProperty, value);
        }

        // Using a DependencyProperty as the backing store for InternalContentManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InternalContentManagerProperty =
            DependencyProperty.RegisterAttached("InternalContentManager", typeof(object), typeof(TabContent), new UIPropertyMetadata(null));

        private static void OnIsCachedChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            if (o == null) return;

			if(!(o is TabControl tabControl))
			{
				throw new InvalidOperationException("Cannot set TabContent.IsCached on object of type " + args.NewValue.GetType().Name +
					". Only objects of type TabControl can have TabContent.IsCached property.");
			}

			bool newValue = (bool)args.NewValue;

            if (!newValue)
            {
                if (args.OldValue != null && ((bool)args.OldValue))
                {
                    throw new NotImplementedException("Cannot change TabContent.IsCached from True to False. Turning tab caching off is not implemented");
                }

                return;
            }

            EnsureContentTemplateIsNull(tabControl);
            tabControl.ContentTemplate = CreateContentTemplate();
            EnsureContentTemplateIsNotModified(tabControl);
        }

        private static DataTemplate CreateContentTemplate()
        {
            const string xaml = 
                "<DataTemplate><Border b:TabContent.InternalTabControl=\"{Binding RelativeSource={RelativeSource AncestorType=TabControl}}\" /></DataTemplate>";

			var context = new ParserContext
			{
				XamlTypeMapper = new XamlTypeMapper(Array.Empty<string>())
			};
			context.XamlTypeMapper.AddMappingProcessingInstruction("b", typeof(TabContent).Namespace, typeof(TabContent).Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("b", "b");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }

        private static void OnInternalTabControlChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            if (o == null) return;

			if(!(o is Decorator container))
			{
				var message = "Cannot set TabContent.InternalTabControl on object of type " + o.GetType().Name +
					". Only controls that derive from Decorator, such as Border can have a TabContent.InternalTabControl.";
				throw new InvalidOperationException(message);
			}

			if (args.NewValue == null) return;
            if (!(args.NewValue is TabControl))
            {
                throw new InvalidOperationException("Value of TabContent.InternalTabControl cannot be of type " + args.NewValue.GetType().Name +", it must be of type TabControl");
            }

            var tabControl = (TabControl)args.NewValue;
            var contentManager = GetContentManager(tabControl, container);
            contentManager.UpdateSelectedTab();
        }

        private static ContentManager GetContentManager(TabControl tabControl, Decorator container)
        {
            var contentManager = (ContentManager)GetInternalContentManager(tabControl);
            if (contentManager != null)
            {
                /*
                 * Content manager already exists for the tab control. This means that tab content template is applied 
                 * again, and new instance of the Border control (container) has been created. The old container 
                 * referenced by the content manager is no longer visible and needs to be replaced
                 */
                contentManager.ReplaceContainer(container);
            }
            else
            {
                // create content manager for the first time
                contentManager = new ContentManager(tabControl, container);
                SetInternalContentManager(tabControl, contentManager);
            }

            return contentManager;
        }

        private static void EnsureContentTemplateIsNull(TabControl tabControl)
        {
            if (tabControl.ContentTemplate != null)
            {
                throw new InvalidOperationException("TabControl.ContentTemplate value is not null. If TabContent.IsCached is True, use TabContent.Template instead of ContentTemplate");
            }
        }

        private static void EnsureContentTemplateIsNotModified(TabControl tabControl)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(TabControl.ContentTemplateProperty, typeof(TabControl));
            descriptor.AddValueChanged(tabControl, (sender, args) =>
                {
                    throw new InvalidOperationException("Cannot assign to TabControl.ContentTemplate when TabContent.IsCached is True. Use TabContent.Template instead");
                });
        }

        public class ContentManager
        {
			readonly TabControl _tabControl;
            Decorator _border;

            public ContentManager(TabControl tabControl, Decorator border)
            {
                _tabControl = tabControl;
                _border = border;
                if(tabControl != null)
                {
                    _tabControl.SelectionChanged += (sender, args) => { UpdateSelectedTab(); };
                }
            }

            public void ReplaceContainer(Decorator newBorder)
            {
                if (Object.ReferenceEquals(_border, newBorder)) return;

                _border.Child = null; // detach any tab content that old border may hold
                _border = newBorder;
            }

            public void UpdateSelectedTab()
            {
                _border.Child = GetCurrentContent();
            }

            private ContentControl GetCurrentContent()
            {
                var item = _tabControl.SelectedItem;
                if (item == null) return null;

                var tabItem = _tabControl.ItemContainerGenerator.ContainerFromItem(item);
                if (tabItem == null) return null;

                var cachedContent = TabContent.GetInternalCachedContent(tabItem);
                if (cachedContent == null)
                {
                    cachedContent = new ContentControl 
                    { 
                        DataContext = item,
                        ContentTemplate = TabContent.GetTemplate(_tabControl), 
                        ContentTemplateSelector = TabContent.GetTemplateSelector(_tabControl)
                    };
                
                    cachedContent.SetBinding(ContentControl.ContentProperty, new Binding());
                    TabContent.SetInternalCachedContent(tabItem, cachedContent);
                }

                return cachedContent;
            }
        }
    }
}
