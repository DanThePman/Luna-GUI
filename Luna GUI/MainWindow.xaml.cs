using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        internal List<FileManager.CodeSnippetComparison> listCodeSnippetComparisons;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => MessageBox.Show(args.ExceptionObject.ToString());

            InitializeComponent();

            bgWorker.DoWork += (sender, args) =>
            {
                Dispatcher.Invoke(new Action(() => progress1.Visibility = Visibility.Visible));
                listCodeSnippetComparisons = FileManager.CheckCodeSnippets();
            };

            bgWorker.RunWorkerCompleted += (o, args) =>
            {
                try
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
                }
                catch
                {
                    //Windows XP crash here
                    this.ShowMessageAsync("Fehler", "XP-Fehler beim Laden der OnlineSnippets..");
                }
            };
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowManager.MainWindow = this;

            if (!Directory.Exists(FileManager._extensionPath))
            {
                this.ShowMessageAsync("Fehler", "Diese Datei befindet sich nicht im Sublime Text Verzeichnis");
                return;
            }

            if (!Testing.DebugMode)//fill data grid
                bgWorker.RunWorkerAsync();
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
        /// Update CodeSnippets-Button
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

        /*OnCompile + DragDrop*/
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPositionDefinitions())
                Testing.OnCompile(checkBox.IsChecked);
            else
            {
                this.ShowMessageAsync("Fehler", "Positionen nicht gesetzt. Bitte starte den Assistenten");
            }
        }

        /*init compiling (tab change)*/
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

        /// <summary>
        /// Compile only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onCompileButton_Click(object sender, RoutedEventArgs e)
        {
            if (Testing.luapath == null)
            {
                this.ShowMessageAsync("Fehler", "Wähle erst eine Lua-Datei.");
                return;
            }

            var codeAnalysis = CompilingAnalysis.RunCodeAnalysis();
            if (codeAnalysis.Result == CompilingAnalysis.CodeAnalysisResult.CodeFine)
            {
                CompilingAnalysis.CompileLuaFile(codeAnalysis.GetLuaLinesFile());
                codeAnalysis.RemoveLunaFile();
            }
            else
            {
                string compilingWarning = string.Join("\n", codeAnalysis.Announcements.ToArray());
                WindowManager.SetForegroundWindow(this.Title);
                this.ShowMessageAsync("Warnung", "Es wurden folgende Code-Warnungen endeckt:\n" +
                    compilingWarning + "\n\nTrotzdem kompilieren?", MessageDialogStyle.AffirmativeAndNegative).
                    ContinueWith(task => 
                    {
                        if (task.Result == MessageDialogResult.Affirmative)
                        {
                            CompilingAnalysis.CompileLuaFile(codeAnalysis.GetLuaLinesFile());
                            codeAnalysis.RemoveLunaFile();
                        }
                    });
            }
        }
    }
}
