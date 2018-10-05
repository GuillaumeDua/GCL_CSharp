using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GCL.WPF_App
{
    abstract public class AbstractFeature
    {
        public string Name { get; set; }
        public Page Page { get; set; }
        public Double Version { get; set; }
    }
}
