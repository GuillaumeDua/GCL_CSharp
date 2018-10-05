using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace GCL
{
    namespace Serialization
    {
        //[Serializable]
        //[AttributeUsage(AttributeTargets.All)]
        //public class Data //: System.Attribute
        //{ }

        namespace Policies
        {
            public interface Interface
            {
                void I_Serialize<T_ToSerialize>(string filePath, T_ToSerialize data)
                    where T_ToSerialize : new()
                    ;

                void I_DeSerialize<T_ToSerialize>(string filePath, out T_ToSerialize data)
                    where T_ToSerialize : class, new()
                    ;
            }

            public class AsXML : Interface
            {
                public static void Serialize<T_ToSerialize>(string filePath, T_ToSerialize data)                // Can throw
                    where T_ToSerialize : new()
                {
                    XmlSerializer x = new XmlSerializer(typeof(T_ToSerialize));
                    TextWriter writer = new StreamWriter(filePath);
                    x.Serialize(writer, data);  // Make sure every attribut is serializable
                    writer.Close();
                }
                public static T_ToSerialize DeSerialize<T_ToSerialize>(string filePath, out T_ToSerialize data) // Can throw
                    where T_ToSerialize : class, new()
                {
                    {
                        var fileContent = File.ReadAllText(filePath);
                        if (fileContent.Length == 0)
                            return data = null;
                    }

                    XmlSerializer x = new XmlSerializer(typeof(T_ToSerialize));
                    using (TextReader reader = new StreamReader(filePath))
		    {
		            try
		            {
		                return data = x.Deserialize(reader) as T_ToSerialize;

		                //foreach (var property in typeof(T_ToSerialize).GetProperties())
		                //{
		                //    var debugName = property.Name;
		                //    if (typeof(T_ToSerialize).IsSerializable &&
		                //        typeof(T_ToSerialize).BaseType.GetProperty(property.Name) == null) // todo : improve this condition
		                //        property.SetValue(data, data.GetType().GetProperty(property.Name).GetValue(data));
		                //}

		                //foreach (var field in typeof(T_ToSerialize).GetFields())
		                //{
		                //    var debugName = field.Name;
		                //    if (field.IsPublic &&
		                //        !field.IsStatic &&
		                //        typeof(T_ToSerialize).BaseType.GetField(field.Name) == null)  // todo : improve this condition
		                //    {
		                //        field.SetValue(data, data.GetType().GetField(field.Name).GetValue(data));
		                //    }
		                //}
		            }
		            catch (NullReferenceException)
		            { }
		            catch (System.Runtime.Serialization.SerializationException)
		            { }
		    }

                    throw new System.Runtime.Serialization.SerializationException("GCL Serialization");
                }

                public void I_Serialize<T_ToSerialize>(string filePath, T_ToSerialize data)
                    where T_ToSerialize : new()
                {
                    Serialize(filePath, data);
                }
                public void I_DeSerialize<T_ToSerialize>(string filePath, out T_ToSerialize data)
                    where T_ToSerialize : class, new()
                {
                    DeSerialize<T_ToSerialize>(filePath, out data);
                }
            }
            public class AsFile : Interface
            {
                public static void Serialize<T_ToSerialize>(string filePath, T_ToSerialize data)       // Can throw
                    where T_ToSerialize : new()
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    formatter.Serialize(stream, data); // Make sure every attribut is serializable
                    stream.Close();
                }
                public static void DeSerialize<T_ToSerialize>(string filePath, out T_ToSerialize data) // Can throw
                    where T_ToSerialize : new()
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    data = (T_ToSerialize)formatter.Deserialize(stream);

                    //try
                    //{
                    //    foreach (var property in typeof(T_ToSerialize).GetProperties())
                    //    {
                    //        var debugName = property.Name;
                    //        if (typeof(T_ToSerialize).IsSerializable &&
                    //            typeof(T_ToSerialize).BaseType.GetProperty(property.Name) == null) // todo : improve this condition
                    //            property.SetValue(data, data.GetType().GetProperty(property.Name).GetValue(data));
                    //    }

                    //    foreach (var field in typeof(T_ToSerialize).GetFields())
                    //    {
                    //        var debugName = field.Name;
                    //        if (field.IsPublic &&
                    //            !field.IsStatic &&
                    //            typeof(T_ToSerialize).BaseType.GetField(field.Name) == null)  // todo : improve this condition
                    //        {
                    //            field.SetValue(data, data.GetType().GetField(field.Name).GetValue(data));
                    //        }
                    //    }
                    //}
                }

                public void I_Serialize<T_ToSerialize>(string filePath, T_ToSerialize data)
                    where T_ToSerialize : new()
                {
                    Serialize(filePath, data);
                }
                public void I_DeSerialize<T_ToSerialize>(string filePath, out T_ToSerialize data)
                    where T_ToSerialize : class, new()
                {
                    DeSerialize<T_ToSerialize>(filePath, out data);
                }
            }
        }

        public class Helper
        {
            public static void Serialize<T_SerializationPolicy, T_ToSerialize>(string filePath, T_ToSerialize data)    // Can throw
                where T_ToSerialize : new()
                where T_SerializationPolicy : Policies.Interface, new()
            {
                var serializer = new T_SerializationPolicy();
                serializer.I_Serialize(filePath, data);
            }
            public static T_ToSerialize DeSerialize<T_SerializationPolicy, T_ToSerialize>(string filePath)             // Can throw
                where T_ToSerialize : class, new()
                where T_SerializationPolicy : Policies.Interface, new()
            {
                var data = new T_ToSerialize();

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("GCL Serialization");

                try
                {
                    var serializer = new T_SerializationPolicy();
                    serializer.I_DeSerialize<T_ToSerialize>(filePath, out data);
                    return data;
                }
                catch (NullReferenceException)
                { }
                catch (System.Runtime.Serialization.SerializationException)
                { }
                return null;
            }
        }

        namespace Configuration
        {
            using SerializationPolicy = Policies.AsXML;

            public class Auto<T_SerializationPolicy, T_ConfigurationData>
                where T_SerializationPolicy : Policies.Interface, new()
                where T_ConfigurationData : class, new()
            {
                private string _filePath;

                [XmlElement(ElementName = "GCL_Configuration_Auto_InnerData")]
                public T_ConfigurationData Data
                {
                    get;
                    set;
                } = new T_ConfigurationData();

                public Auto(string filePath)
                {
                    _filePath = filePath;
                    if ((Data = Helper.DeSerialize<T_SerializationPolicy, T_ConfigurationData>(_filePath)) == null)
                         Data = new T_ConfigurationData();
                }
                ~Auto()
                {
                    Helper.Serialize<T_SerializationPolicy, T_ConfigurationData>(_filePath, Data);
                }
            }

            // todo : refactor
            /*
            [Serializable]
            public class AutoFromWindow : GCL.Serialization.Configuration.Auto
            {
                public static readonly string CONFIGURATION_FILE_PATH = @".\config.txt";

                public AutoFromWindow()
                    : base(CONFIGURATION_FILE_PATH)
                { }

                private void FieldTextBoxChanged(object sender, TextChangedEventArgs e)
                {
                    try
                    {
                        var textBox = sender as TextBox;
                        this.GetType().GetField(textBox.Name).SetValue(this, textBox.Text);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.instance.Write("[Error]::[System.Exception] : Configuration.FieldTextBoxChanged : " + ex.ToString());
                    }
                }
                public void Load()
                {
                    if ((this.DeSerialize()) == false)
                        this.GetFromWindowForm();
                }

                //public static Configuration GetInstance()
                //{
                //    Configuration config;
                //    if ((config = DeSerialize()) == null)
                //        config = GetFromWindowForm();
                //    return config;
                //}
                protected new void Serialize()
                {
                    try
                    {
                        base.Serialize();
                    }
                    catch (System.Exception ex)
                    {
                        Logger.instance.Write("[Error]::[System.Exception] : Configuration.Serialize : [" + ex.ToString() + "]");
                    }
                }
                public void GetFromWindowForm()
                {
                    var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

                    foreach (var field in this.GetType().GetFields())
                    {
                        if (field.IsPublic && !field.IsStatic)
                        {
                            Grid grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                            grid.Children.Add(new Label { Content = field.Name });
                            var value = field.GetValue(this);
                            var textBox = new TextBox { Text = (value == null ? "" : value).ToString(), Width = 300, Name = field.Name };
                            textBox.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.FieldTextBoxChanged);
                            grid.Children.Add(textBox);

                            stackPanel.Children.Add(grid);
                        }
                    }

                    Window window = new Window { Height = this.GetType().GetFields().Length * 40, Width = 600 }; // Background="#FF1B1A1A"
                    window.Content = stackPanel;

                    window.ShowDialog();
                }
            }*/
        }
    }
}
