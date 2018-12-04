using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FFMpegWrapper;
using Microsoft.Win32;
using System.IO;
using System.Windows.Threading;

namespace VideoEncoder
{

    public partial class MainWindow : Window
    {
        private FFMpegWorker FFMpegWorker = new FFMpegWorker();
        private List<string> VideosList = new List<string>();
        private MediaRecords Records;
        DispatcherTimer Timer;
        PlayerState State = PlayerState.Stop;

        enum PlayerState
        {
            Play = 0, 
            Pause = 1,
            Stop = 2
        }

        private bool IsNewSession = true;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            SetDataToContols();

            RegisterOnEvents();

            SetTimer();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) => StopPlaying();

        private void PreviewMediaElement_MediaEnded(object sender, RoutedEventArgs e) => State = PlayerState.Stop;

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (PositionSlider.Value + 1 > PositionSlider.Maximum)
            {
                PositionSlider.Value = PositionSlider.Maximum;
            }
            else
                ++PositionSlider.Value;
        }

        private void ListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as ListView).SelectedIndex = -1;
        }

        private void MainWindow_Closed(object sender, EventArgs e) => SaveSettings();

        private void WidthTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0) || (sender as TextBox).Text.Length >= 4) e.Handled = true;
        }

        private void HeightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0) || (sender as TextBox).Text.Length >= 4) e.Handled = true;
        }

        private void ButtonOpenFilesClick(object sender, RoutedEventArgs e) => OpenFilesAsync();

        private async void ButtonEncodeVideosAsyncClicked(object sender, RoutedEventArgs e) => await EncodeVideosAsync();

        private void OpenDestinationFolderButton_Click(object sender, RoutedEventArgs e) => OpenDestinationFolder();

        private void PlayButton_Click(object sender, RoutedEventArgs e) => StartPlaying();
        private void StopButton_Click(object sender, RoutedEventArgs e) => StopPlaying();
        private void PauseButton_Click(object sender, RoutedEventArgs e) => PausePlaying();

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (State == PlayerState.Play)
            {
                Timer.Stop();
                PreviewMediaElement.Pause();
                PreviewMediaElement.Position = TimeSpan.FromSeconds(PositionSlider.Value);
                PreviewMediaElement.Play();
                LabelStart.Content = PreviewMediaElement.Position.ToString(@"hh\:mm\:ss");
            }
            else
            {
                PreviewMediaElement.Position = TimeSpan.FromSeconds(PositionSlider.Value);
                LabelStart.Content = PreviewMediaElement.Position.ToString(@"hh\:mm\:ss");
            }
        }

        private void PreviewListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) => StartPlaying();
        private void PreviewMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            PositionSlider.Maximum = PreviewMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            LabelStop.Content = PreviewMediaElement.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
        }

        private void StartPlaying()
        {
            if (PreviewListView.Items.Count == 0)
                return;

            if (this.PreviewListView.SelectedIndex == -1 && PreviewMediaElement.Source == null)
                PreviewListView.SelectedIndex = 0;


            switch (State)
            {
                case PlayerState.Play:
                    {
                        Timer.Stop();
                        PreviewMediaElement.Pause();
                        State = PlayerState.Pause;
                    }
                    break;
                case PlayerState.Pause:
                    {
                        this.PreviewMediaElement.Play();
                        State = PlayerState.Play;
                        Timer.Start();
                    }
                    break;
                case PlayerState.Stop:
                    {
                        PositionSlider.Value = 0;
                        string path = (PreviewListView.SelectedItem as VideoRepresenter).FullPath;
                        PreviewMediaElement.Source = new Uri(path);
                        Timer.Start();
                        this.PreviewMediaElement.Play();
                    }
                    break;
                default:
                    break;
            }
        }

        private void StopPlaying()
        {
            Timer.Stop();
            PositionSlider.Value = 0;
            PreviewMediaElement.Stop();
            State = PlayerState.Stop;
            LabelStart.Content = "00:00:00";
        }

        private void PausePlaying()
        {
            if (State == PlayerState.Pause)
            {
                StartPlaying();
                State = PlayerState.Play;
            }
            else
            {
                Timer.Stop();
                PreviewMediaElement.Pause();
                State = PlayerState.Pause;
            }
        }

        private void SetTimer()
        {
            Timer = new DispatcherTimer();
            Timer.Interval = new TimeSpan(0, 0, 1);
            Timer.Tick += Timer_Tick;
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
        private MediaRecords GetRecords(string path)
        {
            FileInfo info = new FileInfo(path);
            return new MediaRecords(
                path,
                TextBoxOutputPath.Text + @"\" + FFMpegWorker.GetNameOfFile(info) + "_encoded." + info.Extension,
                Defines.BitrateDictionary[BitrateComboBox.Text],
                Byte.Parse(FramerateСomboBox.Text.ToString()),
                UInt32.Parse(WidthTextBox.Text),
                UInt32.Parse(HeightTextBox.Text),
                Byte.Parse(Defines.ChannelsDictionary[ChannelsComboBox.Text]),
                UInt32.Parse(Defines.SamplerateDictionary[SamplerateComboBox.Text]));
        }

        //reset == true - set Value to 0;
        private async void UploadProgressBarAsync(ProgressBar pb, bool reset = false)
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

        private async Task EncodeVideosAsync()
        {
            if (!IsNewSession || MainListView.Items.Count == 0)
                return;

            IsNewSession = false;
            int counter = 0;
            UploadProgressBar.Value = 0;

            if (MainListView.SelectedIndex == -1)
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

        private void RegisterOnEvents()
        {
            this.Closed += MainWindow_Closed;
            this.MainListView.MouseLeftButtonDown += ListView_MouseLeftButtonDown;
            this.PreviewListView.MouseLeftButtonDown += ListView_MouseLeftButtonDown;
            this.PreviewListView.MouseDoubleClick += PreviewListView_MouseDoubleClick;

            this.PositionSlider.ValueChanged += PositionSlider_ValueChanged;
            this.PreviewMediaElement.MediaOpened += PreviewMediaElement_MediaOpened;
            this.PreviewMediaElement.MediaEnded += PreviewMediaElement_MediaEnded;

            this.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
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
        private void OpenFilesAsync()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var files = dialog.FileNames;
                
                foreach (var file in files)
                {                   
                    var vr = new VideoRepresenter(file);
                    vr.GetFirstFrame();

                    MainListView.Items.Add(vr);
                    PreviewListView.Items.Add(vr);
                    ListBoxJoin.Items.Add(vr);               
                }
            }
        }

        private async void ButtonJoinAll_Click(object sender, RoutedEventArgs e)
        {
            if (MainListView.Items.Count == 0)
                return;

            var mediaRecordsList = new List<MediaRecords>();

            foreach(VideoRepresenter videos in MainListView.Items)
                mediaRecordsList.Add(GetRecords(videos.FullPath));

            bool result = await FFMpegWorker.JoinVideosAsync(mediaRecordsList);
        }
    }
}
