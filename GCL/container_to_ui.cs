using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Reflection;
using System.Collections;
using System.Windows.Data;

using GCL.MP;
using GCL.Containers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace gcl
{
    public class container_to_ui
    {
        public class Attributes
        {
            public class NotUIVisible : Attribute { }
            public class NotUIEditable : Attribute { }
        }
        public class Delegates
        {
            public delegate FrameworkElement PropertyToUIElementConversion<T>(PropertyInfo property, T item = null)
                where T : class, new();
            public delegate FrameworkElementFactory PropertyToUIElementFactoryConversion(PropertyInfo property);
            public delegate T objectFactory<T>();
        }

        static public TreeView to_treeview<T>(ref T tree) // such like a List<HierarchicalData<T>>
            where T : class, new()
        {
            if (tree == null)
                throw new NullReferenceException("gcl.container_to_ui.to_treeview");
            if (!type_info.IsHierarchyTree<T>())
                throw new InvalidOperationException("gcl.container_to_ui.to_treeview : HasHierarchyTree");

            PropertyInfo    recProperty = type_info.GetRecursivePropertyInfo_Parent(typeof(T));
            var             items = new Dictionary<T, TreeViewItem>();
            TreeView        view = new TreeView { AllowDrop = true };

            items.Add(tree, new TreeViewItem { Header = tree.ToString(), DataContext = tree });
            foreach (var elem in tree.GetType().GetProperty("childrens").GetValue(tree) as IEnumerable)
            {
                items.Add(elem as T, new TreeViewItem { Header = elem.ToString(), DataContext = elem });
            }

            foreach (var elem in items)
            {
                object tmp = null;
               if ((tmp = recProperty.GetValue(elem.Key)) == null)
                { // is Parent
                    view.Items.Add(elem.Value);
                }
                else
                { // has Parent
                    items[tmp as T].Items.Add(elem.Value);
                }
            }

            return view;
        }

        static public GridView GridView_of(Type EntityType)
        {   // todo : Join with GetMembers ?
            return GridView_of(from property in EntityType.GetProperties()
                               where !System.Attribute.IsDefined(property, typeof(Attributes.NotUIVisible))
                               select property.Name);
        }
        static public GridView GridView_of(IEnumerable<String> colsName)
        {
            var view = new GridView();
            foreach (var colName in colsName)
            {
                view.Columns.Add(new GridViewColumn
                {
                    Header = colName,
                    DisplayMemberBinding = new Binding(colName),
                    Width = double.NaN
                });
            }
            return view;
        }

        static private void SetListViewColums<T_Entity>(ref ListView listView)
        {
            SetListViewColums(typeof(T_Entity), ref listView);
        }
        static private void SetListViewColums(Type EntityType, IEnumerable<string> propertiesName, ref ListView listView)
        {
            listView.View = GridView_of(propertiesName);
            listView.ItemContainerStyle = new System.Windows.Style();
            listView.ItemContainerStyle.TargetType = typeof(ListViewItem);
        }
        static private void SetListViewColums(Type EntityType, ref ListView listView)
        {
            listView.View = GridView_of(EntityType);
            listView.ItemContainerStyle = new System.Windows.Style();
            listView.ItemContainerStyle.TargetType = typeof(ListViewItem);
        }
        

        static public ListView ToListView<T_Entity>()
             where T_Entity : class, new()
        {
            return ToListView(typeof(T_Entity));
        }
        static public ListView ToListView(Type entityType)
        {
            ListView listView = new ListView
            {
                Name = string.Format("listView_{0}", GCL.Converters.StringHelper.CleanString(entityType.ToString()))
            };
            SetListViewColums(entityType, ref listView);
            return listView;
        }


        // FIXME :
        //static private FrameworkElementFactory      ToStringToUIElementFactory<T>()
        //{
        //    return ToStringToUIElementFactory(typeof(T));
        //}
        //static private FrameworkElementFactory      ToStringToUIElementFactory(Type type)
        //{
        //    FrameworkElementFactory elemFactory = new FrameworkElementFactory(typeof(Label));
        //    elemFactory.SetBinding(Label.ContentProperty, new Binding());
        //    elemFactory.SetValue(Label.IsEnabledProperty, false);
        //    elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
        //    return elemFactory;
        //}
        //static private FrameworkElement             PropertyToUIElement<T>(PropertyInfo property, T dummy = null)
        //    where T : class, new()
        //{
        //    FrameworkElement elem;

        //    if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
        //        throw new InvalidOperationException("PropertyToUIElement<T> : Attributes.NotUIVisible is set");

        //    if (property.PropertyType == typeof(bool))
        //    {
        //        elem = new CheckBox();
        //        elem.SetBinding(CheckBox.IsCheckedProperty, new Binding(property.Name));
        //    }
        //    else if (property.PropertyType.IsEnum)
        //    {
        //        elem = new ComboBox();
        //        elem.SetValue(ComboBox.ItemsSourceProperty, property.PropertyType.GetEnumValues());
        //        elem.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
        //    }
        //    else
        //    {
        //        elem = new TextBox();
        //        if (property.PropertyType == typeof(int))
        //            elem.AddHandler(TextBox.PreviewTextInputEvent,
        //            new TextCompositionEventHandler((sender, e) =>
        //            {
        //                if (!char.IsDigit(e.Text, e.Text.Length - 1))
        //                    e.Handled = true;
        //            }));
        //        elem.SetBinding(TextBox.TextProperty, new Binding(property.Name));
        //    }

        //    elem.SetValue(ListViewItem.WidthProperty, Double.NaN);
        //    elem.SetValue(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
        //    elem.SetValue(FrameworkElement.IsEnabledProperty, !(Attribute.IsDefined(property, typeof(Attributes.NotUIEditable))));
        //    return elem;
        //}
        //static private FrameworkElementFactory      PropertyToUIElementFactory(PropertyInfo property)
        //{
        //    FrameworkElementFactory elemFactory;

        //    if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
        //        throw new InvalidOperationException("PropertyToUIElementFactory : Attributes.NotUIVisible is set");

        //    if (property.PropertyType == typeof(bool))
        //    {
        //        elemFactory = new FrameworkElementFactory(typeof(CheckBox));
        //        elemFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding(property.Name));
        //        elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
        //    }
        //    else if (property.PropertyType.IsEnum)
        //    {
        //        elemFactory = new FrameworkElementFactory(typeof(ComboBox));
        //        elemFactory.SetValue(ComboBox.ItemsSourceProperty, property.PropertyType.GetEnumValues());
        //        elemFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
        //        elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
        //    }
        //    else if (typeof(ICollection<>).IsAssignableFrom(property.PropertyType))
        //    {
        //        throw new NotImplementedException("PropertyToUIElementFactory : collection");
        //    }
        //    else
        //    {
        //        elemFactory = new FrameworkElementFactory(typeof(TextBox));
        //        if (property.PropertyType == typeof(int))
        //            elemFactory.AddHandler(TextBox.PreviewTextInputEvent,
        //            new TextCompositionEventHandler((sender, e) =>
        //            {
        //                if (!char.IsDigit(e.Text, e.Text.Length - 1))
        //                    e.Handled = true;
        //            }));
        //        elemFactory.SetBinding(TextBox.TextProperty, new Binding(property.Name));
        //        elemFactory.SetValue(ListViewItem.WidthProperty, 200D);
        //    }

        //    // elemFactory.SetValue(ListViewItem.WidthProperty, col.Width); // => No effect
        //    elemFactory.SetValue(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
        //    elemFactory.SetValue(FrameworkElement.IsEnabledProperty, !Attribute.IsDefined(property, typeof(Attributes.NotUIEditable)));
        //    return elemFactory;
        //}
        //static private Delegates.PropertyToUIElementFactoryConversion PropertyToUIElementFactoryAdapter()
        //{
        //    return (PropertyInfo property) => { return PropertyToUIElementFactory(property); };
        //}
        //static public ListView ToEditableListView<T_Entity>()
        //                 where T_Entity : class, new()
        //{
        //    var EntityType = typeof(T_Entity);
        //    var listView = new ListView
        //    {
        //        Name = string.Format("listBox_{0}", StringHelper.CleanString(EntityType.ToString())),
        //        ItemContainerStyle = new System.Windows.Style { TargetType = typeof(ListViewItem) }
        //    };

        //    var gridView = new GridView();
        //    #region Object ToString
        //    {
        //        GridViewColumn col = new GridViewColumn
        //        {
        //            Header = "Object",
        //            Width = Double.NaN,
        //            CellTemplate = new DataTemplate
        //            {
        //                VisualTree = ToStringToUIElementFactory(EntityType)
        //            }
        //        };
        //        gridView.Columns.Add(col);
        //    }
        //    #endregion
        //    #region GridView columns
        //    foreach (var property in EntityType.GetProperties())
        //    {
        //        if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
        //            continue;

        //        GridViewColumn col = new GridViewColumn
        //        {
        //            Header = property.Name,
        //            Width = Double.NaN,
        //            CellTemplate = new DataTemplate
        //            {
        //                VisualTree = PropertyToUIElementFactoryAdapter()(property)
        //            }
        //        };
        //        gridView.Columns.Add(col);
        //    }
        //    #endregion

        //    #region Save/Delete buttons
        //    //GridViewColumn buttonsCol = new GridViewColumn { Width = Double.NaN };

        //    //var btnsPanelFactory = SaveAndDeleteButtonspanelFactory(EntityType);
        //    //var btnDeleteFactory = btnsPanelFactory.FirstChild.NextSibling;
        //    //btnDeleteFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((sender, e) =>
        //    //{
        //    //    listView.Items.Refresh();
        //    //}));

        //    //buttonsCol.CellTemplate = new DataTemplate { VisualTree = btnsPanelFactory };
        //    //gridView.Columns.Add(buttonsCol);
        //    #endregion

        //    listView.View = gridView;

        //    return listView;
        //}
    }
}
