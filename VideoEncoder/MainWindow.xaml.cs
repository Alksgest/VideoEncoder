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
using FFMpegWrapper;
using Microsoft.Win32;
using System.IO;

namespace VideoEncoder
{

    public partial class MainWindow : Window
    {
        private FFMpegWorker EncodeWorker = new FFMpegWorker();
        private List<string> VideosList = new List<string>();
        private MediaRecords Records;

        private bool IsNewSession = true;

        public MainWindow()
        {
            InitializeComponent();

            SetDataToContols();
        }

        private void SetDataToContols()
        {
            foreach (var s in Defines.ChannelsDictionary.Keys)
                ChannelsComboBox.Items.Add(s);
            foreach (var s in Defines.SamplerateDictionary.Keys)
                SamplerateComboBox.Items.Add(s);
            foreach (var s in Defines.BitrateDictionary.Keys)
                BitrateComboBox.Items.Add(s);
            foreach (var s in Defines.FramerateList)
                FramerateСomboBox.Items.Add(s);
        }

        private void WidthTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0) || (sender as TextBox).Text.Length >= 4) e.Handled = true;
        }

        private void HeightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0) || (sender as TextBox).Text.Length >= 4) e.Handled = true;
        }

        private void ButtonOpenFilesClick(object sender, RoutedEventArgs e) => OpenFiles();

        private void OpenFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var files =  dialog.FileNames;
                foreach (var file in files)
                    MainListView.Items.Add(new VideoRepresenter(file));
            }
        }

        private async void ButtonEncodeVideosClicked(object sender, RoutedEventArgs e)
        {
            if (!IsNewSession || MainListView.SelectedItems.Count <= 0)
                return;

            UploadProgressBar.Maximum = MainListView.SelectedItems.Count;

            IsNewSession = false;
            int counter = 0;

            foreach (var item in MainListView.SelectedItems)
            {
                Records = GetRecords((item as VideoRepresenter).FullPath);
                bool result = await EncodeWorker.EncodeAsync(Records);

                if (result)
                    UploadProgressBar.Value++;
                    //UploadProgressBarAsync(UploadProgressBar, counter);

                ++counter;
            }
            if (counter != MainListView.SelectedItems.Count) ; //todo 

            UploadProgressBar.Value = 0;
            IsNewSession = true;
        }

        private MediaRecords GetRecords(string path)
        {
            return new MediaRecords(
                path,
                VideoTitleTextBox.Text,
                Defines.BitrateDictionary[BitrateComboBox.Text],
                Byte.Parse(FramerateСomboBox.Text.ToString()),
                UInt32.Parse(WidthTextBox.Text),
                UInt32.Parse(HeightTextBox.Text),
                Byte.Parse(Defines.ChannelsDictionary[ChannelsComboBox.Text]),
                UInt32.Parse(Defines.SamplerateDictionary[SamplerateComboBox.Text]));
        }

        private async void UploadProgressBarAsync(ProgressBar pb, int indexPart)
        {
            await Task.Run(() =>
            {
                pb.Dispatcher.Invoke(() => pb.Value = indexPart,
                    System.Windows.Threading.DispatcherPriority.Background);
            });
        }
    }
}
