using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using static GCL.DBEF;

namespace GCL
{
    public class UIHelper
    {
        public class Attributes
        {
            // Options
            public class NotUIVisible : Attribute
            { }
            public class NotUIEditable : Attribute
            { }
            public class FormMandatory : Attribute
            { }
            // Types
            public class UIImageData : Attribute
            { }
            public class UIFileData : Attribute
            { }
            public class UIRichText : Attribute
            { }
        }
        public class Delegates
        {
            public delegate FrameworkElement        PropertyToUIElementConversion<T>(PropertyInfo property, T item = null)
                where T : class, new();
            public delegate FrameworkElementFactory PropertyToUIElementFactoryConversion(PropertyInfo property);
            public delegate void                    OnUpdate<T_Item>(T_Item item);
            public delegate T                       objectFactory<T>();
        }

        // todo : Deprecated [?]
        static public void  SetUIElementValue<T>(UIElement elem, T value)
        {   // May throw InvalidCastException / NullReferenceException
            if (elem is ComboBox)
                (elem as ComboBox).SelectedValue = value;
            else if (elem is CheckBox)
                (elem as CheckBox).IsChecked = (value as bool?);
            else if (elem is TextBox)
                (elem as TextBox).Text = (value == null ? "" : value.ToString());
            else
                throw new InvalidOperationException("GCL.UIHelper.SetUIElementValue");
        }

        public class Converter
        {
            [ValueConversion(typeof(byte[]), typeof(ImageSource))]
            public class ByteArrayToImage : IValueConverter
            {
                static public byte[]        imageSourceToByteArray(ImageSource imageSource)
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();

                    byte[] bytes = null;

                    var bitmapSource = imageSource as BitmapSource;

                    if (bitmapSource != null)
                    {
                        var tmp = new Image { Source = imageSource };


                        encoder.Frames.Add(BitmapFrame.Create(tmp.Source as BitmapSource));

                        using (var stream = new MemoryStream())
                        {
                            encoder.Save(stream);
                            bytes = stream.ToArray();
                        }
                    }

                    return bytes;
                }
                static public ImageSource   byteArrayToImageSource(byte[] data)
                {
                    var imgSource = new BitmapImage();
                    if (data == null || data.Length == 0)
                        return null;

                    using (var mem = new MemoryStream(data))
                    {
                        mem.Position = 0;
                        imgSource.BeginInit();
                        imgSource.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        imgSource.CacheOption = BitmapCacheOption.OnLoad;
                        imgSource.UriSource = null;
                        imgSource.StreamSource = mem;
                        imgSource.EndInit();
                    }
                    imgSource.Freeze();
                    return imgSource;
                }

                public object               Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                {
                    byte[] format = value as byte[];
                    return byteArrayToImageSource(format);
                }
                public object               ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                {
                    ImageSource imgSrc = value as ImageSource;
                    return imageSourceToByteArray(imgSrc);
                }
            }
        }
        public class WPFElementFactory
        {
            static public FrameworkElementFactory GetButton(string contentPropertyValue, RoutedEventHandler onClickEvent)
            {
                var button = new FrameworkElementFactory(typeof(Button));
                button.SetValue(Button.ContentProperty, contentPropertyValue);
                if (onClickEvent != null)
                    button.AddHandler(Button.ClickEvent, onClickEvent);
                return button;
            }
            static public FrameworkElementFactory GetButtonWtImg(string imgPath, RoutedEventHandler onClickEvent)
            {
                var btnFactory = new FrameworkElementFactory(typeof(Button));
                var imgFactory = new FrameworkElementFactory(typeof(Image));
                imgFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri(imgPath)));
                imgFactory.SetValue(Image.WidthProperty, 20D);
                imgFactory.SetValue(Image.HeightProperty, 20D);

                ControlTemplate btnCtrlTemplate = new ControlTemplate(typeof(Button));
                //var trigger = new Trigger
                //{
                //    Setters =
                //    {
                //        new Setter { Property = Button.OpacityProperty, Value = 50D }
                //    }
                //};
                //btnCtrlTemplate.Triggers.Add(trigger);

                btnCtrlTemplate.VisualTree = imgFactory;
                btnFactory.SetValue(Button.TemplateProperty, btnCtrlTemplate);
                if (onClickEvent != null)
                    btnFactory.AddHandler(Button.ClickEvent, onClickEvent);

                return btnFactory;
            }
        }
        public class Control
        {
            public class RichTextEditor : RichTextBox
            {
                // todo
                // + Drag/drop event
            }
            // TODO : Recursion
            public class SimpleForm<T_Entity> : Grid
                where T_Entity : class, new()
            {
                public T_Entity                                             entity { get; set; } = null;
                public RoutedEventHandler                                   onValidate
                {
                    get { return onValidateValue; }
                    set
                    {
                        if (value == null)
                            return;
                        if (onValidateValue != null)
                            validButton.Click -= onValidateValue;
                        validButton.Click += (onValidateValue = value);
                    }
                }
                public Delegates.PropertyToUIElementConversion<T_Entity>    propertyToUIElementConvertor { get; set; } = PropertyToUIElement;

                private Button                                              validButton = new Button
                {
                    Name = "btn_Validate",
                    Content = "Validate"
                };
                private RoutedEventHandler                                  onValidateValue = null;

                public SimpleForm(T_Entity entityData, RoutedEventHandler onValidateEvent = null, Delegates.PropertyToUIElementConversion<T_Entity> PropertyToUIElementConvertor = null)
                {
                    entity = entityData;
                    onValidate = onValidateEvent;
                    propertyToUIElementConvertor = (PropertyToUIElementConvertor == null ? propertyToUIElementConvertor : PropertyToUIElementConvertor);

                    InitializeGrid();
                    InitializeColumns();
                    FillWithDatas();

                    validButton.Click += (sender, e) =>
                    {
                        foreach (var property in entity.GetType().GetProperties())
                        {
                            if (Attribute.IsDefined(property, typeof(GCL.UIHelper.Attributes.FormMandatory))
                            && property.GetValue(entity) == null)
                            {
                                e.Handled = true;
                                MessageBox.Show(String.Format("{0} is mandatory", property.Name),
                                    String.Format("{0} editor error", entity.GetType()),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error
                                );
                                return;
                            }
                        }
                    };
                }

                private void InitializeGrid()
                {
                    Margin = new Thickness(10, 10, 10, 10);
                    Background = Application.Current.FindResource("theme_BackgroundColor") as Brush;
                    Width = Double.NaN;
                    Height = Double.NaN;
                    HorizontalAlignment = HorizontalAlignment.Center;
                    VerticalAlignment = VerticalAlignment.Center;
                }
                private void InitializeColumns()
                {
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200D) });
                    foreach (var property in typeof(T_Entity).GetProperties())
                        RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
                    RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) }); // buttons
                }
                private void FillWithDatas()
                {
                    int rowIndex = 0;
                    foreach (var property in typeof(T_Entity).GetProperties())
                    {
                        if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
                            continue;

                        var elem = propertyToUIElementConvertor(property, entity);
                        #region Left column
                        var label = new Label { Content = property.Name + ":" };
                        Grid.SetColumn(label, 0);
                        Grid.SetRow(label, rowIndex);
                        Children.Add(label);
                        #endregion
                        #region right column
                        elem.Name = property.Name;
                        elem.Margin = new Thickness { Bottom = 10D };
                        Grid.SetColumn(elem, 1);
                        Grid.SetRow(elem, rowIndex);
                        elem.DataContext = entity;
                        #endregion
                        Children.Add(elem);
                        // Grid.SetRowSpan
                        ++rowIndex;
                    }
                    #region ValidateButton
                    
                    Grid.SetColumnSpan(validButton, 2);
                    Grid.SetRow(validButton, rowIndex);
                    Children.Add(validButton);
                    #endregion
                }
            }
            public class Popup : Window
            {
                static public void CenterWindowOnScreen(Window window)
                {
                    double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                    double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                    double windowWidth = window.Width;
                    double windowHeight = window.Height;
                    window.Left = (screenWidth / 2) - (windowWidth / 2);
                    window.Top = (screenHeight / 2) - (windowHeight / 2);
                }

                public Popup(Grid content, EventHandler onclose = null, string title = "")
                {
                    Width = content.Width;
                    Height = content.Height;
                    ResizeMode = ResizeMode.NoResize;
                    Title = title;
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    // Background = Brushes.Black;
                    // AllowDrop = true;

                    Content = content;

                    Window creatorWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                    creatorWindow.IsEnabled = false;
                    creatorWindow.Opacity = 50d;
                    this.Closed += (sender, e) =>
                    {
                        creatorWindow.IsEnabled = true;
                        creatorWindow.Opacity = 100d;
                    };
                    if (onclose != null)
                        this.Closed += onclose;
                    this.Loaded += (openSender, evOpened) =>
                    {
                        this.Width = content.ActualWidth + content.Margin.Left + content.Margin.Right + 30;
                        this.Height = content.ActualHeight + content.Margin.Bottom + content.Margin.Top + 50;
                        CenterWindowOnScreen(this);
                    };
                }
            }

            static public FrameworkElement          PropertyToUIElement<T>(PropertyInfo property, T dummy = null)
                where T : class, new()
            {
                FrameworkElement elem;

                if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
                    throw new InvalidOperationException("PropertyToUIElement : Attributes.NotUIVisible is set");

                #region Custom generation using GCL.DBEF.Attributes
                if (Attribute.IsDefined(property, typeof(Attributes.UIImageData)))
                {
                    elem = new Image
                    {
                        AllowDrop = true,
                        MinHeight = 200,
                        MinWidth = 200,
                        MaxHeight = 200,
                        MaxWidth = 200,
                        ToolTip = "Drop an image to replace"
                    };
                    elem.Drop += (sender, e) =>
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                        {
                            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                            var newSource = new BitmapImage(new Uri(files[0]));
                            (elem as Image).Source = newSource;
                            e.Handled = true;
                        }
                    };
                    elem.SetBinding(Image.SourceProperty, new Binding { Mode = BindingMode.TwoWay, Path = new PropertyPath(property.Name) });
                }
                //else if (Attribute.IsDefined(property, typeof(Attributes.UIFileData)))
                //{
                //    // OpenFileDialog 
                //    // [file1 | file2 | file3]
                //}
                //else if (Attribute.IsDefined(property, typeof(Attributes.UIRichText)))
                //{
                //    elem = new GCL.UIHelper.Control.RichTextEditor();
                //}
                #endregion
                #region Default generation
                else if (property.PropertyType == typeof(bool))
                {
                    elem = new CheckBox();
                    elem.SetBinding(CheckBox.IsCheckedProperty, new Binding(property.Name));
                }
                else if (property.PropertyType.IsEnum)
                {
                    elem = new ComboBox();
                    elem.SetValue(ComboBox.ItemsSourceProperty, property.PropertyType.GetEnumValues());
                    elem.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
                }
                else
                {
                    elem = new TextBox();
                    if (property.PropertyType == typeof(int))
                        elem.AddHandler(TextBox.PreviewTextInputEvent,
                        new TextCompositionEventHandler((sender, e) =>
                        {
                            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                                e.Handled = true;
                        }));
                    elem.SetBinding(TextBox.TextProperty, new Binding(property.Name));
                }
                #endregion

                elem.SetValue(ListViewItem.WidthProperty, Double.NaN);
                elem.SetValue(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
                elem.SetValue(FrameworkElement.IsEnabledProperty, !(Attribute.IsDefined(property, typeof(Attributes.NotUIEditable))));
                return elem;
            }
            static public FrameworkElementFactory   PropertyToUIElementFactory(PropertyInfo property)
            {
                FrameworkElementFactory elemFactory;

                if (Attribute.IsDefined(property, typeof(Attributes.NotUIVisible)))
                    throw new InvalidOperationException("PropertyToUIElementFactory : Attributes.NotUIVisible is set");

                if (property.PropertyType == typeof(bool))
                {
                    elemFactory = new FrameworkElementFactory(typeof(CheckBox));
                    elemFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding(property.Name));
                    elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
                }
                else if (property.PropertyType.IsEnum)
                {
                    elemFactory = new FrameworkElementFactory(typeof(ComboBox));
                    elemFactory.SetValue(ComboBox.ItemsSourceProperty, property.PropertyType.GetEnumValues());
                    elemFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
                    elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
                }
                else
                {
                    elemFactory = new FrameworkElementFactory(typeof(TextBox));
                    if (property.PropertyType == typeof(int))
                        elemFactory.AddHandler(TextBox.PreviewTextInputEvent,
                        new TextCompositionEventHandler((sender, e) =>
                        {
                            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                                e.Handled = true;
                        }));
                    elemFactory.SetBinding(TextBox.TextProperty, new Binding(property.Name));
                    elemFactory.SetValue(ListViewItem.WidthProperty, 200D);
                }

                // elemFactory.SetValue(ListViewItem.WidthProperty, col.Width); // => No effect
                elemFactory.SetValue(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
                elemFactory.SetValue(FrameworkElement.IsEnabledProperty, !Attribute.IsDefined(property, typeof(Attributes.NotUIEditable)));
                return elemFactory;
            }
            static public FrameworkElementFactory   ToStringToUIElementFactory<T>()
            {
                return ToStringToUIElementFactory(typeof(T));
            }
            static public FrameworkElementFactory   ToStringToUIElementFactory(Type type)
            {
                FrameworkElementFactory elemFactory = new FrameworkElementFactory(typeof(Label));
                elemFactory.SetBinding(Label.ContentProperty, new Binding());
                elemFactory.SetValue(Label.IsEnabledProperty, false);
                elemFactory.SetValue(ListViewItem.WidthProperty, double.NaN);
                return elemFactory;
            }
        }
        public class DragDropHelper
        {
            public static bool IsDragging = false;
        }
        public class ControlManipulation
        {
            public static void AttachAddDeleteContextMenu<T_Item>(ref ListView view,
                Delegates.OnUpdate<T_Item> onAdd,
                Delegates.OnUpdate<T_Item> onDelete,
                Delegates.OnUpdate<T_Item> onEdit,
                Delegates.PropertyToUIElementConversion<T_Item> PropertyToUIElementConvertor = null,
                Delegates.objectFactory<T_Item> itemFactory = null)
                where T_Item : class, new()
            {
                var viewCpy = view;

                var viewItemCM = new ContextMenu();
                {
                    // ListViewItem context Menu
                    #region MenuItem binded to commands
                    #region Add MenuItem
                    var AddMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/add.png")), Width = 16, Height = 16 },
                        Header = "Add"
                    };
                    AddMenuItem.Click += (sender, e) =>
                    {
                        var item = (itemFactory == null ? new T_Item() : itemFactory());
                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(
                            form,
                            null,
                            "Product formulary");
                        form.onValidate = (s, ev) => { onAdd(item); viewCpy.Items.Refresh(); popup.Close(); };
                        popup.Show();
                    };
                    viewItemCM.Items.Add(AddMenuItem);
                    #endregion
                    #region Delete MenuItem
                    var DeleteMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/trash.png")), Width = 16, Height = 16 },
                        Header = "Delete"
                    };
                    DeleteMenuItem.Click += (sender, e) =>
                    {
                        onDelete(viewCpy.SelectedItem as T_Item);
                        viewCpy.Items.Refresh();
                    };
                    viewItemCM.Items.Add(DeleteMenuItem);
                    #endregion
                    #region Edit MenuItem
                    var EditMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/edit.png")), Width = 16, Height = 16 },
                        Header = "Edit"
                    };
                    EditMenuItem.Click += (sender, e) =>
                    {
                        var item = viewCpy.SelectedItem as T_Item;
                        if (item == null)
                            throw new InvalidOperationException("AttachAddDeleteContextMenu : Bad item type");
                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(form, null, "Product modification");
                        form.onValidate = (s, ev) => { onEdit(item); popup.Close(); };
                        popup.Show();
                    };
                    viewItemCM.Items.Add(EditMenuItem);
                    #endregion
                    #endregion
                }
                   

                view.ItemContainerStyle = new Style(typeof(ListViewItem));
                view.ItemContainerStyle.Setters.Add(new Setter(ListViewItem.ContextMenuProperty, viewItemCM));

                var viewCM = new ContextMenu();
                {   // ListView context menu
                    #region Add MenuItem
                    var AddMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/add.png")), Width = 16, Height = 16 },
                        Header = "Add"
                    };
                    AddMenuItem.Click += (sender, e) =>
                    {
                        var item = (itemFactory == null ? new T_Item() : itemFactory());
                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(
                            form,
                            null,
                            "Product formulary");
                        form.onValidate = (s, ev) => { onAdd(item); viewCpy.Items.Refresh(); popup.Close(); };
                        popup.Show();
                    };
                    viewCM.Items.Add(AddMenuItem);
                    #endregion
                }

                view.SetValue(ListView.ContextMenuProperty, viewCM);
            }
            public static void AttachAddDeleteContextMenu<T_Item>(ref TreeView view,
                Delegates.OnUpdate<T_Item> onAdd,
                Delegates.OnUpdate<T_Item> onDelete,
                Delegates.OnUpdate<T_Item> onEdit,
                Delegates.PropertyToUIElementConversion<T_Item> PropertyToUIElementConvertor = null)
                where T_Item : class, new()
            {
                var viewCpy = view;

                var viewItemCM = new ContextMenu();
                {   // TreeViewItem context Menu
                    #region MenuItem binded to commands
                    #region Add MenuItem
                    var AddMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/add.png")), Width = 16, Height = 16 },
                        Header = "Add"
                    };
                    AddMenuItem.Click += (sender, e) =>
                    {
                        var item = new T_Item();
                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(
                            form,
                            null,
                            "Product formulary");
                        form.onValidate = (s, ev) => { onAdd(item); viewCpy.Items.Refresh(); popup.Close(); };
                        popup.Show();
                    };
                    viewItemCM.Items.Add(AddMenuItem);
                    #endregion
                    #region Delete MenuItem
                    var DeleteMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/trash.png")), Width = 16, Height = 16 },
                        Header = "Delete"
                    };
                    DeleteMenuItem.Click += (sender, e) =>
                    {
                        var selectedItem = (viewCpy.SelectedItem as TreeViewItem);
                        if (selectedItem == null)
                            return;

                        onDelete(selectedItem.DataContext as T_Item as T_Item);
                        viewCpy.Items.Refresh();
                    };
                    viewItemCM.Items.Add(DeleteMenuItem);
                    #endregion
                    #region Edit MenuItem
                    var EditMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/edit.png")), Width = 16, Height = 16 },
                        Header = "Edit"
                    };
                    EditMenuItem.Click += (sender, e) =>
                    {
                        var selectedItem = (viewCpy.SelectedItem as TreeViewItem);
                        if (selectedItem == null)
                            return;
                            // throw new InvalidOperationException("AttachAddDeleteContextMenu : Bad item type");

                        var item = selectedItem.DataContext as T_Item;

                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(form, null, "Product modification");
                        form.onValidate = (s, ev) => { onEdit(item); popup.Close(); };
                        popup.Show();
                    };
                    viewItemCM.Items.Add(EditMenuItem);
                    #endregion
                    #endregion
                }

                view.ItemContainerStyle = new Style(typeof(TreeViewItem));
                view.ItemContainerStyle.Setters.Add(new Setter(TreeViewItem.ContextMenuProperty, viewItemCM));

                var viewCM = new ContextMenu();
                {   // TreeView context menu
                    #region Add MenuItem
                    var AddMenuItem = new MenuItem
                    {
                        Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/VidaProteina;component/Images/Icons/add.png")), Width = 16, Height = 16 },
                        Header = "Add"
                    };
                    AddMenuItem.Click += (sender, e) =>
                    {
                        var item = new T_Item();
                        var form = new Control.SimpleForm<T_Item>(item, null, PropertyToUIElementConvertor);
                        var popup = new Control.Popup(form, null, "Product formulary");
                        form.onValidate = (s, ev) => { onAdd(item); viewCpy.Items.Refresh(); popup.Close(); };
                        popup.Show();
                    };
                    viewCM.Items.Add(AddMenuItem);
                    #endregion
                }

                view.SetValue(TreeView.ContextMenuProperty, viewCM);
            }

            public class TreeViewHelper
            {
                public class DragAndDrop
                {
                    public class Delegates
                    {
                        public delegate bool MoveCondition(/*TreeViewItem or TreeView*/ object dest, IDataObject src);
                        public delegate bool OnDrop(/*TreeViewItem or TreeView*/ object dest, IDataObject src);
                        public delegate void OnUpdate();
                    }
                    public class Configuration : List<KeyValuePair<Delegates.MoveCondition, Delegates.OnDrop>> { };

                    public static void Attach(TreeView view, Configuration dragAndDropConfiguration)
                    {
                        view.MouseMove += (object sender, MouseEventArgs e) =>
                        {
                            if (!UIHelper.DragDropHelper.IsDragging && view.SelectedValue != null && e.LeftButton == MouseButtonState.Pressed)
                            {
                                UIHelper.DragDropHelper.IsDragging = true;
                                DragDrop.DoDragDrop(view, view.SelectedValue, DragDropEffects.Move);
                                UIHelper.DragDropHelper.IsDragging = false;
                            }
                        };
                        view.DragOver += (object sender, DragEventArgs e) =>
                        {
                            if (!UIHelper.DragDropHelper.IsDragging || e == null || e.Source == null || !(e.Source is TreeViewItem))
                                return;
                            (e.Source as TreeViewItem).Background = Brushes.DimGray;
                        };
                        view.DragLeave += (object sender, DragEventArgs e) =>
                        {
                            if (!UIHelper.DragDropHelper.IsDragging || e == null || e.Source == null || !(e.Source is TreeViewItem))
                                return;
                            (e.Source as TreeViewItem).Background = Brushes.Transparent;
                        };
                        view.Drop += (object sender, DragEventArgs e) =>
                        {
                            e.Handled = true;
                            e.Effects = DragDropEffects.None;
                            try
                            {
                                object dest = (e.Source as TreeViewItem);//.DataContext as T_Entity;
                                var source = e.Data;// ((sender as TreeView).SelectedItem as TreeViewItem).DataContext as T_Entity;

                                if (dest == null && (dest = (e.Source as TreeView)) == null)
                                    return;

                                Func<IEnumerable<KeyValuePair<Delegates.MoveCondition, Delegates.OnDrop>>> isMoveAllowedCondition = () =>
                                {
                                    return (from configuration
                                            in dragAndDropConfiguration
                                            where configuration.Key(dest, source)
                                            select configuration);
                                };
                                var allowMove = isMoveAllowedCondition();

                                if (dest == source ||
                                    allowMove.Count() == 0)
                                {
                                    (dest as TreeViewItem).Background = Brushes.Transparent;
                                    return;
                                }
                                foreach (var configuration in allowMove)
                                {
                                    configuration.Value(dest as TreeViewItem, source);
                                }
                            }
                            catch (NullReferenceException) { }
                            catch (InvalidCastException) { }
                        };
                    }
                }
            }
        }
    }
}
