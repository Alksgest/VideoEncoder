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
        private FFMpegWorker FFMpegWorker = new FFMpegWorker();
        private List<string> VideosList = new List<string>();
        private MediaRecords Records;

        private bool IsNewSession = true;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            SetDataToContols();

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.ResolutionWidth = Int32.Parse(WidthTextBox.Text);
            Properties.Settings.Default.ResolutionHeight = Int32.Parse(HeightTextBox.Text);

            Properties.Settings.Default.ChannelsIndex = ChannelsComboBox.SelectedIndex;
            Properties.Settings.Default.BitrateIndex = BitrateComboBox.SelectedIndex;
            Properties.Settings.Default.FramerateIndex = FramerateСomboBox.SelectedIndex;
            Properties.Settings.Default.SamplerateIndex = SamplerateComboBox.SelectedIndex;

            Properties.Settings.Default.OutputPath = TextBoxOutputPath.Text;

            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            WidthTextBox.Text = Properties.Settings.Default.ResolutionWidth.ToString();
            HeightTextBox.Text = Properties.Settings.Default.ResolutionHeight.ToString();

            ChannelsComboBox.SelectedIndex = Properties.Settings.Default.ChannelsIndex;
            BitrateComboBox.SelectedIndex = Properties.Settings.Default.BitrateIndex;
            FramerateСomboBox.SelectedIndex = Properties.Settings.Default.FramerateIndex;
            SamplerateComboBox.SelectedIndex = Properties.Settings.Default.SamplerateIndex;

            TextBoxOutputPath.Text = Properties.Settings.Default.OutputPath;
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
                var files = dialog.FileNames;
                foreach (var file in files)
                    MainListView.Items.Add(new VideoRepresenter(file));
            }
        }

        private async void ButtonEncodeVideosClicked(object sender, RoutedEventArgs e)
        {
            if (!IsNewSession || MainListView.Items.Count == 0)
                return;

            IsNewSession = false;
            int counter = 0;
            UploadProgressBar.Value = 0;

            if (MainListView.SelectedItems.Count == 0)
            {
                UploadProgressBar.Maximum = MainListView.Items.Count;
                foreach (var item in MainListView.Items)
                {
                    Records = GetRecords((item as VideoRepresenter).FullPath);
                    bool result = await FFMpegWorker.EncodeAsync(Records);

                    if (result)
                        UploadProgressBarAsync(UploadProgressBar);

                    ++counter;
                }
            }
            else
            {
                UploadProgressBar.Maximum = MainListView.SelectedItems.Count;
                foreach (var item in MainListView.SelectedItems)
                {
                    Records = GetRecords((item as VideoRepresenter).FullPath);
                    bool result = await FFMpegWorker.EncodeAsync(Records);

                    if (result)
                        UploadProgressBarAsync(UploadProgressBar);

                    ++counter;
                }
            }
            if (counter != MainListView.SelectedItems.Count) ; //todo 

            UploadProgressBarAsync(UploadProgressBar, true);

            IsNewSession = true;
        }

        private MediaRecords GetRecords(string path)
        {
            FileInfo info = new FileInfo(path);
            return new MediaRecords(
                path,
                TextBoxOutputPath.Text + @"\" + GetNameOfFile(info) + "_encoded." + info.Extension,
                Defines.BitrateDictionary[BitrateComboBox.Text],
                Byte.Parse(FramerateСomboBox.Text.ToString()),
                UInt32.Parse(WidthTextBox.Text),
                UInt32.Parse(HeightTextBox.Text),
                Byte.Parse(Defines.ChannelsDictionary[ChannelsComboBox.Text]),
                UInt32.Parse(Defines.SamplerateDictionary[SamplerateComboBox.Text]));
        }

        //reset == true - set Value to 0;
        private async void UploadProgressBarAsync(ProgressBar pb, bool reset  = false)
        {
            await Task.Run(() =>
            {
                if (!reset)
                    pb.Dispatcher.Invoke(() => pb.Value++,
                        System.Windows.Threading.DispatcherPriority.Background);
                else
                    pb.Dispatcher.Invoke(() => pb.Value = 0,
                        System.Windows.Threading.DispatcherPriority.Background);
            });
        }
        private String GetNameOfFile(FileInfo info)
        {
            var name = info.Name;
            var splitedName = name.Split('.');
            string result = "";
            for (int i = 0; i < splitedName.Length - 1; ++i)
            {
                result += splitedName[i];
            }
            return result;
        }

        private void OpenDestinationFolderButton_Click(object sender, RoutedEventArgs e) => OpenDestinationFolder();
        private void OpenDestinationFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TextBoxOutputPath.Text = dialog.SelectedPath;
                }
            }
        }
    }
}
