using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GCL.MP;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections;

namespace GCL.Containers
{
    [Serializable]
    public class HierarchicalData<T> : INotifyPropertyChanged
        where T : class, new()
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public HierarchicalData() { }
        public HierarchicalData(T value)
        {
            this.data = value;
        }
        public HierarchicalData(T value, HierarchicalData<T> parent_value)
        {
            this.data = value;
            this.parent = parent_value;
        }

        public override string ToString()
        {
            return data.ToString();
        }

        public void addChild(T child_data)
        {
            childrens.Add(new HierarchicalData<T>(child_data, this));
        }
        public void addChild(HierarchicalData<T> child_data)
        {
            childrens.Add(child_data);
            child_data.parent = this;
        }

        public bool is_parent_of(HierarchicalData<T> tree)
        {
            foreach (var node in childrens)
                if (node == tree || node.is_parent_of(tree))
                    return true;
            return false;
        }
        public void refresh_parent()
        {
            foreach (var elem in _childrens)
            {
                elem.parent = this;
                elem.refresh_parent();
            }
        }

        public T data { get { return _data; } set { _data = value; OnPropertyChanged("data"); } }
        private T _data = new T();

        public List<HierarchicalData<T>> nodes()
        {
            var node_list = new List<HierarchicalData<T>>();
            node_list.Add(this);

            foreach (var child in childrens)
            {
                node_list.AddRange(child.nodes());
            }

            return node_list;
        }

        [XmlIgnoreAttribute]
        public HierarchicalData<T> parent { get; set; } = null;
        public ObservableCollection<HierarchicalData<T>> childrens
        {
            get { return (_childrens == null ? _childrens = new ObservableCollection<HierarchicalData<T>>() : _childrens); }
            set { _childrens = value; OnPropertyChanged("childrens"); }
        }
        private ObservableCollection<HierarchicalData<T>> _childrens = null;
    }
}
