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
using TCC_CCA___Shaking_Table_Control_IHM.src;

namespace TCC_CCA___Shaking_Table_Control_IHM.pages
{
    /// <summary>
    /// Interaction logic for ConfigurationsPage.xaml
    /// </summary>
    public partial class ConfigurationsPage : Page
    {
        public Program Program { get; set; } = ((MainWindow)Application.Current.MainWindow).Program;

        public ConfigurationsPage()
        {
            InitializeComponent();

            Program.ConfigurationsPage = this;
        }
    }
}
