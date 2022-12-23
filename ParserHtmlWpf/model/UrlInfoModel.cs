using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ParserHtmlWpf.model
{
    public class UrlInfoModel : INotifyPropertyChanged
    {
        private string url;
        private int countTags;
        private int progressBar;
        private int progressBarValue;


        public UrlInfoModel(string url)
        {
            this.url = url;
        }

        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                if (url == value)
                    return;
                url = value;
                OnPropertyChanged();
            }
        }

        public int CountTags
        {
            get
            {
                return countTags;
            }
            set
            {
                if (countTags == value)
                    return;
                countTags = value;
                OnPropertyChanged();
            }
        }




        public int ProgressBar
        {
            get
            {
                return progressBar;
            }
            set
            {
                if (progressBar == value)
                    return;
                progressBar = value;
                if (progressBarValue < 0)
                    progressBarValue = 0;
                if (progressBarValue > 100)
                    progressBarValue = 100;

                OnPropertyChanged();
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
