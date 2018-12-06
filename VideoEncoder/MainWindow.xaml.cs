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
        private const string FormatFilter =
            "Files (*.mp3; *.wav; *.wma; *.flac; *.ogg; *.m4a; *.mp4; *.avi; *.wmv; *.mov; *.mkv; *.3gp) |" +
            "*.mp3;*.wav;*.wma;*.flac;*.ogg;*.m4a;*.mp4;*.avi; *.wmv; *.mov; *.mkv; *.3gp";

        private FFMpegWorker FFMpegWorker = new FFMpegWorker();
        private List<string> VideosList = new List<string>();
        DispatcherTimer Timer;
        PlayerState State = PlayerState.Stop;

        enum PlayerState
        {
            Play = 0, 
            Pause = 1,
            Stop = 2
        }
        enum JoinState
        {
            JoinAll = 0,
            JoinSelected = 1,
        }
        List<string> JoinStateList = new List<string> { "Join all", "Join selected" };

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

        private async void ButtonEncodeVideosAsyncClicked(object sender, RoutedEventArgs e)
        {
            List<MediaRecords> list = new List<MediaRecords>();

            if (MainListView.SelectedIndex == -1)
                foreach (VideoRepresenter item in MainListView.Items)
                    list.Add(GetRecords(item.FullPath));
            else
                foreach (VideoRepresenter item in MainListView.SelectedItems)
                    list.Add(GetRecords(item.FullPath));

            await EncodeVideosAsync(list);
        }

        private void OpenDestinationFolderButton_Click(object sender, RoutedEventArgs e) => OpenDestinationFolder(TextBoxOutputPath);

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
        private void ButtonMoveFront_Click(object sender, RoutedEventArgs e) => SwapItem(1);
        private void ButtonMoveBack_Click(object sender, RoutedEventArgs e) => SwapItem(-1);

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
                Timer.Start();
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
            Properties.Settings.Default.JoinStateIndex = ComboBoxJoinState.SelectedIndex;

            Properties.Settings.Default.OutputPath = TextBoxOutputPath.Text;
            Properties.Settings.Default.OutputJoinPath = TextBoxOutputJoinPath.Text;

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
            ComboBoxJoinState.SelectedIndex = Properties.Settings.Default.JoinStateIndex;

            TextBoxOutputJoinPath.Text = Properties.Settings.Default.OutputJoinPath;
            TextBoxOutputPath.Text = Properties.Settings.Default.OutputPath;
        }

        private void OpenDestinationFolder(TextBox txt)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txt.Text = dialog.SelectedPath;
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

        private async Task EncodeVideosAsync(List<MediaRecords> records)
        {
            if (!IsNewSession || records.Count == 0)
                return;

            IsNewSession = false;
            int counter = 0;
            EncodeProgressBar.Value = 0;

            EncodeProgressBar.Maximum = records.Count;

            foreach (var item in records)
            {
                bool result = await FFMpegWorker.EncodeAsync(item);

                if (result)
                    UploadProgressBarAsync(EncodeProgressBar);

                ++counter;
            }

            if (counter != records.Count) ; //todo 

            UploadProgressBarAsync(EncodeProgressBar, true);

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

            this.ListBoxJoin.MouseLeftButtonDown += ListBoxJoin_MouseLeftButtonDown;
        }

        private void ListBoxJoin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as ListBox).SelectedIndex = -1;
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
            foreach (var s in JoinStateList)
                ComboBoxJoinState.Items.Add(s);
        }

        private async void OpenFilesAsync()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = FormatFilter
            };

            if (dialog.ShowDialog() == true)
            {
                var files = dialog.FileNames;

                await SetDataToContainers(files);
            }
        }

        private Task SetDataToContainers(string[] files)
        {
            return Task.Run(() =>
           {
               foreach (var file in files)
               {
                   var vr = new VideoRepresenter(file);
                   vr.GetFirstFrame();

                   MainListView.Dispatcher.Invoke(() => MainListView.Items.Add(vr), DispatcherPriority.Background);
                   PreviewListView.Dispatcher.Invoke(() => PreviewListView.Items.Add(vr), DispatcherPriority.Background);
                   ListBoxJoin.Dispatcher.Invoke(() => ListBoxJoin.Items.Add(vr), DispatcherPriority.Background);
               }
           });
        }

        private async void ButtonJoin_Click(object sender, RoutedEventArgs e)
        {
            //JoinProgressBar

            JoinState state = (JoinState)ComboBoxJoinState.SelectedIndex;
            var mediaRecordsList = new List<MediaRecords>();
            switch (state)
            {
                case JoinState.JoinAll:
                    {
                        foreach (VideoRepresenter videos in ListBoxJoin.Items)
                            mediaRecordsList.Add(GetRecords(videos.FullPath));
                        JoinProgressBar.Maximum = mediaRecordsList.Count;
                        await JoinVideos(mediaRecordsList);
                    }
                    break;
                case JoinState.JoinSelected:
                    {
                        foreach (VideoRepresenter videos in ListBoxJoin.SelectedItems)
                            mediaRecordsList.Add(GetRecords(videos.FullPath));
                        JoinProgressBar.Maximum = mediaRecordsList.Count;
                        await JoinVideos(mediaRecordsList);
                    }
                    break;
                default:
                    break;
            }
            UploadProgressBarAsync(JoinProgressBar, true);
        }

        private async Task JoinVideos(List<MediaRecords> records)
        {
            if (MainListView.Items.Count == 0)
                return;
            List<MediaRecords> forJoin = records;

            if (EncodeBeforJoinCheckBox.IsChecked.Value)
            {
                await EncodeVideosAsync(records);
                forJoin = new List<MediaRecords>();
                foreach (var item in records)
                {
                    var tmp = item;
                    tmp.InPath = item.OutPath;
                    forJoin.Add(tmp);
                }
            }

            DateTime curentTime = DateTime.Now;
            string outputName = "joined." + curentTime.ToString("yyyy_mm_dd_hh_MM_ss_") + (ListBoxJoin.Items[0] as VideoRepresenter).Extension;
            bool result = await FFMpegWorker.JoinVideosAsync(forJoin, TextBoxOutputJoinPath.Text + "\\" + outputName);

            if (result)
                UploadProgressBarAsync(JoinProgressBar);
        }

        private void SwapItem(int direction)
        {
            var selectedIndex = ListBoxJoin.SelectedIndex;

            if (selectedIndex == -1)
                return;

            if (selectedIndex + direction >= ListBoxJoin.Items.Count)
            {
                var item = ListBoxJoin.SelectedItem;
                ListBoxJoin.Items.RemoveAt(selectedIndex);
                ListBoxJoin.Items.Insert(0, item);
                ListBoxJoin.SelectedIndex = 0;
                ListBoxJoin.Focus();
            }
            else if (selectedIndex + direction < 0)
            {
                var item = ListBoxJoin.SelectedItem;
                ListBoxJoin.Items.RemoveAt(selectedIndex);
                ListBoxJoin.Items.Insert(ListBoxJoin.Items.Count, item);
                ListBoxJoin.SelectedIndex = ListBoxJoin.Items.Count - 1;
                ListBoxJoin.Focus();
            }
            else
            {
                var item = ListBoxJoin.SelectedItem;
                ListBoxJoin.Items.RemoveAt(selectedIndex);
                ListBoxJoin.Items.Insert(selectedIndex + direction, item);
                ListBoxJoin.SelectedIndex = selectedIndex + direction;
                ListBoxJoin.Focus();
            }
        }

        private void ButtonOpenDestinationFolder_Click(object sender, RoutedEventArgs e) => OpenDestinationFolder(TextBoxOutputJoinPath);

        private void ListBoxJoin_DragOver(object sender, DragEventArgs e)
        {
            return;
            if(e.Data.GetDataPresent("Object"))
            {
                if(e.KeyStates == DragDropKeyStates.LeftMouseButton)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        private void ListBoxJoin_Drop(object sender, DragEventArgs e)
        {
            return;
            if(!e.Handled)
            {
                ListBox box = sender as ListBox;
                UIElement el = e.Data.GetData("Object") as UIElement;

                if(box != null && el != null)
                {

                }
            }
        }
    }
}
