using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GCL
{
    public class Reflection
    {
        public static PropertyInfo GetPropertyInfoByName<T>(string name)
            where T : class, new()
        {
            var propertyByName = (from property in typeof(T).GetRuntimeProperties()
                                  where property.Name == name
                                  // where property.DeclaringType == typeof(T)
                                  select property
                    );
            return (propertyByName.Count() == 0 ? null : propertyByName.First());
        }
    }
}
