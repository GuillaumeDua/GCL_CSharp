using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GCL.WPF_App
{
    public partial class Dashboard : Window
    {
        public Dashboard()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.frame_currentFeature.Content = new Page { Background = Brushes.Black };
        }
        public Dashboard(List<AbstractFeature> features)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.frame_currentFeature.Content = new Page { Background = Brushes.Black };
            AddFeatures(features);
        }

        public void AddFeatures(List<AbstractFeature> features)
        {
            foreach (var feature in features)
                AddFeature(feature);
        }
        public void AddFeature(AbstractFeature feature)
        {
            AbstractFeature match = null;
            if (_features.TryGetValue(feature.Name, out match)
                && match.Version > feature.Version)
                return;

            #region labelInstance
            var featureNameButton = new Label();
            var featureName = feature.Name.Replace(" ", "");

            featureNameButton.Name = featureName;
            featureNameButton.Content = feature.Name;
            featureNameButton.Style = Application.Current.Resources["label_FeatureName_Desactivated"] as Style;
            this._features[featureName] = feature;
            #endregion

            featureNameButton.MouseLeftButtonUp += ((sender, e) =>
            {
                if (e.ClickCount != 1)
                    return;

                this.SelectFeature(featureName);
            });

            this.panel_featuresButtons.Children.Add(featureNameButton);

            if (this.frame_currentFeature.Content == null)
            {
                SelectFeature(this._features.First().Key);
            }
        }

        public void SelectFeature(string featureName)
        {
            Label button = null;
            foreach (var featureButton in this.panel_featuresButtons.Children)
            {
                if ((featureButton as Label).Name == featureName)
                    { button = featureButton as Label;  break; }
            }
            if (button == null)
                throw new NullReferenceException("gcl.Dashboard.SelectFeature");

            if (this.frame_currentFeature.Content == this._features[featureName].Page)
                return;

            foreach (var featureButton in this.panel_featuresButtons.Children)
            {
                (featureButton as Label).Style = Application.Current.Resources["label_FeatureName_Desactivated"] as Style;
            }

            button.Style = Application.Current.Resources["label_FeatureName_Activated"] as Style;
            this.frame_currentFeature.Content = this._features[featureName].Page;
        }


        private Dictionary<string, AbstractFeature> _features = new Dictionary<string, AbstractFeature>();
    }
}
