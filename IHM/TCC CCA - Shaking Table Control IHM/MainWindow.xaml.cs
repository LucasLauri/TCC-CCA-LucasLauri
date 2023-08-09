using LucasLauriHelpers.pages;
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
using static LucasLauriHelpers.pages.AlarmsViewerPage;


namespace TCC_CCA___Shaking_Table_Control_IHM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Obj do programa da IHM
        /// </summary>
        public Program Program { get; set; } = new Program();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnDebugerOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Program.DebugManagerPage.ToggleDebugManagerVisibility();
        }

        private void BtnConfigs_Click(object sender, RoutedEventArgs e)
        {
            if(Program.CurrentPage != Program.ProgramPages.Configurations)
                Program.ShowPage(Program.ProgramPages.Configurations);
            else
                Program.ShowPage(Program.ProgramPages.Operation);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Program.Loaded();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.Closing();
        }

        private void BtnAlarmsOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            s_AlarmsViewerPage.ToggleAlarmsViewerVisibility();
        }
    }
}
