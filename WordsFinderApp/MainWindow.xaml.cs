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
using WordsFinderLib;
using Microsoft.Win32;
using System.IO;
using WordsFinderApp.ViewModel;

namespace WordsFinderApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       // private WordsFinder wordsFinder { get; set; }
        private AppViewModel AppViewModel { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            AppViewModel = new AppViewModel(this);
            DataContext = AppViewModel;

            
        }

        private void Button_AddWordToList_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.AddWordToList();
        }

        private void Button_LoadWordsFromFile_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.LoadWordsFromFile();
        }

        private void Button_ClearWordsList_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.ClearWordsList();
            
        }

        private void Button_StartMainAppProcess_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.StartMainProcess();
        }

        private void Button_PauseMainAppProcess_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.PauseMainProcess();
        }

        private void Button_StopMainAppProcess_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.StopMainProcess();
            
        }

        private void Button_ChangePath_Click(object sender, RoutedEventArgs e)
        {
             AppViewModel.CHangeDestinationFolder();

        }

        private void Button_ChangeSearchPath_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.ChangeSearchPath();
        }

        private void Button_SearchFilesInComputer_Click(object sender, RoutedEventArgs e)
        {
            AppViewModel.SearchFilesInAllComputerClick();
        }
    }
}
