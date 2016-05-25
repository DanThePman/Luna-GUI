﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Luna_GUI._Compiling;
using MahApps.Metro.Controls.Dialogs;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;

namespace Luna_GUI
{
    public class LocalDataGidSnippet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        private string _localSnippetName;

        public string Snippet
        {
            get { return _localSnippetName; }
            set
            {
                _localSnippetName = value;
                NotifyPropertyChanged("SnippetNameSet");
            }
        }

        private bool _IsMissing;

        public bool Fehlend
        {
            get { return _IsMissing; }
            set
            {
                _IsMissing = value;
                NotifyPropertyChanged("Missing");
            }
        }


        private bool _IsRemoved;

        public bool Entfernt
        {
            get { return _IsRemoved; }
            set
            {
                _IsRemoved = value;
                NotifyPropertyChanged("Removed");
            }
        }

        private bool CodeChanged;

        public bool Veraltet
        {
            get { return CodeChanged; }
            set
            {
                CodeChanged = value;
                NotifyPropertyChanged("CodeChanged");
            }
        }

        private int CodeChanges;

        public int Änderungen
        {
            get { return CodeChanges; }
            set
            {
                CodeChanges = value;
                NotifyPropertyChanged("CodeChangesAmount");
            }
        }


        public LocalDataGidSnippet(string localSnippetName, bool missing, bool removed, bool codechange,
            int codeChangeAmount)
        {
            this.Snippet = localSnippetName;
            this.Fehlend = missing;
            this.Entfernt = removed;
            this.Veraltet = codechange;
            this.Änderungen = codeChangeAmount;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public ObservableCollection<LocalDataGidSnippet> LocalSnippetCollection { get; set; } =
            new ObservableCollection<LocalDataGidSnippet>();

        private readonly BackgroundWorker bgWorker = new BackgroundWorker();
        private List<FileManager.CodeSnippetComparison> listCodeSnippetComparisons;

        public MainWindow()
        {
            InitializeComponent();

            bgWorker.DoWork += (sender, args) =>
            {
                Dispatcher.Invoke(new Action(() => progress1.Visibility = Visibility.Visible));
                listCodeSnippetComparisons = FileManager.CheckCodeSnippets();
            };

            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                progress1.Visibility = Visibility.Collapsed;
                LocalSnippetCollection.Clear();

                foreach (FileManager.CodeSnippetComparison snip in listCodeSnippetComparisons)
                {
                    string snipName = snip.snippetName;
                    snipName = snipName.Substring(0, snipName.IndexOf(".sublime-snippet"));

                    LocalSnippetCollection.Add(
                        new LocalDataGidSnippet(snipName, snip.isMissing, snip.gotRemoved, snip.codeChanged,
                            snip.codeChanged ? snip._codeChanges.Count : 0));
                }

                localSnippetGrid.ItemsSource = LocalSnippetCollection;

                if (listCodeSnippetComparisons.All(x => x.uptodate))
                    this.ShowMessageAsync("Information", "Alle Code-Snippets sind aktuell");
            };
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Testing.DebugMode)
            {
                CallDebugThread();
            }

            WindowManager.MainWindow = this;

            if (!Directory.Exists(FileManager._extensionPath))
            {
                this.ShowMessageAsync("Fehler", "Diese Datei befindet sich nicht im Sublime Text Verzeichnis");
                return;
            }

            if (!Testing.DebugMode) //fill data grid
                bgWorker.RunWorkerAsync();
        }

        private void CallDebugThread()
        {
            Thread debugThread = new Thread(() =>
            {
                while (true)
                {
                    if (Keyboard.IsKeyDown(Key.F10))
                    {
                        var codeAnalysis = CompilingAnalysis.RunCodeAnalysis();
                        if (codeAnalysis.Result == CompilingAnalysis.CodeAnalysisResult.CodeFine)
                        {
                            //tnsOutputPath = CompilingAnalysis.CompileLuaFile();
                            MessageBox.Show("fineee");
                        }
                        else
                        {
                            string compilingWarning = string.Join("\n", codeAnalysis.Announcements.ToArray());
                            var messageResult = MessageBox.Show("Es wurden folgende Code-Warnungen endeckt:\n" +
                                compilingWarning + "\n\nTrotzdem kompilieren?", "Warnung", MessageBoxButton.YesNo);

                            if (messageResult == MessageBoxResult.Yes)
                            {
                                //tnsOutputPath = CompilingAnalysis.CompileLuaFile();
                                MessageBox.Show("fineee");
                            }
                        }
                    }
                }
            });
            debugThread.SetApartmentState(ApartmentState.STA);
            debugThread.Start();
        }

        /// <summary>
        /// load code changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void codeChangeTab_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object item = localSnippetGrid.SelectedItem;
            if (item == null)
            {
                tabControl.SelectedIndex = 0; /*Menu*/
                this.ShowMessageAsync("Fehler", "Kein Item aus der Liste ausgewählt");
                return;
            }
            // ReSharper disable once PossibleNullReferenceException
            string id = (localSnippetGrid.SelectedCells[0].Column.GetCellContent(item) as TextBlock).Text
                        + ".sublime-snippet";

            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once PossibleInvalidOperationException
            bool codeChanged = (bool)(localSnippetGrid.Columns[3].GetCellContent(item) as CheckBox).IsChecked;

            if (!codeChanged)
            {
                tabControl.SelectedIndex = 0; /*Menu*/
                this.ShowMessageAsync("Fehler", "Keine Codeänderungen bei '" + id + "' gefunden");
                return;
            }

            richTextBox1.AppendText(FileManager.GetLocalSnippetContent(id));
            richTextBox2.AppendText(FileManager.GetOnlineSnippetContent(id));

            HighligtCodeChanges(id);
        }

        static IEnumerable<TextRange> GetAllWordRanges(FlowDocument document)
        {
            TextPointer pointer = document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);

                    TextPointer start = pointer.GetPositionAtOffset(0);
                    TextPointer end = start.GetPositionAtOffset(textRun.Length);
                    yield return new TextRange(start, end);
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private void HighligtCodeChanges(string snippetName)
        {
            var codeComparison = listCodeSnippetComparisons.First(x => x.snippetName == snippetName);

            foreach (KeyValuePair<string, string> codeChange in codeComparison._codeChanges)
            {
                var oldCode = codeChange.Key;
                var newCode = codeChange.Value;

                foreach (var wordRange in GetAllWordRanges(richTextBox1.Document))
                {
                    if (wordRange.Text.Contains(oldCode))
                    {
                        wordRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.OrangeRed);
                    }
                }
                foreach (var wordRange in GetAllWordRanges(richTextBox2.Document))
                {
                    if (wordRange.Text.Contains(newCode) && newCode != string.Empty)
                    {
                        wordRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.GreenYellow);
                    }
                }
            }
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            progress1.Visibility = Visibility.Visible;

            foreach (FileManager.CodeSnippetComparison comparison in listCodeSnippetComparisons)
            {
                string destLocalPath = FileManager._extensionPath + "\\" + comparison.snippetName;
                if (comparison.gotRemoved)
                {
                    File.Delete(destLocalPath);
                }
                else if (comparison.isMissing || comparison.codeChanged)
                {
                    using (StreamWriter sw = new StreamWriter(destLocalPath))
                    {
                        sw.Write(FileManager.GetOnlineSnippetContent(comparison.snippetName));
                        sw.Close();
                    }
                }
            }

            mainWindow_Loaded(new object(), new RoutedEventArgs());
        }

        private void luaFileButton_Click(object sender, RoutedEventArgs e)
        {
            string luafileName = Testing.OnSelectLuaFile();
            luapathLabel.Content = luafileName;
        }

        /*OnCompile*/
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPositionDefinitions())
                Testing.OnCompile(checkBox.IsChecked);
            else
            {
                this.ShowMessageAsync("Fehler", "Positionen nicht gesetzt. Bitte starte den Assistenten");
            }
        }

        /*init compiling*/
        private void compilingMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\LunaUI_positionOffets.config"))
            {
                this.ShowMessageAsync("Fehler", "LunaUI_positionOffets.config fehlt");
            }
            else
            {
                if (!CheckPositionDefinitions())
                    OffsetReader.FillOffsetList();
                else if (!Testing.DebugMode)
                    this.ShowMessageAsync("Fehler", "Positionen nicht gesetzt. Bitte starte den Assistenten");
            }


            MyResourceManager.ExractExecutableResource(Properties.Resources.libeay32,
                MyResourceManager.GetNameOf(() => Properties.Resources.libeay32), ".dll");
            MyResourceManager.ExractExecutableResource(Properties.Resources.luna,
                MyResourceManager.GetNameOf(() => Properties.Resources.luna), ".exe");
            MyResourceManager.ExractExecutableResource(Properties.Resources.src,
                MyResourceManager.GetNameOf(() => Properties.Resources.src), ".zip");
            MyResourceManager.ExractExecutableResource(Properties.Resources.lua52,
                MyResourceManager.GetNameOf(() => Properties.Resources.lua52), ".dll");
            MyResourceManager.ExractExecutableResource(Properties.Resources.KeraLua,
                MyResourceManager.GetNameOf(() => Properties.Resources.KeraLua), ".dll");
            MyResourceManager.ExractExecutableResource(Properties.Resources.NLua,
                MyResourceManager.GetNameOf(() => Properties.Resources.NLua), ".dll");
        }

        private bool CheckPositionDefinitions()
        {
            bool undef = false;
            using (StreamReader sr = new StreamReader(Environment.CurrentDirectory + "\\LunaUI_positionOffets.config"))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (line.Contains("undef"))
                    {
                        undef = true;
                        break;
                    }
                }
                sr.Close();
            }

            positionInfoLabel.Content = undef ? "Positionen: Undefiniert" : "Positionen: Gesetzt";
            positionInfoLabel.Background = undef ? Brushes.Red : Brushes.LimeGreen;

            return undef;
        }

        /// <summary>
        /// only if lua path selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartOffsetAssistent_Click(object sender, RoutedEventArgs e)
        {
            if (Testing.luapath == null)
            {
                this.ShowMessageAsync("Fehler", "Wähle erst eine Lua-Datei");
                return;
            }

            new OffsetFinderAssistent
            {
                luaExplorerPath = Testing.explorerPathToLuaFile,
                finishCall = () =>
                {
                    positionInfoLabel.Content = "Positionen: Gesetzt";
                    positionInfoLabel.Background = Brushes.LimeGreen;
                },
            }.ShowDialog();
        }

        private void mainWindow_Closing(object sender, CancelEventArgs e)
        {
        }
    }
}
