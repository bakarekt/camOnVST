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

namespace KTPM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            System.Mvc.Engine.Register(this, result => { 
                MainContent.Child = (UIElement)result.View.Content;
            });
        }

        void Home_Click(object sender, RoutedEventArgs e)
        {
            System.Mvc.Engine.Execute("Home");
        }
        void About_Click(object sender, RoutedEventArgs e)
        {
        }
        void Contact_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
