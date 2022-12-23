
using Microsoft.Win32;
using ParserHtmlWpf.Command;
using ParserHtmlWpf.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace ParserHtmlWpf.ViewModel
{
    public class UrlInfoViewModel : INotifyPropertyChanged
    {
        private int maxTagValue;
        private CancellationTokenSource CancellToken;

        private bool isInProgress;
        private int totalProgress;

        private ICommand loadCommand;
        private ICommand startCommand;
        private ICommand stopCommand;
        public ICommand LoadCommand
        {
            get
            {
                return loadCommand ?? (loadCommand = new RelayCommand(() => LoadFromFile(), () => !isInProgress));
            }
        }
        public ICommand StartCommand
        {
            get
            {
                return startCommand ?? (startCommand = new RelayCommand(() => Start(), () => !isInProgress && UrlInfoModel.Count > 0));
            }
        }
        public ICommand StopCommand
        {
            get
            {
                return stopCommand ?? (stopCommand = new RelayCommand(() => Stop(), () => isInProgress));
            }
        }

        public ObservableCollection<UrlInfoModel> UrlInfoModel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int TotalProgress
        {
            get
            {
                return totalProgress;
            }
            set
            {
                totalProgress = value;
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public UrlInfoViewModel()
        {
            UrlInfoModel = new ObservableCollection<UrlInfoModel>();
        }

        private void LoadFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(openFileDialog.FileName);
                UrlInfoModel.Clear();
                foreach (var url in lines)
                {
                    if (IsUrlValid(url))
                    {
                        UrlInfoModel.Add(new UrlInfoModel(url));
                    }
                }
            }
        }

        public void Start()
        {
            var thread = new Thread(new ThreadStart(SimpleTask));
            thread.Start();
        }

        public void SimpleTask()
        {
            ClearState();
            CancellToken = new CancellationTokenSource();

            var outer = Task.Factory.StartNew(async () =>
            {
                isInProgress = true;

                var tasks = new List<Task>();
                foreach (var item in UrlInfoModel)
                {
                    var task = Task.Factory.StartNew((obj) =>
                    {
                        var CountTagsProgress = new Progress<int>();

                        CountTagsProgress.ProgressChanged += (sender, e) =>
                        {
                            if (e > maxTagValue)
                            {
                                maxTagValue = e;
                            }

                            item.CountTags = e;
                        };
                        var taskUrl = Task.Factory.StartNew(async () =>
                        {
                            await CountTagsInUrl(item.Url, CountTagsProgress, CancellToken.Token);
                        }, CancellToken.Token);
                        taskUrl.Wait();
                    }, CancellToken.Token, TaskCreationOptions.AttachedToParent);
                }

                await Task.WhenAll(tasks.ToArray());
            });
            outer.Wait();
            RecalculateProgress();
            isInProgress = false;
        }

        public Task CountTagsInUrl(string url, IProgress<int> countTagsProgress, CancellationToken token)
        {
            int countTag = 0;
            string content = DownloadUrl(url);

            string[] contentLines = content.Split(new char[] { '>' });
            for (int i = 0; i < contentLines.Length; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return Task.FromException(new OperationCanceledException());
                }
                if (contentLines[i].IndexOf("<a") != -1)
                {
                    countTag++;
                    countTagsProgress.Report(countTag);
                    Thread.Sleep(150);
                }
                var progress = (int)Math.Round((double)100 * i / contentLines.Length);
            }
            countTagsProgress.Report(countTag);

            return Task.CompletedTask;
        }


        private void ClearState()
        {
            maxTagValue = 0;
            TotalProgress = 0;
            foreach (var item in UrlInfoModel)
            {
                item.ProgressBar = 0;
            }
        }

        private void RecalculateProgress()
        {
            foreach (var item in UrlInfoModel)
            {
                item.ProgressBar = 100 * item.CountTags / maxTagValue;
            }
        }

        public void Stop()
        {
            CancellToken.Cancel();
        }


        private string DownloadUrl(string url)
        {
            using (WebClient wcDownload = new WebClient())
            {
                var a = wcDownload.DownloadString(url);
                return wcDownload.DownloadString(url);
            }
        }

        public bool IsUrlValid(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
