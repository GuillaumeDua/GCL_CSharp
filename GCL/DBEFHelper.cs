using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace GCL
{
    public class StringHelper
    {
        static public String CleanString(String str)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(str, "_");
        }
    }

    public class DBEF
    {
        public class Attributes
        {
            public class Hierarchy
            {
                public class FK_OnlyLeafs : Attribute { }
                public class FK_OnlyRoots : Attribute { }
                public class FK_OnlyOwnChildrens : Attribute { }
            }
            public class Binding
            {
                public class HasSource : Attribute
                {
                    public string Name = null;
                }
            }
        }
        public class Delegates
        {
            public delegate void onUpdate(object entity);
        }

        public class EntityInfo
        {
            static public PropertyInfo          GetPK<T>()
            where T : class, new()
            {
                return (from property in typeof(T).GetProperties()
                        where Attribute.IsDefined(property, typeof(KeyAttribute))
                        select property
                        ).First();
            }
            static public PropertyInfo          GetPK(object var)
            {
                return (from property in var.GetType().GetProperties()
                        where Attribute.IsDefined(property, typeof(KeyAttribute))
                        select property
                        ).First();
            }
            static public bool                  IsRecursive<T_Entity>()
            {
                return IsRecursive(typeof(T_Entity));
            }
            static public bool                  IsRecursive(Type entityType)
            {
                return (from property
                        in entityType.GetProperties()
                        where property.PropertyType == entityType
                        select property
                        ).Count() != 0;
            }

            static public PropertyInfo          GetRecursivePropertyInfo_Parent(Type entityType)
            { 
                return (
                    from    property
                    in      entityType.GetProperties()
                    where   property.PropertyType == entityType
                    select  property
                ).FirstOrDefault();
            }
            static public IEnumerable<object>   GetRecursivePropertiesValues_Parents(Type entityType, object instance)
            {
                if (instance == null)
                    return new List<object>();
                return GetRecursivePropertiesValues_Parents(entityType, instance, GetRecursivePropertyInfo_Parent(entityType), new List<object>());
            }
            static private IEnumerable<object>  GetRecursivePropertiesValues_Parents(Type entityType, object instance, PropertyInfo recProperty, List<object> list)
            {
                var parent = recProperty.GetValue(instance);
                if (parent == null)
                    return list;
                list.Add(parent);
                return GetRecursivePropertiesValues_Parents(entityType, parent, recProperty, list);
            }

            static public PropertyInfo          GetRecursivePropertyInfo_Childrens(Type entityType)
            {
                return (
                    from property
                    in entityType.GetProperties()
                    where property.PropertyType.IsConstructedGenericType &&
                            //TODO : property.PropertyType convertible to IEnumerable
                            property.PropertyType.GenericTypeArguments[0] == entityType
                    select property
                ).FirstOrDefault();
            }
            static public IEnumerable<object>   GetRecursivePropertiesValues_Childrens(Type entityType, object instance)
            {
                if (instance == null)
                    return new List<object>();
                return GetRecursivePropertiesValues_Childrens(entityType, instance, GetRecursivePropertyInfo_Childrens(entityType), new List<object>());
            }
            static private IEnumerable<object>  GetRecursivePropertiesValues_Childrens(Type entityType, object instance, PropertyInfo recProperty, List<object> list)
            {
                var childrens = recProperty.GetValue(instance);
                if ((childrens as IEnumerable<object>).Count() != 0)
                    foreach (var child in childrens as IEnumerable<object>)
                    {
                        list.Add(child);
                        GetRecursivePropertiesValues_Childrens(entityType, child, recProperty, list);
                    }
                return list;
            }

            static public bool                  ExistsInRecursivity<T_Entity>(T_Entity entity)
            {
                var recursiveProperty = (from property
                        in typeof(T_Entity).GetProperties()
                                         where property.PropertyType == typeof(T_Entity)
                                         select property
                        ).FirstOrDefault();

                if (recursiveProperty == null)
                    throw new InvalidOperationException("GCL.DBEF.EntityInfo.ExistsInRecursivity<E> : E is not recursive");
                var rec = recursiveProperty.GetValue(entity);
                return ExistsInRecursivity(entity, rec, recursiveProperty);
            }
            static public bool                  ExistsInRecursivity(object entity)
            {
                if (entity == null)
                    throw new NullReferenceException("GCL.DBEF.EntityInfo.ExistsInRecursivity<E> : E instance is null");

                var recursiveProperty = (from property
                        in entity.GetType().GetProperties()
                        where property.PropertyType == entity.GetType()
                        select property
                        ).FirstOrDefault();

                if (recursiveProperty == null)
                    throw new InvalidOperationException("GCL.DBEF.EntityInfo.ExistsInRecursivity<E> : E is not recursive");
                var rec = recursiveProperty.GetValue(entity);
                return ExistsInRecursivity(entity, rec, recursiveProperty);
            }
            static public bool                  ExistsInRecursivity(object entity, object rec, PropertyInfo recursiveProperty)
            {
                if (entity == null)
                    throw new NullReferenceException("GCL.DBEF.EntityInfo.ExistsInRecursivity<E> : E instance is null");

                if (rec == null)
                    return false;
                if (recursiveProperty.GetValue(rec).Equals(entity))
                    return true;
                return ExistsInRecursivity(entity, recursiveProperty.GetValue(rec), recursiveProperty);
            }

            static public bool                  HasHierarchyTree<T_Entity>()
            {
                return HasHierarchyTree(typeof(T_Entity));
            }
            static public bool                  HasHierarchyTree(Type entityType)
            {
                return IsRecursive(entityType) &&   // Parent
                    (                               // Children
                        from property
                        in entityType.GetProperties()
                        where property.PropertyType.IsGenericType &&
                                property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)/*.GetGenericTypeDefinition()*/ &&
                                property.PropertyType.GenericTypeArguments[0] == entityType
                        select property
                    ).Count() == 1;
            }
        }

        public class ContextAction<T_Context>
            where T_Context : DbContext, new()
        {
            static public PropertyInfo              GetDbSet<T_Entity>()
                where T_Entity : class, new()
            {
                return (from property in typeof(T_Context).GetProperties()
                        where property.PropertyType == typeof(DbSet<T_Entity>)
                        select property
                        ).First();
            }
            static public PropertyInfo              GetDbSet(Type entityType)
            {
                return (from property in typeof(T_Context).GetProperties()
                        where property.PropertyType.IsConstructedGenericType
                        where property.PropertyType.GenericTypeArguments[0] == entityType
                        select property
                        ).First();
            }
            static public bool                      SetExists<T_Entity>(T_Context context)
                where T_Entity : class, new()
            {   // TODO : Test
                foreach (var property in context.GetType().GetProperties())
                {
                    if (property.PropertyType == typeof(DbSet<T_Entity>))
                        return true;
                }
                return false;
            }
            static public bool                      SetExists(T_Context context, Type type)
            {
                foreach (var property in context.GetType().GetProperties())
                {
                    if (property.PropertyType.IsGenericType &&
                        property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>).GetGenericTypeDefinition() &&
                        property.PropertyType.GenericTypeArguments[0] == type
                        )
                        return true;
                }
                return false;
            }
            static public IEnumerable<PropertyInfo> GetFKs(T_Context context, Type entityType)
            {
                return (from property
                        in entityType.GetProperties()
                        where SetExists(context, property.PropertyType)
                        select property
                        );
            }

            #region Operations
            static public void InsertOrUpdate<T_Entity>(T_Entity entity) where T_Entity : class, new()
            {
                using (var context = new T_Context())
                {
                    var primaryKeyPropertyInfo = EntityInfo.GetPK(entity);
                    Debug.Assert(primaryKeyPropertyInfo != null);

                    var primaryKeyValue = primaryKeyPropertyInfo.GetValue(entity);

                    if (primaryKeyValue == null ||
                        primaryKeyValue.ToString() == "0")
                    {   // Insert
                        var setPropertyInfo = GetDbSet<T_Entity>();
                        Debug.Assert(setPropertyInfo != null);

                        var set = setPropertyInfo.GetValue(context) as DbSet<T_Entity>;
                        set.Add(entity);
                    }
                    else
                    {   // Update
                        context.Entry(entity).State = EntityState.Modified;
                    }

                    context.SaveChanges();
                }
            }
            static public void EnsureUpdate<T_Entity>(T_Entity entity) where T_Entity : class, new()
            {
                using (var context = new T_Context())
                {
                    var primaryKeyPropertyInfo = EntityInfo.GetPK(entity);
                    Debug.Assert(primaryKeyPropertyInfo != null);

                    var primaryKeyValue = primaryKeyPropertyInfo.GetValue(entity);

                    if (primaryKeyValue == null ||
                        primaryKeyValue.ToString() == "0")
                    {   // Insert
                        throw new InvalidOperationException("GCL.DBEF.ContextAction.EnsureUpdate : Entity does not exist in current context");
                    }
                    else
                    {   // Update
                        context.Entry(entity).State = EntityState.Modified;
                    }

                    context.SaveChanges();
                }
            }
            static public void EnsureInsert<T_Entity>(T_Entity entity) where T_Entity : class, new()
            {
                using (var context = new T_Context())
                {
                    var primaryKeyPropertyInfo = EntityInfo.GetPK(entity);
                    Debug.Assert(primaryKeyPropertyInfo != null);

                    var primaryKeyValue = primaryKeyPropertyInfo.GetValue(entity);

                    if (primaryKeyValue == null ||
                        primaryKeyValue.ToString() == "0")
                    {   // Insert
                        var setPropertyInfo = GetDbSet<T_Entity>();
                        Debug.Assert(setPropertyInfo != null);

                        var set = setPropertyInfo.GetValue(context) as DbSet<T_Entity>;
                        set.Add(entity);
                    }
                    else
                    {   // Update
                        throw new InvalidOperationException("GCL.DBEF.ContextAction.EnsureInsert : Entity already exist in current context");
                    }

                    context.SaveChanges();
                }
            }
            static public void EnsureDelete<T_Entity>(T_Entity entity) where T_Entity : class, new()
            {
                using (var context = new T_Context())
                {
                    var primaryKeyPropertyInfo = EntityInfo.GetPK(entity);
                    Debug.Assert(primaryKeyPropertyInfo != null);

                    var primaryKeyValue = primaryKeyPropertyInfo.GetValue(entity);

                    if (primaryKeyValue == null ||
                        primaryKeyValue.ToString() == "0")
                    {
                        throw new InvalidOperationException("GCL.DBEF.ContextAction.EnsureDelete : Entity Does not exist in current context");
                    }
                    else
                    {   // delete
                        var setPropertyInfo = GetDbSet(entity.GetType());
                        Debug.Assert(setPropertyInfo != null);

                        // var set = setPropertyInfo.GetValue(context) as DbSet<T_Entity>;
                        context.Entry(entity).State = EntityState.Deleted;
                        // set.Remove(entity);
                    }
                    context.SaveChanges();
                }
            }
            #endregion

            #region Views
            static public List<object>              CopyDatasToList<T_EntityType>()
            {
                return CopyDatasToList(typeof(T_EntityType));
            }
            static public List<object>              CopyDatasToList(Type EntityType)
            {
                using (var context = new T_Context())
                {
                    var task = context.Set(EntityType).ToListAsync();
                    task.Wait();
                    return task.Result;
                }
            }

            // TODO : Merge PropertyToUIElement and PropertyToUIElementFactory

            static public FrameworkElement          PropertyToUIElement<T_EntityType>(T_Context context, T_EntityType entity, PropertyInfo property)
                where T_EntityType : class, new()
            {
                if (property.DeclaringType != typeof(T_EntityType))
                    throw new InvalidOperationException("GCL.DBEF.ContextAction<>.PropertyToUIElement<> : type mismatch");

                FrameworkElement elem;

                if (SetExists(context, property.PropertyType))
                {
                    #region comboBox
                    elem = new ComboBox();
                    IEnumerable<object> res;

                    if (property.PropertyType == property.DeclaringType)
                    {   // Recursive key hierarchy
                        res = (from object e
                               in context.Set(property.PropertyType).Local
                               where (!e.Equals(entity))
                               select e);
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Hierarchy.FK_OnlyLeafs)))
                    {   // FK
                        var parentProperty = property.PropertyType.GetProperty("Parent");
                        var childrenProperty = property.PropertyType.GetProperty("Children");

                        if (parentProperty != null &&
                            childrenProperty != null &&
                            childrenProperty.PropertyType.IsGenericType &&
                            childrenProperty.PropertyType.GetGenericArguments()[0] == parentProperty.PropertyType
                            )
                        {
                            res = (from object e
                                   in context.Set(property.PropertyType).Local
                                   let childrens = childrenProperty.GetValue(e)
                                   let countProperty = childrens.GetType().GetProperty("Count")
                                   where countProperty != null
                                   let countValue = countProperty.GetValue(childrens)
                                   where (countValue is int && (int)countProperty.GetValue(childrens) == 0)
                                   select e);
                        }
                        else
                            res = null;
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Binding.HasSource)))
                    {
                        var attr = Attribute.GetCustomAttribute(property, typeof(Attributes.Binding.HasSource));
                        res = entity.GetType().GetProperty((attr as Attributes.Binding.HasSource).Name).GetValue(entity) as IEnumerable<object>;
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Hierarchy.FK_OnlyRoots)))
                    {
                        var parentProperty = property.PropertyType.GetProperty("Parent");
                        if (parentProperty != null)
                            res = (from object e
                                   in context.Set(property.PropertyType).Local
                                   where parentProperty.GetValue(e) == null
                                   select e);
                        else
                            res = null;
                    }
                    else
                    {   // FK, All values
                        res = (from object e
                               in context.Set(property.PropertyType).Local
                               select e);
                    }

                    elem.SetValue(ComboBox.ItemsSourceProperty, res);
                    elem.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
                    #endregion
                }
                else
                    elem = UIHelper.Control.PropertyToUIElement<T_EntityType>(property);

                if (Attribute.IsDefined(property, typeof(KeyAttribute)))
                    elem.SetValue(FrameworkElement.IsEnabledProperty, false);
                return elem;
            }
            static public UIHelper.Delegates.PropertyToUIElementConversion<T_Entity>
                                                    PropertyToUIElementAdapter<T_Entity>(T_Context context)
                where T_Entity : class, new()
            {
                return (PropertyInfo property, T_Entity entity) => { return PropertyToUIElement<T_Entity>(context, entity, property); };
            }
            static public FrameworkElementFactory   PropertyToUIElementFactory(T_Context context, PropertyInfo property)
            {
                FrameworkElementFactory elemFactory;

                if (SetExists(context, property.PropertyType))
                {
                    #region comboBox
                    elemFactory = new FrameworkElementFactory(typeof(ComboBox));
                    IEnumerable<object> res;

                    if (property.PropertyType == property.DeclaringType)
                    {   // Recursive key hierarchy
                        res = (from object e
                               in context.Set(property.PropertyType).Local
                               // where (!e.Equals(entity)) // TODO : Fix
                               select e);
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Binding.HasSource)))
                    {
                        var attr = Attribute.GetCustomAttribute(property, typeof(Attributes.Binding.HasSource));
                        elemFactory.SetBinding(ComboBox.ItemsSourceProperty, new Binding((attr as Attributes.Binding.HasSource).Name));
                        elemFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));

                        elemFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler((sender, ev) =>
                        {
                            context.SaveChanges();
                            // var cb = sender as ComboBox;
                            // todo : update itemsource
                        }));

                        return elemFactory;
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Hierarchy.FK_OnlyLeafs)))
                    {   // FK
                        var parentProperty = property.PropertyType.GetProperty("Parent");
                        var childrenProperty = property.PropertyType.GetProperty("Children");

                        if (parentProperty != null &&
                            childrenProperty != null &&
                            childrenProperty.PropertyType.IsGenericType &&
                            childrenProperty.PropertyType.GetGenericArguments()[0] == parentProperty.PropertyType
                            )
                        {
                            res = (from object e
                                   in context.Set(property.PropertyType).Local
                                   let childrens = childrenProperty.GetValue(e)
                                   let countProperty = childrens.GetType().GetProperty("Count")
                                   where countProperty != null
                                   let countValue = countProperty.GetValue(childrens)
                                   where (countValue is int && (int)countProperty.GetValue(childrens) == 0)
                                   select e);
                            // todo : color for parent and childrens
                        }
                        else
                            res = null;
                    }
                    else if (Attribute.IsDefined(property, typeof(Attributes.Hierarchy.FK_OnlyRoots)))
                    {
                        var parentProperty = property.PropertyType.GetProperty("Parent");
                        if (parentProperty != null)
                            res = (from object e
                                   in context.Set(property.PropertyType).Local
                                   where parentProperty.GetValue(e) == null
                                   select e);
                        else
                            res = null;
                    }
                    else
                    {   // FK, All values
                        res = (from object e
                               in context.Set(property.PropertyType).Local
                               select e);
                    }

                    elemFactory.SetValue(ComboBox.ItemsSourceProperty, res);
                    elemFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
                    #endregion

                    elemFactory.SetValue(ComboBox.IsEditableProperty, true);
                    elemFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler((sender, ev) => { context.SaveChanges(); }));
                }
                else
                    elemFactory = UIHelper.Control.PropertyToUIElementFactory(property);

                if (Attribute.IsDefined(property, typeof(KeyAttribute)))
                    elemFactory.SetValue(FrameworkElement.IsEnabledProperty, false);
                return elemFactory;
            }

            static public UIHelper.Delegates.PropertyToUIElementFactoryConversion
                                                    PropertyToUIElementFactoryAdapter(T_Context context)
            {
                return (PropertyInfo property) => { return PropertyToUIElementFactory(context, property); };
            }
            static public FrameworkElementFactory   SaveAndDeleteButtonspanelFactory(Type EntityType)
            {
                var btnsPanelFactory = new FrameworkElementFactory(typeof(WrapPanel));
                var btnValidateFactory = GCL.UIHelper.WPFElementFactory.GetButtonWtImg(
                    "pack://application:,,,/VidaProteina;component/Images/save.png",
                    new RoutedEventHandler((sender, e) =>
                    {
                        var item = (e.OriginalSource as Button).DataContext;
                        var entity = Convert.ChangeType(item, EntityType);
                        EnsureUpdate(entity);
                    })
                );
                var btnDeleteFactory = GCL.UIHelper.WPFElementFactory.GetButtonWtImg(
                    "pack://application:,,,/VidaProteina;component/Images/cancel.png",
                    new RoutedEventHandler((sender, e) =>
                    {
                        var item = (e.OriginalSource as Button).DataContext;
                        var entity = Convert.ChangeType(item, EntityType);

                        MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                            string.Format("Confirm entry : \n{0}\ndeletion", entity),
                            "Confirmation requiered",
                            System.Windows.MessageBoxButton.YesNo
                        );
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            EnsureDelete(entity);
                        }
                    })
                );
                btnsPanelFactory.AppendChild(btnValidateFactory);
                btnsPanelFactory.AppendChild(btnDeleteFactory);
                return btnsPanelFactory;
            }

            static public List<PropertyInfo>        ToDbSetList()
            {
                List<PropertyInfo> list = new List<PropertyInfo>();
                foreach (var property in typeof(T_Context).GetRuntimeProperties())
                {
                    if (property.DeclaringType == typeof(T_Context))
                        list.Add(property);
                }
                return list;
            }

            public class Controler
            {
                // Edit a single entry
                public class Entity
                {
                    static public Grid      SimpleForm<T_Entity>(T_Context context, T_Entity entity)
                        where T_Entity : class, new()
                    {
                        return new UIHelper.Control.SimpleForm<T_Entity>(entity, (sender, e) =>
                        {
                            // InsertOrUpdate(entity);
                        }, PropertyToUIElementAdapter<T_Entity>(context));
                    }
                }
                // Add/delete entries in DbSet
                public class DbSet
                {
                    // Todo : Opti algo, avoid full-scan
                    static public TreeView  ToTreeView<T_Entity>(ref T_Context context)
                        where T_Entity : class, new()
                    {
                        if (!EntityInfo.IsRecursive<T_Entity>())
                            throw new InvalidOperationException("GCL.DBEF.ContextAction<T>.Controler.DbSet.ToTreeView<E> : E is not recursive");

                        PropertyInfo recProperty    = EntityInfo.GetRecursivePropertyInfo_Parent(typeof(T_Entity));
                        var items                   = new Dictionary<T_Entity, TreeViewItem>();
                        TreeView view               = new TreeView { AllowDrop = true };

                        foreach (var elem in context.Set<T_Entity>().Local)
                        {
                            items.Add(elem, new TreeViewItem { Header = elem.ToString(), DataContext = elem });
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
                                items[tmp as T_Entity].Items.Add(elem.Value);
                            }
                        }

                        foreach (TreeViewItem item in items.Values)
                        {
                            item.ExpandSubtree();
                            item.Foreground = (item.Items.Count == 0 ?
                                Application.Current.FindResource("theme_PrimaryVariant1Color") :
                                Application.Current.FindResource("theme_SecondaryColor")) as Brush;
                        }

                        return view;
                    }
                    static public void      TreeView_AddDragAndDrop<T_Entity>(TreeView productView, Func<int> onTEntityUpdate)
                        where T_Entity : class, new()
                    {
                        var DragAndDropConfiguration_Products = new KeyValuePair
                        <
                            UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Delegates.MoveCondition,
                            UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Delegates.OnDrop
                        >(
                            (dest, src) =>
                            {
                                try
                                {
                                    var source = (src.GetData(typeof(TreeViewItem)) as TreeViewItem).DataContext as T_Entity;
                                    var destination = (dest as TreeViewItem != null ? (dest as TreeViewItem).DataContext as T_Entity : null);

                                    if (source == null)
                                        return false;

                                    PropertyInfo recProperty = EntityInfo.GetRecursivePropertyInfo_Parent(typeof(T_Entity));
                                    var sourceValue = recProperty.GetValue(source);

                                    var recChildrens = EntityInfo.GetRecursivePropertiesValues_Childrens(typeof(T_Entity), source);
                                    bool isTreeLoop = (from child
                                                        in recChildrens
                                                        where child == destination
                                                       select child).Count() != 0;
                                    return (source != destination &&
                                            sourceValue != destination &&
                                            !isTreeLoop);
                                }
                                catch (NullReferenceException) { }
                                catch (InvalidCastException) { }
                                return false;
                            },
                            (dest, src) =>
                            {
                                var source = (src.GetData(typeof(TreeViewItem)) as TreeViewItem).DataContext as T_Entity;
                                var destination = (dest as TreeViewItem != null ? (dest as TreeViewItem).DataContext as T_Entity : null);

                                PropertyInfo recProperty = EntityInfo.GetRecursivePropertyInfo_Parent(typeof(T_Entity));
                                recProperty.SetValue(source, destination);
                                onTEntityUpdate();
                                return true;
                            }
                        );

                        UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Attach(
                            productView,
                            new UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Configuration
                            {
                                DragAndDropConfiguration_Products
                            }
                        );
                    }
                    //static public void      Test__TreeView_AddDragAndDrop<T_Entity>(TreeView productView, Func<int> onTEntityUpdate)
                    //    where T_Entity : class, new()
                    //{
                    //    var DragAndDropConfiguration_Products = new KeyValuePair
                    //    <
                    //        UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Delegates.MoveCondition,
                    //        UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Delegates.OnDrop
                    //    >(
                    //        (dest, src) =>
                    //        {
                    //            try
                    //            {
                    //                var source = (src.GetData(typeof(TreeViewItem)) as TreeViewItem).DataContext as T_Entity;
                    //                var destination = (dest as TreeViewItem != null ? (dest as TreeViewItem).DataContext as T_Entity : null);

                    //                if (source == null)
                    //                    return false;

                    //                PropertyInfo recProperty = EntityInfo.GetRecursivePropertyInfo_Parent(typeof(T_Entity));
                    //                var sourceValue = recProperty.GetValue(source);

                    //                var recChildrens = EntityInfo.GetRecursivePropertiesValues_Childrens(typeof(T_Entity), source);
                    //                bool isTreeLoop = (from child
                    //                                    in recChildrens
                    //                                    where child == destination
                    //                                   select child).Count() != 0;
                    //                return (source != destination &&
                    //                        sourceValue != destination &&
                    //                        !isTreeLoop);
                    //            }
                    //            catch (NullReferenceException) { }
                    //            catch (InvalidCastException) { }
                    //            return false;
                    //        },
                    //        (dest, src) =>
                    //        {
                    //            var source = (src.GetData(typeof(TreeViewItem)) as TreeViewItem).DataContext as T_Entity;
                    //            var destination = (dest as TreeViewItem != null ? (dest as TreeViewItem).DataContext as T_Entity : null);

                    //            PropertyInfo recProperty = EntityInfo.GetRecursivePropertyInfo_Parent(typeof(T_Entity));
                    //            recProperty.SetValue(source, destination);
                    //            onTEntityUpdate();
                    //            return true;
                    //        }
                    //    );

                    //    UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Attach(
                    //        productView,
                    //        new UIHelper.ControlManipulation.TreeViewHelper.DragAndDrop.Configuration
                    //        {
                    //            DragAndDropConfiguration_Products
                    //        }
                    //    );
                    //}

                    // todo :
                    //  - Dynamic ordering
                    //  - Dynamic filter
                    static public ListView  ToEditableListView<T_Entity>(T_Context context)
                         where T_Entity : class, new()
                    {
                        return ToEditableListView(context, typeof(T_Entity));
                    }
                    static public ListView  ToEditableListView(T_Context context, Type EntityType)
                    {
                        var listView = new ListView
                        {
                            Name = string.Format("listBox_{0}_{1}",
                                                 StringHelper.CleanString(typeof(T_Context).ToString()),
                                                 StringHelper.CleanString(EntityType.ToString())),
                            ItemContainerStyle = new System.Windows.Style { TargetType = typeof(ListViewItem) }
                        };

                        var gridView = new GridView();
                        #region Object ToString
                        {
                            GridViewColumn col = new GridViewColumn
                            {
                                Header = "Object",
                                Width = Double.NaN,
                                CellTemplate = new DataTemplate
                                {
                                    VisualTree = UIHelper.Control.ToStringToUIElementFactory(EntityType)
                                }
                            };
                            gridView.Columns.Add(col);
                        }
                        #endregion
                        #region GridView columns
                        foreach (var property in EntityType.GetProperties())
                        {
                            if (Attribute.IsDefined(property, typeof(UIHelper.Attributes.NotUIVisible)))
                                continue;

                            GridViewColumn col = new GridViewColumn
                            {
                                Header = property.Name,
                                Width = Double.NaN,
                                CellTemplate = new DataTemplate
                                {
                                    VisualTree = PropertyToUIElementFactoryAdapter(context)(property)
                                }
                            };
                            gridView.Columns.Add(col);
                        }
                        #endregion

                        #region Save/Delete buttons
                        //GridViewColumn buttonsCol = new GridViewColumn { Width = Double.NaN };

                        //var btnsPanelFactory = SaveAndDeleteButtonspanelFactory(EntityType);
                        //var btnDeleteFactory = btnsPanelFactory.FirstChild.NextSibling;
                        //btnDeleteFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((sender, e) =>
                        //{
                        //    listView.Items.Refresh();
                        //}));

                        //buttonsCol.CellTemplate = new DataTemplate { VisualTree = btnsPanelFactory };
                        //gridView.Columns.Add(buttonsCol);
                        #endregion

                        listView.View = gridView;

                        return listView;
                    }

                    static public GridView  GridViewOf(Type EntityType)
                    {
                        return GridViewOf(from property
                                                 in EntityType.GetProperties()
                                                 where !Attribute.IsDefined(property, typeof(UIHelper.Attributes.NotUIVisible))
                                                 select property.Name);
                    }
                    static public GridView  GridViewOf(IEnumerable<String> colsName)
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

                    static public void      SetListViewColums(Type EntityType, IEnumerable<string> propertiesName, ref ListView listView)
                    {
                        listView.View = GridViewOf(propertiesName);
                        listView.ItemContainerStyle = new System.Windows.Style();
                        listView.ItemContainerStyle.TargetType = typeof(ListViewItem);
                    }
                    static public void      SetListViewColums(Type EntityType, ref ListView listView)
                    {
                        listView.View = GridViewOf(EntityType);
                        listView.ItemContainerStyle = new System.Windows.Style();
                        listView.ItemContainerStyle.TargetType = typeof(ListViewItem);
                        // listView.ItemsSource = CopyDatasToList(EntityType);
                    }
                    static public void      SetListViewColums<T_Entity>(ref ListView listView)
                    {
                        SetListViewColums(typeof(T_Entity), ref listView);
                    }

                    static public ListView  ToListView<T_Entity>()
                         where T_Entity : class, new()
                    {
                        return ToListView(typeof(T_Entity));
                    }
                    static public ListView  ToListView(Type entityType)
                    {
                        ListView listView = new ListView
                        {
                            Name = string.Format("listView_{0}_{1}",
                            StringHelper.CleanString(typeof(T_Context).ToString()),
                            StringHelper.CleanString(entityType.ToString())),
                        };
                        SetListViewColums(entityType, ref listView);
                        return listView;
                    }
                    static public ListView  ToListView<T_Entity>(ref T_Context context)
                         where T_Entity : class, new()
                    {
                        var view = ToListView(typeof(T_Entity));
                        view.ItemsSource = context.Set<T_Entity>().Local;
                        return view;
                    }
                    static public ListView  ToListView(Type entityType, T_Context context)
                    {
                        var view = ToListView(entityType);
                        view.ItemsSource = context.Set(entityType).Local;
                        return view;
                    }
                    static public ListView  ToRestrictedListView<T_Entity>(IEnumerable<string> propertiesName)
                        where T_Entity : class, new()
                    {
                        return ToRestrictedListView(typeof(T_Entity), propertiesName);
                    }
                    static public ListView  ToRestrictedListView(Type entityType, IEnumerable<string> propertiesName)
                    {
                        ListView listView = new ListView
                        {
                            Name = string.Format("listView_{0}_{1}",
                            StringHelper.CleanString(typeof(T_Context).ToString()),
                            StringHelper.CleanString(entityType.ToString()))
                        };
                        SetListViewColums(entityType, propertiesName, ref listView);
                        return listView;
                    }
                }
            }

            #region Deprecated
            [ObsoleteAttribute("Obselete : Use DbSetToEditableListView instead.", true)]
            static public WrapPanel DbSetEntryControler(Type EntityType, RoutedEventHandler OnValidateButtonClicked = null)
            {
                var content = new WrapPanel();

                foreach (var property in EntityType.GetProperties())
                {
                    UIElement elem = null;
                    if (property.PropertyType == typeof(bool))
                    {
                        elem = new CheckBox();
                        (elem as CheckBox).Name = property.Name;
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        elem = new ComboBox();
                        (elem as ComboBox).ItemsSource = Enum.GetValues(property.PropertyType)/*.Cast<QWE>()*/;
                        (elem as ComboBox).Name = property.Name;
                    }
                    else
                    {
                        elem = new TextBox();
                        (elem as TextBox).Name = property.Name;
                        (elem as TextBox).Width = 150;
                        if (property.PropertyType == typeof(int))
                            (elem as TextBox).PreviewTextInput += (sender, e) =>
                            {
                                if (!char.IsDigit(e.Text, e.Text.Length - 1))
                                    e.Handled = true;
                            };
                        else if (property.PropertyType != typeof(string))
                            elem.IsEnabled = false;
                    }

                    if (elem == null)
                        throw new NotImplementedException("DBEF::ContextAction<T_Context>::DbSetEntryControler<T_Entity> : Unmanaged type");
                    content.Children.Add(elem);
                    content.Name = String.Format("wrapPanl_{0}_{1}_Controler",
                        StringHelper.CleanString(typeof(T_Context).Name),
                        StringHelper.CleanString(EntityType.Name));
                }

                var validButton = new Button
                {
                    Name = "btn_Validate",
                    Content = "Validate"
                };
                if (OnValidateButtonClicked != null)
                    validButton.Click += OnValidateButtonClicked;
                content.Children.Add(validButton);

                return content;
            }
            [ObsoleteAttribute("Obselete : Use DbSetToEditableListView instead.", true)]
            static public WrapPanel DbSetEntryControler<T_Entity>(RoutedEventHandler OnValidateButtonClicked = null)
                where T_Entity : class, new()
            {
                return DbSetEntryControler(typeof(T_Entity), OnValidateButtonClicked);
            }
            [ObsoleteAttribute("Obselete : Use DbSetToEditableListView instead.", true)]
            static public WrapPanel DbSetListViewControler<T_Entity>(ref ListView listView)
                 where T_Entity : class, new()
            {
                var controler = DbSetEntryControler<T_Entity>();

                listView.SelectionChanged += (sender, e) =>
                {
                    if (e.AddedItems.Count == 0)
                        return;
                    var slistView = sender as ListView;
                    //slistView.SelectedItems.

                    // todo
                };

                return controler;
            }
            [ObsoleteAttribute("Obselete : Use DbSetToEditableListView instead.", true)]
            static public ListBox               DbSetToListBox<T_Entity>()
                 where T_Entity : class, new()
            {
                return DbSetToListBox(typeof(T_Entity));
            }
            [ObsoleteAttribute("Obselete : Use DbSetToEditableListView instead.", true)]
            static public ListBox               DbSetToListBox(Type EntityType)
            {
                var listBox = new ListBox();

                listBox.Name = string.Format("listBox_{0}_{1}",
                    StringHelper.CleanString(typeof(T_Context).ToString()),
                    StringHelper.CleanString(EntityType.ToString()));

                var gridFactory = new FrameworkElementFactory(typeof(Grid));
                gridFactory.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                //gridFactory.SetValue(Border.StyleProperty, new Style());

                #region Columns creation as T_entity property
                foreach (var property in EntityType.GetProperties())
                {
                    var col = new FrameworkElementFactory(typeof(ColumnDefinition));
                    col.SetValue(ColumnDefinition.WidthProperty, new GridLength(150)); // Star ? // new GridLength(1, GridUnitType.Auto)
                    col.SetValue(ColumnDefinition.NameProperty, property.Name);
                    gridFactory.AppendChild(col);
                    // col.SetValue(GridViewColumn.DisplayMemberBinding, property.Name);
                    //col.DisplayMemberBinding = new Binding(property.Name);
                    //col.Header = property.Name;
                    //col.Width = Double.NaN;
                    //gridView.Columns.Add(col);
                }
                #endregion

                #region Columns content
                int it = 0;
                foreach (var property in EntityType.GetProperties())
                {
                    FrameworkElementFactory elemFactory;

                    if (property.PropertyType == typeof(bool))
                    {
                        elemFactory = new FrameworkElementFactory(typeof(CheckBox));
                        elemFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding(property.Name));
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        elemFactory = new FrameworkElementFactory(typeof(ComboBox));
                        elemFactory.SetValue(ComboBox.ItemsSourceProperty, property.PropertyType.GetEnumValues());
                        elemFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding(property.Name));
                    }
                    else
                    {
                        elemFactory = new FrameworkElementFactory(typeof(TextBox));
                        elemFactory.SetBinding(TextBox.TextProperty, new Binding(property.Name));
                    }
                    
                    elemFactory.SetValue(Grid.ColumnProperty, it);
                    gridFactory.AppendChild(elemFactory);
                    it++;
                }
                #endregion

                var template = new DataTemplate()
                {
                    VisualTree = gridFactory
                };

                listBox.ItemTemplate = template;
                listBox.ItemsSource = CopyDatasToList(EntityType);

                return listBox;
            }
            #endregion
            #endregion
        }
    }
}
