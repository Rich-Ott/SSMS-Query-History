using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace SSMSQueryHistory
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    [Guid("b30aae7c-e969-4d35-8ed1-27c2c3a6228c")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public partial class SSMSQueryHistoryControl : UserControl, ISSMSQueryHistory
    {
        /// <summary>
        /// Constructor for a TestControl
        /// </summary>
        public SSMSQueryHistoryControl()
        {
            InitializeComponent();
            this.StartDate.SelectedDate = DateTime.Now.AddDays(-7);
            this.EndDate.SelectedDate = DateTime.Now;
        }

        private void SearchHistory_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = this.StartDate.SelectedDate ?? DateTime.Now.AddDays(-7);
            DateTime end = this.EndDate.SelectedDate ?? DateTime.Now;
            string search = this.Search.Text.Trim();

            this.SearchResults.ItemsSource = null;
            this.CurrentQuery.Text = String.Empty;

            this.SearchProgress.IsIndeterminate = true;
            SearchHistoryAsync(start, end, search);
        }

        private void SearchHistoryAsync(DateTime start, DateTime end, string search)
        {
            var context = TaskScheduler.FromCurrentSynchronizationContext();
            var task = new Task<List<HistoryEntry>>(() =>
                {
                    List<HistoryEntry> result = GetHistoryItems(start, end, search);
                    return result;
                });
            task.ContinueWith(t =>
                {
                    if (t.Result.Count == 0)
                    {
                        this.SearchResults.ItemsSource = null;
                        this.CurrentQuery.Text = "No results found";
                    }
                    else
                    {
                        this.SearchResults.ItemsSource = t.Result;
                    }
                    this.SearchProgress.IsIndeterminate = false;
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, context);
            task.ContinueWith(t =>
                {
                    StringBuilder output = new StringBuilder();
                    foreach (var exception in t.Exception.InnerExceptions)
                    {
                        if (exception is FileNotFoundException)
                        {
                            FileNotFoundException fnf = (FileNotFoundException)exception;
                            output.AppendFormat("The path to the history files ({0}) does not exist.{1}", fnf.FileName, Environment.NewLine);
                            output.AppendLine("The most likely cause for this error is that you have not yet performed any queries.");
                        }
                        else
                        {
                            output.AppendFormat("{0}: {2}{1}",
                                exception.GetType(),
                                Environment.NewLine,
                                exception.Message);
                        }
                    }
                    this.SearchResults.ItemsSource = null;
                    this.CurrentQuery.Text = output.ToString();
                    this.SearchProgress.IsIndeterminate = false;
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, context);
            task.Start();
        }

        private List<HistoryEntry> GetHistoryItems(DateTime start, DateTime end, string search)
        {
            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SSMS Query History");

            if (!System.IO.Directory.Exists(path))
            {
                throw new FileNotFoundException("The path to the history files does not exist.", path);
            }

            List<HistoryEntry> results = new List<HistoryEntry>();
            DateTime currentDate = start;
            while (currentDate <= end)
            {
                // Set the filename of the history file
                string filename = System.IO.Path.Combine(path,
                    currentDate.ToString("yyyy-MMM-dd"),
                    "QueryHistory.xml");

                XDocument history = new XDocument();

                try
                {
                    history = XDocument.Load(filename);
                }
                catch (DirectoryNotFoundException)
                {
                    // No directory found means there are no entries for that date
                }
                catch (FileNotFoundException)
                {
                    // No history file found means there won't be any entries either
                }

                var entries = from entry in history.Descendants("HistoryEntry")
                              select new HistoryEntry
                              {
                                  Server = entry.Element("Server").Value,
                                  Database = entry.Element("Database").Value,
                                  DateTime = DateTime.Parse(entry.Element("DateTime").Value),
                                  Query = entry.Element("Query").Value,
                              };


                foreach (HistoryEntry entry in entries)
                {
                    if (Regex.IsMatch(entry.Query, search, RegexOptions.IgnoreCase))
                    {
                        results.Add(entry);
                    }
                }
                currentDate = currentDate.AddDays(1);
            }
            return results;
        }

        private void SearchResults_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView lv = sender as ListView;
            GridView gv = lv.View as GridView;

            var width = lv.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            if (gv.Columns.Count > 0)
            {
                double percentage = 1.0 / gv.Columns.Count;

                foreach (var c in gv.Columns)
                {
                    c.Width = width * percentage;
                }
            }
        }
    }
}
