using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WordsFinderLib;
using Ookii.Dialogs.Wpf;
using System.Windows.Media.Imaging;

namespace WordsFinderApp.ViewModel
{
    class AppViewModel : INotifyPropertyChanged
    {
        private object LockObject { get; set; }

        internal WordsFinder WordsFinder { get; set; }

        private MainWindow MainWindow { get; set; }

        private bool flagMainFindUpdateOperationIsRun;
        public bool FlagMainFindUpdateOperationIsRun
        {
            get { return flagMainFindUpdateOperationIsRun; }
            set
            {
                flagMainFindUpdateOperationIsRun = value;
                OnPropertyChanged(nameof(FlagMainFindUpdateOperationIsRun));
            }
        }

        private bool flagMainFindUpdateOperationIsPaused;
        public bool FlagMainFindUpdateOperationIsPaused
        {
            get { return flagMainFindUpdateOperationIsPaused; }
            set
            {
                flagMainFindUpdateOperationIsPaused = value;
                OnPropertyChanged(nameof(FlagMainFindUpdateOperationIsPaused));
            }
        }

        private List<string> wordsList;
        public List<string> WordsList
        {
            get { return wordsList; }
            set
            {
                wordsList = value;
                OnPropertyChanged("WordsList");
            }
        }

        private List<string> report;
        public List<string> Report
        {
            get { return report; }
            set
            {
                report = value;
                OnPropertyChanged(nameof(Report));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private void SearchFilesIndexerChanged(object sender, EventArgs e)//
        {
            currentDisp.Invoke(() =>
            {
                MainWindow.TextBloick_FilesCount.Text = WordsFinder.FileSearchIndexer.ToString();
            });
        }

        private void SearchWordsInFilesIndexerChanged(object sender, EventArgs e)//
        {
            currentDisp.Invoke(() =>
            {
                MainWindow.ProgressBar_SearchFilesWithForbidenWords.Maximum = WordsFinder.ListOfAllFiles.Count;
                MainWindow.ProgressBar_SearchFilesWithForbidenWords.Value = WordsFinder.SearchFilesWithForbiddenWordsIndexer;
                
            });
        }
        private void CopySelectedFilesIndexerChanged(object sender, EventArgs e)//
        {
            currentDisp.Invoke(() =>
            {
                MainWindow.ProgressBar_CopyFiles.Maximum = WordsFinder.DictionaryOriginCopiedUpdatedFiles.Count;
                MainWindow.ProgressBar_CopyFiles.Value = WordsFinder.CopySelectedFilesIndexer;
                MainWindow.TextBlock_CopyFilesCount.Text = WordsFinder.DictionaryOriginCopiedUpdatedFiles.Count.ToString();
            });
        }

        private void UpdateSelectedFilesIndexerChanged(object sender, EventArgs e)//
        {
            currentDisp.Invoke(() =>
            {
                MainWindow.ProgressBar_UpdateFiles.Maximum = WordsFinder.DictionaryOriginCopiedUpdatedFiles.Count;
                MainWindow.ProgressBar_UpdateFiles.Value = WordsFinder.UpdateSelectedFilesIndexer;
                MainWindow.TextBlock_UpdateFilesCount.Text = WordsFinder.DictionaryOriginCopiedUpdatedFiles.Count.ToString();
            });
        }

        

        private Dispatcher currentDisp;
        public AppViewModel(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            WordsFinder = new WordsFinder();
            currentDisp = Application.Current.Dispatcher;

            WordsList = WordsFinder.ListOfForbiddenWords;
            Report = WordsFinder.Report;
            MainWindow.Button_StartMainAppProcess.Content = 
                Application.Current.Resources["Title_Button_StartMainAppProcessStart"] as string;
            MainWindow.Button_StartMainAppProcess.IsEnabled = false;
            MainWindow.Button_PauseMainAppProcess.IsEnabled = false;
            MainWindow.Button_StopMainAppProcess.IsEnabled = false;

            #region установка стартового пути для поиска файлов mainWindow.TextBox_SearchPath.Text =
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                //перебираю диски которые доступны
                if (drive.IsReady)
                {
                    mainWindow.TextBox_SearchPath.Text =
                       drive.RootDirectory.ToString();
                    break;
                }
            }
            #endregion

            MainWindow.TextBox_Path.Text = Directory.GetCurrentDirectory();
            LockObject = new object();
            StartInitHelper();
        }

        public void CHangeDestinationFolder()
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                MainWindow.TextBox_Path.Text = dialog.SelectedPath;
                WordsFinder.PathForCopyUpdateFolder = dialog.SelectedPath;
            }

        }

        private void StartInitHelper()
        {
            WordsFinder.AddErrorShowHandler(ErrorShowHandler);//добавляю обработчик вывода ошибок на экран
            WordsFinder.FlagChanged += WordFinderRunFlagChanged;
            WordsFinder.Indexer1 += SearchFilesIndexerChanged;
            WordsFinder.Indexer2 += SearchWordsInFilesIndexerChanged;
            WordsFinder.Indexer3 += CopySelectedFilesIndexerChanged;
            WordsFinder.Indexer4 += UpdateSelectedFilesIndexerChanged;
            //Dispatcher currentDisp = Application.Current.Dispatcher;
            currentDisp.Invoke(() =>
            {
                MainWindow.TextBloick_FilesCount.Text = "0";
                MainWindow.TextBlock_CopyFilesCount.Text = "0";
                MainWindow.TextBlock_UpdateFilesCount.Text = "0";
                MainWindow.ProgressBar_CopyFiles.Value = 0;
                MainWindow.ProgressBar_SearchFilesWithForbidenWords.Value = 0;
                MainWindow.ProgressBar_UpdateFiles.Value = 0;
                //try
                //{
                //    BitmapImage bitmapImage = new();
                //    bitmapImage.BeginInit();
                //    bitmapImage.UriSource = new Uri("/Resources/exam.png");
                //    bitmapImage.EndInit();
                //    MainWindow.Image_ExamBackground.Source = bitmapImage;
                //}
                //catch (Exception ex) { MessageBox.Show(ex.Message); }

            });
            
            
        }

        private void WordFinderRunFlagChanged(object sender, EventArgs e)
        {
            FlagMainFindUpdateOperationIsRun = WordsFinder.FlagMainFindUpdateOperationIsRun;
            FlagMainFindUpdateOperationIsPaused = WordsFinder.FlagMainFindUpdateOperationIsPaused;
        }

        public void StartMainProcess()
        {
            if (MainWindow.Button_StartMainAppProcess.Content.ToString() ==
                Application.Current.Resources["Title_Button_StartMainAppProcessStart"] as string)
            {
                ClearWordsFinder();
                string searchPath = MainWindow.TextBox_SearchPath.Text;
                string copyPath = MainWindow.TextBox_Path.Text;
                bool equalsFalg = MainWindow.TextBox_SearchPath.Text.Equals("Весь компьютер");
                Task.Run(() =>
                {
                    //WordsFinder.CommonSearchCopyUpdateMethod("C:\\temp");
                    //WordsFinder.CommonSearchCopyUpdateMethod("C:\\KMPlayer");
                    //WordsFinder.CommonSearchCopyUpdateMethod("C:\\");
                    if (equalsFalg)
                    {
                        WordsFinder.CommonSearchCopyUpdateMethod(null, copyPath);
                    }
                    else
                    {
                        WordsFinder.CommonSearchCopyUpdateMethod(searchPath, copyPath);
                    }


                    //Dispatcher currentDisp = Application.Current.Dispatcher;
                    currentDisp.Invoke(() =>
                    {
                        MainWindow.Button_StartMainAppProcess.Content =
                            Application.Current.Resources["Title_Button_StartMainAppProcessStart"] as string;
                        MainWindow.TabItem_Words.IsEnabled = true;
                        MainWindow.Button_StartMainAppProcess.IsEnabled = true;
                        MainWindow.Button_PauseMainAppProcess.IsEnabled = false;
                        MainWindow.Button_StopMainAppProcess.IsEnabled = false;
                        
                        MainWindow.ListBox_Report.Items.Refresh();
                    });



                });
                //обработка демонстрации процесса поиска и т.д
                Task.Run(() =>
                {
                    
                });

            }
            MainWindow.TabItem_Words.IsEnabled = false;
            MainWindow.Button_StartMainAppProcess.IsEnabled = false;
            MainWindow.Button_PauseMainAppProcess.IsEnabled = true;
            MainWindow.Button_StopMainAppProcess.IsEnabled = true;
            lock (LockObject)
            {
                WordsFinder.FlagMainFindUpdateOperationIsPaused = false;
            }
            


        }

        public void PauseMainProcess()
        {
            MainWindow.Button_StartMainAppProcess.Content =
                Application.Current.Resources["Title_Button_StartMainAppProcessContinue"] as string;
            MainWindow.Button_PauseMainAppProcess.IsEnabled = false;
            MainWindow.Button_StartMainAppProcess.IsEnabled = true;
            lock (LockObject)
            {
                WordsFinder.FlagMainFindUpdateOperationIsPaused = true;
            }
        }

        public void StopMainProcess()
        {
            MainWindow.Button_StartMainAppProcess.Content =
                Application.Current.Resources["Title_Button_StartMainAppProcessStart"] as string;
            MainWindow.Button_PauseMainAppProcess.IsEnabled = false;
            MainWindow.Button_StopMainAppProcess.IsEnabled = false;
            MainWindow.Button_StartMainAppProcess.IsEnabled = false;

            WordsFinder.FlagMainFindUpdateOperationIsRun = false;



            Task.Run(() =>
            {
                while (!WordsFinder.FlagCommonSearchCopyUpdateMethodFinshed)
                {}
                ClearWordsFinder();
               // Dispatcher currentDisp = Application.Current.Dispatcher;
                currentDisp.Invoke(() =>
                {
                    MainWindow.Button_StartMainAppProcess.IsEnabled = true;
                    MainWindow.TabItem_Words.IsEnabled = true;
                });

            });

            
        }

        private void ClearWordsFinder()
        {
            lock (LockObject)
            {

                List<string> temp = WordsFinder.ListOfForbiddenWords;
                WordsFinder = new WordsFinder();
                WordsFinder.ListOfForbiddenWords = temp;
                WordsList = WordsFinder.ListOfForbiddenWords;
                Report = WordsFinder.Report;
                FlagMainFindUpdateOperationIsPaused = false;
                FlagMainFindUpdateOperationIsRun = false;
                StartInitHelper();
            }
        }


        private void ErrorShowHandler(string message)
        {
            string title = (string)Application.Current.Resources["Title_Error"];
            if (string.IsNullOrEmpty(title)) { title = "Error"; }
            MessageBox.Show(message, title);
        }

        public void ClearWordsList()
        {
            MainWindow.Button_StartMainAppProcess.IsEnabled = false;
            WordsFinder.ClearListOfForbiddenWords();
            MainWindow.ListBox_ForbiddenWords.Items.Refresh();
        }

        public void LoadWordsFromFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Multiselect = false;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                WordsFinder.LoadForbiddenWordsList(dialog.FileName);
                MainWindow.ListBox_ForbiddenWords.Items.Refresh();
            }
            MainWindow.Button_StartMainAppProcess.IsEnabled = (WordsFinder.ListOfForbiddenWords.Count > 0) ? true : false;
        }

        public void AddWordToList()
        {
            if (WordsFinder.AddWordToForbiddenWords(MainWindow.TextBox_AddWordToList.Text))
            {
                MainWindow.TextBox_AddWordToList.Text = "";
                MainWindow.ListBox_ForbiddenWords.Items.Refresh();
            }
            MainWindow.TextBox_AddWordToList.Focus();

            MainWindow.Button_StartMainAppProcess.IsEnabled = (WordsFinder.ListOfForbiddenWords.Count > 0) ? true : false;
            
        }

        public void SearchFilesInAllComputerClick()
        {
            MainWindow.TextBox_SearchPath.Text = "Весь компьютер";       
        }

        public void ChangeSearchPath()
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                MainWindow.TextBox_SearchPath.Text = dialog.SelectedPath;
            }
        }

    }
}
