using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace WordsFinderLib
{
    public class WordsFinder 
    {
        
        public object lockObject;

        /// <summary>
        /// в этот каталог копируются файлы, и в него сохраняются измененные файлы
        /// </summary>
        public string PathForCopyUpdateFolder { get; set; }

        /// <summary>
        /// //список слов которые будем искать
        /// </summary>
        public List<string> ListOfForbiddenWords { get; set; }
        
       

        /// <summary>
        /// string - имя файла в котором есть искомые слова
        /// FilesPathRecord - имя скопированного файла и имя измененного файла
        /// </summary>
        public Dictionary<string, FilesPathRecord> DictionaryOriginCopiedUpdatedFiles { get; set; }

        public List<string> ListOfAllFiles { get; set; } //список всех найденных файлов

        #region public event EventHandler FlagChanged;
        public event EventHandler FlagChanged;
        protected virtual void OnFlagChanged()
        {
            FlagChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        private bool flagMainFindUpdateOperationIsRun;
        /// <summary>
        /// флаг работы всего поцесса поиска
        /// </summary>
        public bool FlagMainFindUpdateOperationIsRun //==true - поиск идет 
        {
            get { return flagMainFindUpdateOperationIsRun; }
            set
            {
                if (flagMainFindUpdateOperationIsRun != value)
                {
                    flagMainFindUpdateOperationIsRun = value;
                    OnFlagChanged();
                }
            }
        }

        private bool flagMainFindUpdateOperationIsPaused;

        public bool FlagMainFindUpdateOperationIsPaused//==true - поиск на паузе
        {
            get { return flagMainFindUpdateOperationIsPaused; }
            set
            {
                if (flagMainFindUpdateOperationIsPaused != value)
                {
                    flagMainFindUpdateOperationIsPaused = value;
                    OnFlagChanged();
                }
            }
        }


        public bool FlagSearchFilesInComputerFinished { get; set; }//индикатор что SearchFilesInComputer() отработал
        public bool FlagSearchFilesWithForbiddenWordFinished { get; set; }//индикатор что SearchFilesWithForbiddenWords() отработал
        public bool FlagCopySelectedFilesFinished { get; set; }//индикатор работы метода CopySelectedFiles(string folderPath)
        public bool FlagUpdateSelectedFilesFinished { get; set; }//индикатор работы метода UpdateSelectedFiles(string folderPath)
        public bool FlagCommonSearchCopyUpdateMethodFinshed { get; set; }

        public Dictionary<string, int> Top10words { get; set; } //топ 10  самых популярных запрещенных слов
        public KeyValuePair<string, int>[] Top10wordsArray { get; set; } //топ 10  самых популярных запрещенных слов

        public Dictionary<string, int> DictionaryOFUpdatedFilesAndUpdatesCount { get; set; } //словарь файл - колличество в нем замен

        /// <summary>
        /// колличество запущенных задач
        /// </summary>
        public int TaskCount { get; set; }

        #region public event EventHandler Indexer1; 
        public event EventHandler Indexer1;
        protected virtual void OnIndexer1Changed()
        {
            Indexer1?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        #region public int FileSearchIndexer
        private int fileSearchIndexer;
        /// <summary>
        /// отображает текущее кол-во найденных файлов; 
        ///  event EventHandler Indexer1 оповещает о изменении
        /// </summary>
        public int FileSearchIndexer
        {
            get { return fileSearchIndexer; }
            set
            {
                if (fileSearchIndexer != value)
                {
                    fileSearchIndexer = value;
                    OnIndexer1Changed();
                }
            }
        }
        #endregion


        #region public event EventHandler Indexer2; 
        public event EventHandler Indexer2;
        protected virtual void OnIndexer2Changed()
        {
            Indexer2?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        #region public int SearchFilesWithForbiddenWordsIndexer
        private int searchFilesWithForbiddenWordsIndexer;
        /// <summary>
        /// отображает текущий обработанный файл в списке FilesWithForbiddenWords; 
        /// event EventHandler Indexer2 уведомляет о изменении свойства
        /// </summary>
        public int SearchFilesWithForbiddenWordsIndexer
        {
            get { return searchFilesWithForbiddenWordsIndexer; }
            set
            {
                if (searchFilesWithForbiddenWordsIndexer != value)
                {
                    searchFilesWithForbiddenWordsIndexer = value;
                    OnIndexer2Changed();
                }
            }
        }
        #endregion

        #region public event EventHandler Indexer3; 
        public event EventHandler Indexer3;
        protected virtual void OnIndexer3Changed()
        {
            Indexer3?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        #region public int CopySelectedFilesIndexer
        private int copySelectedFilesIndexer;
        /// <summary>
        /// отображает текущий скопированный файл из списка файлов DictionaryOriginCopiedUpdatedFiles; 
        /// event EventHandler Indexer3 уведомляет о изменении свойства
        /// </summary>
        public int CopySelectedFilesIndexer
        {
            get { return copySelectedFilesIndexer; }
            set
            {
                if (copySelectedFilesIndexer != value)
                {
                    copySelectedFilesIndexer = value;
                    OnIndexer3Changed();
                }
            }
        }
        #endregion

        #region public event EventHandler Indexer4; 
        public event EventHandler Indexer4;
        protected virtual void OnIndexer4Changed()
        {
            Indexer4?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        #region public int UpdateSelectedFilesIndexer
        private int updateSelectedFilesIndexer;
        /// <summary>
        /// отображает номер текущего измененного файл из списка файлов DictionaryOriginCopiedUpdatedFiles; 
        /// event EventHandler Indexer4 уведомляет о изменении свойства
        /// </summary>
        public int UpdateSelectedFilesIndexer
        {
            get { return updateSelectedFilesIndexer; }
            set
            {
                if (updateSelectedFilesIndexer != value)
                {
                    updateSelectedFilesIndexer = value;
                    OnIndexer4Changed();
                }
            }
        }
        #endregion


        private List<Task> Tasks { get; set; }

        /// <summary>
        /// отчет по проделанной работе
        /// </summary>
        public List<string> Report { get; set; }


        /// <summary>
        /// делегат для вывода сообщений об ошибках на экран
        /// </summary>
        /// <param name="message"></param>
        public delegate void ShowErrorReport(string message);
        private event ShowErrorReport ErrorNotify;


        public WordsFinder()
        {

            ListOfForbiddenWords = new List<string>();
            Report = new List<string>();
            lockObject = new object();
            //ListOfFilesWithSearchedWords = new List<string>();
            DictionaryOriginCopiedUpdatedFiles = new Dictionary<string, FilesPathRecord>();
            //ListOfCopiedFilesWithSearchedWords = new List<string>();
            ListOfAllFiles = new List<string>();
            FlagSearchFilesInComputerFinished = false;
            FlagSearchFilesWithForbiddenWordFinished = false;
            FlagMainFindUpdateOperationIsRun = false;
            FlagMainFindUpdateOperationIsPaused = false;
            FlagCopySelectedFilesFinished = false;
            FlagUpdateSelectedFilesFinished = false;
            FlagCommonSearchCopyUpdateMethodFinshed = false;
            TaskCount = 0;
            FileSearchIndexer = 0;
            SearchFilesWithForbiddenWordsIndexer = 0;
            CopySelectedFilesIndexer = 0;
            //удаляю лог если он есть
            if (File.Exists(Directory.GetCurrentDirectory() + "\\wordsFinder.log"))
            {
                File.Delete(Directory.GetCurrentDirectory() + "\\wordsFinder.log");
            }


        }


        /// <summary>
        /// Добавляет обработчик вывода на экран возникающих ошибок
        /// </summary>
        /// <param name="report"></param>
        public void AddErrorShowHandler(ShowErrorReport report )
        {
            ErrorNotify += report;
        }

        /// <summary>
        /// Загружает из файла список запрещенных слов  
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true - если загрузка прошла удачно, false - если нет</returns>
        public bool LoadForbiddenWordsList(string path)
        {
            try
            {
                string text = File.ReadAllText(path);
                lock (lockObject)
                {
                    List<string> tempListOfForbiddenWords =text.Split(' ', '\n', (char)13).ToList();
                    foreach (string item in tempListOfForbiddenWords)
                    {
                        if (item != "" )  { AddWordToForbiddenWords(item); }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorNotify?.Invoke($"Не получилось нормально загрузить данные из файла \"{path}\"\nПричина: {ex}");
                return false;
            }
        }

        /// <summary>
        /// очищает список запрещенных слов
        /// </summary>
        public void ClearListOfForbiddenWords()
        {
            lock (lockObject)
            {
                ListOfForbiddenWords.Clear();
            }
        }

        /// <summary>
        /// добавляет слово в список запрещеных слов
        /// </summary>
        /// <param name="word">слово которое нужно добавить</param>
        /// <returns>true - добавлено; false - не добавлено</returns>
        public bool AddWordToForbiddenWords(string word)
        {
            lock (lockObject)
            {
                if (ListOfForbiddenWords.Contains(word))
                {
                    return false;
                }
                ListOfForbiddenWords.Add(word);
                return true;
            }

        }


        /// <summary>
        /// записывает в ListOfAllFiles все файлы(на компьютере) 
        /// переменная filesCount отображает текущее колличество посчитанных файлов
        /// </summary>
        public void SearchFilesInComputer(string path = null)
        {
            
            if (path == null)
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    //перебираю диски которые доступны
                    if (drive.IsReady)
                    {
                        TaskCount++;
                        FolderHandling(drive.RootDirectory.ToString());
                    }
                }
            }
            else
            {
                TaskCount++;
                FolderHandling(path);
            }
            while (TaskCount != 0)//жду пока отработают все потоки поиска файлов
            {
                if (!flagMainFindUpdateOperationIsRun) { break; }
            }
            FlagSearchFilesInComputerFinished = true;

        }

        /// <summary>
        /// ищет все файлы(к которым есть доступ) в пути path и добавляет их в ListOfAllFiles
        /// </summary>
        /// <param name="path"></param>
        public void FolderHandling(string path)
        {


            //перебираю  все файлы в данной папке
            string[] files;
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (UnauthorizedAccessException)
            {//нет доступа к папке

                lock (lockObject)
                {
                    TaskCount--;
                }
                return;
            }
            catch (Exception ex)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\wordsFinder.log", $"FolderHandling({path}) Directory.GetFiles(path), type Exception - {ex}\n");
                lock (lockObject)
                {
                    TaskCount--;
                }
                return;
            }
            foreach (var file in files)
            {

                if (!FlagMainFindUpdateOperationIsRun)
                {
                    TaskCount--;
                    return;

                }
                while (FlagMainFindUpdateOperationIsPaused)
                {
                    //Thread.Sleep(2000);
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        TaskCount--;
                        return;

                    }
                }
                //добавляю файлы в список
                lock (lockObject)
                {
                    //проверка файла на размер - если большой не добавляю - проблемы с открытием
                    long size = (((new FileInfo(file)).Length) / 1024) / 1024;
                    if (size < 500)
                    {
                        ListOfAllFiles.Add(file);
                        FileSearchIndexer++;
                    }
                    else
                    {
                        File.AppendAllText(Directory.GetCurrentDirectory() + "\\wordsFinder.log",
                            $"файл {file} пропущен, его размер - {size} мб\n");
                    }

                }
            }


            //перебираю все папки в данной папке
            string[] folders;
            try
            {
                folders = Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException)//пропускаю папки к которым нет доступа
            {
                lock (lockObject)
                {
                    TaskCount--;
                }
                return;
            }
            catch (Exception ex)
            {
                
                lock (lockObject)
                {
                    File.AppendAllText(Directory.GetCurrentDirectory() + "\\wordsFinder.log", $"FolderHandling({path}) Directory.GetDirectories(path),  - {ex}\n");
                    TaskCount--;
                }
                return;
            }
            if (folders.Length > 0)
            {
                foreach (var folder in folders)
                {
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        lock (lockObject)
                        {
                            TaskCount--;
                        }
                        return;

                    }
                    while (FlagMainFindUpdateOperationIsPaused)
                    {
                        //Thread.Sleep(2000);
                        if (!FlagMainFindUpdateOperationIsRun)
                        {
                            lock (lockObject)
                            {
                                TaskCount--;
                            }
                            return;

                        }
                    }
                    string targetFolder = folder.ToString();
                    lock (lockObject)
                    {
                        TaskCount++;
                    }
                    Task task = Task.Run(() =>
                    {
                        FolderHandling(targetFolder);
                    });
                    
                }
            }
            lock (lockObject)
            {
                //если метод закончил работу до конца
                TaskCount--;
            }
        }


        /// <summary>
        /// перебирает все найденные файлы из  ListOfAllFiles 
        /// и файлы с запрещенными словами добавляю в  FilesWithSearchedWords
        /// отслеживание прогресса CurrentFileInOperations
        /// </summary>
        public void SearchFilesWithForbiddenWords()
        {

            bool flagStop = false;
            TaskCount = 0;
            Tasks = new List<Task>();

            for (int i = 0; i < ListOfAllFiles.Count; i++)
            {//перебираю все найденные файлы

                if (!FlagMainFindUpdateOperationIsRun)
                {
                    flagStop = true;
                }
                while (FlagMainFindUpdateOperationIsPaused)
                {
                    //Thread.Sleep(2000);
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        flagStop = true;
                    }
                    if (flagStop)
                    {
                        break;
                    }
                }

                while (TaskCount >= Environment.ProcessorCount)
                {

                }

                SearchFilesWithForbiddenWordsIndexer = i + 1;

                string file = ListOfAllFiles[i];

                lock (lockObject)
                {
                    TaskCount++;
                }

                Task task = Task.Run(() =>
                {
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        lock (lockObject)
                        {
                            TaskCount--;
                        }
                        return;
                    }
                    if (ContainsListWords(file))
                    {//найдены слова в файле 
                        lock (lockObject)
                        {
                            DictionaryOriginCopiedUpdatedFiles[file] =
                                new FilesPathRecord() { copiedPath = "", updatedPath = "" };
                        }
                    }
                    lock (lockObject)
                    {
                        TaskCount--;
                    }
                });
                Tasks.Add(task);
                if (flagStop)
                {
                    i = ListOfAllFiles.Count + 1;
                }
            }
            Task.WaitAll(Tasks.ToArray());
            FlagSearchFilesWithForbiddenWordFinished = true;
        }

        /// <summary>
        /// определяет есть ли в файле filePath слова из списка ForbiddenWords
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>true - найдены слова; false - слова не найдены </returns>
        public bool ContainsListWords(string filePath)
        {
            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (UnauthorizedAccessException)//нет доступа к файлу
            {
                return false;
            }
            catch (ArgumentNullException)//нет доступа к файлу
            {
                return false;
            }
            catch (IOException)//нет доступа к файлу
            {
                return false;
            }
            foreach (var word in ListOfForbiddenWords)
            {

                if (text.Contains(word))
                {

                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// копирую файлы содержащие запрещенные слова в папку folderPath
        /// </summary>
        /// <param name="folderPath"> папка в которую будут копироваться файлы, null - в текущей папке создается папка Copies</param>
        public void CopySelectedFiles(string folderPath = null)
        {
            if (folderPath != null)
            {
                Directory.SetCurrentDirectory(folderPath);
                folderPath = null;
            }
            if (folderPath == null)
            {//пытаюсь создать в текущей папке папку Copies 
                PathForCopyUpdateFolder = Directory.GetCurrentDirectory() + "\\Copies";
                if (!Directory.Exists(PathForCopyUpdateFolder))
                {//если папк нет создаю ее
                    try
                    {
                        Directory.CreateDirectory(PathForCopyUpdateFolder);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(Directory.GetCurrentDirectory() + "\\wordsFinder.log",
                            $"CopySelectedFiles() - пытаюсь создать папку: {PathForCopyUpdateFolder} , type Exception - {ex}\n");
                        PathForCopyUpdateFolder = Directory.GetCurrentDirectory();
                    }
                }
            }
            CopySelectedFilesIndexer = 0;//индикатор текущего положения копирования файлов
            bool flagStop = false;
            TaskCount = 0;
            List<Task> tasks = new List<Task>();
            foreach (var item in DictionaryOriginCopiedUpdatedFiles)
            {//перебираю список файлов
                string fileOrigin = item.Key;

                if (!FlagMainFindUpdateOperationIsRun)
                {
                    flagStop = true;
                    //return;
                }
                while (FlagMainFindUpdateOperationIsPaused)
                {
                    Thread.Sleep(2000);
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        flagStop = true;
                        //return;
                    }
                    if (flagStop)
                    {
                        break;
                    }
                }

                while (TaskCount >= Environment.ProcessorCount)
                {
                    //Thread.Sleep(50);
                }


                lock (lockObject)
                {
                    TaskCount++;
                }
                string indexFilePath = item.Key;//параметр текущего файла для потока
                Task task = Task.Run(() =>
                {

                    string newFile = GetNewFileName(fileOrigin, PathForCopyUpdateFolder);
                    try
                    {
                        File.Copy(fileOrigin, newFile);
                        //добавляю в список скопированных файлов файл
                        lock (lockObject)
                        {

                            //DictionaryOriginCopiedUpdatedFiles[item.Key].SetCopiedPath(newFile);
                            DictionaryOriginCopiedUpdatedFiles[indexFilePath] = new FilesPathRecord() { copiedPath = newFile, updatedPath = "" };
                        }
                    }
                    catch (Exception ex)
                    {//скопировать не уалось
                        File.AppendAllText(Directory.GetCurrentDirectory() + "\\wordsFinder.log",
                            $"CopySelectedFiles() - пытаюсь скопировать файл: {fileOrigin} в место {newFile} ,  {ex}\n");
                        //убираю запись в DictionaryOriginCopiedUpdatedFiles
                        lock (lockObject)
                        {
                            DictionaryOriginCopiedUpdatedFiles.Remove(indexFilePath);
                        }

                    }

                    lock (lockObject)
                    {
                        CopySelectedFilesIndexer++;
                        TaskCount--;
                    }
                });
                tasks.Add(task);
                if (flagStop)
                {
                    //i = ListOfFilesWithSearchedWords.Count + 1;
                    break;
                }
            }

            Task.WaitAll(tasks.ToArray());

            FlagCopySelectedFilesFinished = true;
        }

        /// <summary>
        /// проверяет нет ли по указанному адресу такого файла.
        /// если есть - то добавляет к имени файла надпись "_copy*",  * - номер копии
        /// </summary>
        /// <param name="oldFileName"></param>
        /// <param name="folderPath"></param>
        /// <returns>уникальное имя для файла</returns>
        private string GetNewFileName(string oldFileName, string folderPath)
        {
            var splits = oldFileName.Split('\\');
            string fileName = splits[splits.Length - 1];
            var splitFileName = fileName.Split('.');

            string newFile = $"{folderPath}\\{fileName}";
            //string newFile = $"{folderPath}\\Copies\\{fileName}";

            int index = 1;
            while (File.Exists(newFile))
            {
                if (splitFileName.Length == 2)
                {
                    newFile = $"{folderPath}\\{splitFileName[0]}_copy{index}.{splitFileName[1]}";
                    //newFile = $"{folderPath}\\Copies\\{splitFileName[0]}_copy{index}.{splitFileName[1]}";
                }
                else
                {
                    newFile = $"{folderPath}\\{splitFileName[0]}_copy{index}";
                    //newFile = $"{folderPath}\\Copies\\{splitFileName[0]}_copy{index}";
                }
                index++;
            }
            return newFile;

        }

        /// <summary>
        /// меняет запрещенные слова в файлах из ListOfFilesWithSearchedWords на "*******";  
        /// отслеживание прогресса CurrentFileInOperations
        /// </summary>
        public void UpdateSelectedFiles()
        {

            //подготавливаю топ10
            Top10words = new Dictionary<string, int>();
            Task ts = Task.Run(() => {
                foreach (string word in ListOfForbiddenWords)
                {
                    Top10words[word] = 0;
                }
            });


            //подготавливаю словарь файл - замены в файле
            DictionaryOFUpdatedFilesAndUpdatesCount = new Dictionary<string, int>();
            Task ts1 = Task.Run(() => {
                //foreach (string file in ListOfCopiedFilesWithSearchedWords)
                //{
                //    DictionaryOFUpdatedFilesAndUpdatesCount[file] = 0;
                //}
                foreach (var file in DictionaryOriginCopiedUpdatedFiles)
                {
                    DictionaryOFUpdatedFilesAndUpdatesCount[file.Value.copiedPath] = 0;
                }

            });

            Task.WaitAll(ts, ts1);

            bool flagStop = false;
            TaskCount = 0;
            UpdateSelectedFilesIndexer = 0;
            List<Task> tasks = new List<Task>();
            //for (int i = 0; i < ListOfCopiedFilesWithSearchedWords.Count; i++)
            foreach (var item in DictionaryOriginCopiedUpdatedFiles)
            {//перебираю все файлы
                string fileToUpdate = item.Value.copiedPath;
                if (!FlagMainFindUpdateOperationIsRun)
                {
                    flagStop = true;
                    //return;
                }
                while (FlagMainFindUpdateOperationIsPaused)
                {
                    Thread.Sleep(2000);
                    if (!FlagMainFindUpdateOperationIsRun)
                    {
                        flagStop = true;
                        //return;
                    }
                    if (flagStop)
                    {
                        break;
                    }
                }

                while (TaskCount >= Environment.ProcessorCount)
                {
                    //Thread.Sleep(50);
                }


                lock (lockObject)
                {
                    TaskCount++;
                }
                string indexFileName = item.Key;//сохраняю параметр для таска 
                Task task = Task.Run(() => {

                    string text;
                    string tempText;
                    text = File.ReadAllText(fileToUpdate);
                    for (int j = 0; j < ListOfForbiddenWords.Count; j++)
                    { //перебираю все слова

                        tempText = text.Replace(ListOfForbiddenWords[j], "*******");
                        if (!tempText.Equals(text))
                        {//произведена замена 
                            //записываю в статистику по слову
                            Top10words[ListOfForbiddenWords[j]] = Top10words[ListOfForbiddenWords[j]] + 1;
                            //записываю в статистику по файлу
                            DictionaryOFUpdatedFilesAndUpdatesCount[fileToUpdate] =
                                DictionaryOFUpdatedFilesAndUpdatesCount[fileToUpdate] + 1;
                        }
                        text = tempText;
                    }
                    string newFile;
                    while (true)//цикл для того чтобы гарантировать что название файла никто не занял
                    {
                        newFile = GetNewUpdatedFileName(fileToUpdate, PathForCopyUpdateFolder);
                        if (File.Exists(newFile))
                        {
                            fileToUpdate = newFile;
                            continue;
                        }
                        try
                        {
                            File.WriteAllText(newFile, text);
                            break;
                        }
                        catch (IOException)
                        {//нет доступа к файлу, его занял другой процесс
                            //пробую получить другое название
                            fileToUpdate = newFile;
                        }
                    }

                    //добавляю название измененного файла в словарь
                    //ListOfUpdatedFilesWithSearchedWords.Add(newFile);
                    string first = DictionaryOriginCopiedUpdatedFiles[indexFileName].copiedPath;
                    DictionaryOriginCopiedUpdatedFiles[indexFileName] =
                        new FilesPathRecord()
                        {
                            copiedPath = DictionaryOriginCopiedUpdatedFiles[indexFileName].copiedPath,
                            updatedPath = newFile
                        };
                    lock (lockObject)
                    {
                        UpdateSelectedFilesIndexer++;
                        TaskCount--;
                    }
                });
                tasks.Add(task);
                if (flagStop)
                {
                    //i = ListOfCopiedFilesWithSearchedWords.Count + 1;
                    break;
                }
            }
            Task.WaitAll(tasks.ToArray());
            Top10wordsArray = Top10words.ToArray();
            //Array.Sort(Top10wordsArray, new SortForTop10Wods());
            FlagUpdateSelectedFilesFinished = true;
        }

        /// <summary>
        /// сортировщик для Top10wordsArray
        /// </summary>
        public class SortForTop10Wods : IComparer<KeyValuePair<string, int>>
        {
            public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
            {
                return (y.Value).CompareTo(x.Value);
            }
        }

        /// <summary>
        /// проверяет нет ли по указанному адресу такого файла.
        /// если есть - то добавляет к имени файла надпись "_updated*",  * - номер копии
        /// </summary>
        /// <param name="oldFileName"></param>
        /// <param name="folderPath"></param>
        /// <returns>уникальное имя для файла </returns>
        private string GetNewUpdatedFileName(string oldFileName, string folderPath)
        {
            var splits = oldFileName.Split('\\');
            string fileName = splits[splits.Length - 1];
            var splitFileName = fileName.Split('.');

            string newFile = $"{folderPath}\\{fileName}";
            //string newFile = $"{folderPath}\\Copies\\{fileName}";

            int index = 1;
            while (File.Exists(newFile))
            {
                if (splitFileName.Length == 2)
                {
                    newFile = $"{folderPath}\\{splitFileName[0]}_updated{index}.{splitFileName[1]}";
                    //newFile = $"{folderPath}\\Copies\\{splitFileName[0]}_updated{index}.{splitFileName[1]}";
                }
                else
                {
                    newFile = $"{folderPath}\\{splitFileName[0]}_updated{index}";
                    //newFile = $"{folderPath}\\Copies\\{splitFileName[0]}_updated{index}";
                }
                index++;
            }
            return newFile;

        }

        /// <summary>
        /// создает файл отчета и записывает его в файл по пути path
        /// </summary>
        /// <param name="path">путь/название файла куда сохранить отчет</param>
        public void MakeReport(string path)
        {
            Array.Sort(Top10wordsArray, new SortForTop10Wods());
            //Report = new List<string>();
            Report.Add("\tТоп 10 самых популярных запрещенных слов:");

            for (int j = 0; j < Top10wordsArray.Length; j++)
            {
                if (j == 10)
                {
                    break;
                }
                Report.Add($"{j + 1}) {Top10wordsArray[j].Key} - {Top10wordsArray[j].Value} раз");

            }
            Report.Add($"");
            Report.Add($"\tСписок обработанных файлов:");
            //for (int i = 0; i < ListOfFilesWithSearchedWords.Count; i++)
            //{
            //    ListOfFilesWithSearchedWords.Sort();
            //    ListOfCopiedFilesWithSearchedWords.Sort();
            //    ListOfUpdatedFilesWithSearchedWords.Sort();
            //    long size = (new FileInfo(ListOfFilesWithSearchedWords[i])).Length;
            //    Report.Add($"{i + 1}. файл {ListOfFilesWithSearchedWords[i]}");
            //    Report.Add($"\tразмер файла - {size / 1024} кБайт ({size} байт)");
            //    Report.Add($"\tскопированый файл - {ListOfCopiedFilesWithSearchedWords[i]}");
            //    Report.Add($"\tизмененый файл  - {ListOfUpdatedFilesWithSearchedWords[i]}");
            //    Report.Add($"\tколичество замен - {DictionaryOFUpdatedFilesAndUpdatesCount[ListOfCopiedFilesWithSearchedWords[i]]} раз");
            //}
            int i = 1;
            foreach (var item in DictionaryOriginCopiedUpdatedFiles)
            {
                long size = (new FileInfo(item.Key)).Length;
                Report.Add($"{i++}. файл {item.Key}");
                Report.Add($"\tразмер файла - {size / 1024} кБайт ({size} байт)");
                Report.Add($"\tскопированый файл - {item.Value.copiedPath}");
                Report.Add($"\tизмененый файл  - {item.Value.updatedPath}");
                Report.Add($"\tколичество замен - {DictionaryOFUpdatedFilesAndUpdatesCount[item.Value.copiedPath]} раз");
            }

            //записываю в файл
            File.WriteAllLines(path + "\\Report.txt", Report);

        }


        /// <summary>
        /// метод запускающий всю цепочку вызовов методов
        /// </summary>
        /// <param name="path">путь к папке где искать файлы(null - ищем во всем компьютере)</param>
        /// <param name="folderForFiles">путь к папке куда будут копироваться файлы(null - создается папка Copies в текущем каталоге)</param>
        public void CommonSearchCopyUpdateMethod(string path = null, string folderForFiles = null)
        {
            FlagMainFindUpdateOperationIsRun = true;
            //Console.WriteLine("SearchFilesInComputer starts");
            SearchFilesInComputer(path);
            //Console.WriteLine("SearchFilesWithForbiddenWords starts");
            SearchFilesWithForbiddenWords();
            //Console.WriteLine("CopySelectedFiles starts");
            CopySelectedFiles(folderForFiles);
            //Console.WriteLine("UpdateSelectedFiles starts");
            UpdateSelectedFiles();
            //Console.WriteLine("MakeReport starts");
            MakeReport(PathForCopyUpdateFolder);
            FlagMainFindUpdateOperationIsRun = false;
            FlagMainFindUpdateOperationIsPaused = false;
            FlagCommonSearchCopyUpdateMethodFinshed = true;
        }


    }
    public struct FilesPathRecord
    {
        public string copiedPath;
        public string updatedPath;
    }
}
