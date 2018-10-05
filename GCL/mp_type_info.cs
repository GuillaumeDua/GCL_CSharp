using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GCL.MP
{
    public class type_info
    {
        static public bool IsRecursive<T_Entity>()
        {
            return IsRecursive(typeof(T_Entity));
        }
        static public bool IsRecursive(Type entityType)
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
                from property
                in entityType.GetProperties()
                where property.PropertyType == entityType
                select property
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

        static public bool ExistsInRecursivity<T_Entity>(T_Entity entity)
        {
            var recursiveProperty = (from property
                    in typeof(T_Entity).GetProperties()
                                     where property.PropertyType == typeof(T_Entity)
                                     select property
                    ).FirstOrDefault();

            if (recursiveProperty == null)
                throw new InvalidOperationException("gcl.mp.type_info.ExistsInRecursivity<E> : E is not recursive");
            var rec = recursiveProperty.GetValue(entity);
            return ExistsInRecursivity(entity, rec, recursiveProperty);
        }
        static public bool ExistsInRecursivity(object entity)
        {
            if (entity == null)
                throw new NullReferenceException("gcl.mp.type_info.ExistsInRecursivity<E> : E instance is null");

            var recursiveProperty = (from property
                    in entity.GetType().GetProperties()
                                     where property.PropertyType == entity.GetType()
                                     select property
                    ).FirstOrDefault();

            if (recursiveProperty == null)
                throw new InvalidOperationException("gcl.mp.type_info.ExistsInRecursivity<E> : E is not recursive");
            var rec = recursiveProperty.GetValue(entity);
            return ExistsInRecursivity(entity, rec, recursiveProperty);
        }
        static public bool ExistsInRecursivity(object entity, object rec, PropertyInfo recursiveProperty)
        {
            if (entity == null)
                throw new NullReferenceException("gcl.mp.type_info.ExistsInRecursivity<E> : E instance is null");

            if (rec == null)
                return false;
            if (recursiveProperty.GetValue(rec).Equals(entity))
                return true;
            return ExistsInRecursivity(entity, recursiveProperty.GetValue(rec), recursiveProperty);
        }

        static public bool IsHierarchyTree<T>()
        {
            return IsHierarchyTree(typeof(T));
        }
        static public bool IsHierarchyTree(Type type)
        {
            return IsRecursive(type) &&   // Parent
                (                         // Children
                    from property
                    in type.GetProperties()
                    where property.PropertyType.IsGenericType &&
                            property.PropertyType.GetInterface("ICollection") != null &&
                            property.PropertyType.GenericTypeArguments[0] == type
                    select property
                ).Count() == 1;
        }
    }
}
