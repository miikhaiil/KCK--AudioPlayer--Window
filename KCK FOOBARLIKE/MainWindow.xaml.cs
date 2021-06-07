using Microsoft.Win32;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using System.IO;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace KCK_OKIENKO
{
    public class Song
    {
        public string name { get; set; }
        public string duration { get; set; }
    }

    public partial class MainWindow : Window
    {
        Player player;
        Thread thread;
        public void startAutoPlay()
        {
            thread = new Thread(() => autoPlay());
            thread.Start();
        }

        public void autoPlay()
        {
            while (true)
            {
                if (player.flag == true)
                {
                    player.checkTime();
                }
                Thread.Sleep(100);
            }
        }

        private List<String> GetFiles(string path, string pattern)
        {
            var files = new List<string>();
            var directories = new string[] { };

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                directories = Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException) { }

            foreach (var directory in directories)
                try
                {
                    files.AddRange(GetFiles(directory, pattern));
                }
                catch (UnauthorizedAccessException) { }

            return files;
        }

        public MainWindow()
        {
            player = Player.getInstance(this);
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            ItemsContainer.Foreground = new SolidColorBrush(Colors.Orange);
            startAutoPlay();

        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            if(player.isPlaying)
            {
                player.Stop();
            }
            thread.Abort();
            Close();
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
                WindowState = WindowState.Normal;
        }

        private void MinimalizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void onAddFilesClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Mp3 files (*.mp3)|*.mp3";
            List<string> files = new List<string>();
            int counter=0;
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                foreach (string path in dialog.FileNames)
                {
                    player.addToPlaylist(path);
                    files.Add(path);
                }
                while(ItemsContainer.Items.Count>0)
                {
                    ItemsContainer.Items.RemoveAt(0);
                }
                for (int i = 0; i < player.getPlaylistSize(); i++)
                {
                    ItemsContainer.Items.Add(new Song() { name = player.getName(i), duration = player.getSongTime(i).ToString().Substring(3, 5) });
                }
                
            }  
        }

        private void onAddFolderButtonClick(object sender, RoutedEventArgs e)
        {
            List<String> paths = null;
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if(dialog.ShowDialog() == true)
            {
                paths = GetFiles(dialog.SelectedPath, "*.mp3");
                if (paths.Count != 0)
                {
                    foreach (string path in paths)
                    {
                        player.addToPlaylist(path);
                    }
                    while (ItemsContainer.Items.Count > 0)
                    {
                        ItemsContainer.Items.RemoveAt(0);
                    }
                    for (int i = 0; i < player.getPlaylistSize(); i++)
                    {
                        ItemsContainer.Items.Add(new Song() { name = player.getName(i), duration = player.getSongTime(i).ToString().Substring(3, 5) });
                    }
                }
                else
                    MessageBox.Show("There is no songs to load");
            }
        }

        private void onLoadPlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Playlist file (*.plr)|*.plr";
            dialog.Multiselect = true;
            ItemsContainer.SelectedIndex = -1;
            if (dialog.ShowDialog() == true)
            {
                while (ItemsContainer.Items.Count > 0)
                {
                    ItemsContainer.Items.RemoveAt(0);
                }
                foreach (string path in dialog.FileNames)
                {
                    player.loadPlaylist(path);
                }
                for (int i = 0; i < player.getPlaylistSize(); i++)
                {
                    ItemsContainer.Items.Add(new Song() { name = player.getName(i), duration = player.getSongTime(i).ToString().Substring(3, 5) });
                }
                    
            }
        }

        private void onSavePlaylistButtonClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Playlist file (*.plr)|*.plr";
            if(dialog.ShowDialog() == true)
            {
                File.WriteAllLines(dialog.FileName, player.getPlaylist());
            }
        }

        private void onStopButtonClick(object sender, RoutedEventArgs e)
        {
            CurrentTime.Content = "00:00";
            player.Stop();
        }

        private void SelectItem(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsContainer.SelectedIndex != -1)
            {
                SongTime.Content = player.getSongTime(player.currentSong).ToString().Substring(3, 5);
            }
            else
            {
                SongTime.Content = "";
                SongName.Content = "";
                return;
            }
                
            if (!player.isPaused)
            {
                player.currentSong = ItemsContainer.SelectedIndex-1;
                player.playNext();
            }
            else
                player.currentSong = ItemsContainer.SelectedIndex-1;
           
        }

        private void onPlayButtonClick(object sender, RoutedEventArgs e)
        {
            if(player.getPlaylistSize()>0)
            {
                if (ItemsContainer.SelectedIndex == -1)
                {
                    ItemsContainer.SelectedIndex = 0;
                    player.Play();
                }

                if (ItemsContainer.SelectedIndex >= 0)
                {
                    if (!player.isPlaying)
                    {
                        CurrentTime.Content = "00:00";
                        player.Play();
                    }
                    else if (player.isPlaying && !player.isPaused)
                    {
                        player.choosePlay();

                    }
                    else if (player.isPlaying && player.isPaused)
                    {
                        player.Play();
                    }
                }
            }
        }

        private void onPauseButtonClick(object sender, RoutedEventArgs e)
        {
            if (player.isPlaying)
            {
                player.Play();
            }
            else
                return;
        }

        private void onPreviousButtonClick(object sender, RoutedEventArgs e)
        {
            if(player.getPlaylistSize()>0)
            {
                if (ItemsContainer.SelectedIndex == -1)
                {
                    ItemsContainer.SelectedIndex = ItemsContainer.Items.Count;
                    player.Play();
                }
                if (player.isPlaying)
                {
                    player.playPrevious();
                    ItemsContainer.SelectedIndex = player.currentSong;
                }
                else
                {
                    player.playPrevious();
                    ItemsContainer.SelectedIndex = player.currentSong;
                }
            }
        }

        private void onNextButtonClick(object sender, RoutedEventArgs e)
        {
            if(player.getPlaylistSize()>0)
            {
                if (ItemsContainer.SelectedIndex == -1)
                {
                    ItemsContainer.SelectedIndex = 1;
                    player.currentSong = 0;
                    player.Play();
                }
                if (player.isPlaying)
                {
                    player.playNext();
                    ItemsContainer.SelectedIndex = player.currentSong;
                }
                else
                {
                    player.playNext();
                    ItemsContainer.SelectedIndex = player.currentSong;
                }
            }
        }

        private void onLoopButtonClick(object sender, RoutedEventArgs e)
        {
            player.isLooped = !player.isLooped;
            if(player.isLooped==true)
            {
                player.isShuffled = false;
                Loop.Background = new SolidColorBrush(Colors.Gray);
                Shuffle.Background = null;
            }
            else
            {
                Loop.Background = null;
            }
            

        }

        private void onShuffleButtonClick(object sender, RoutedEventArgs e)
        {
            player.isShuffled = !player.isShuffled;
            if (player.isShuffled == true)
            {
                player.isLooped = false;
                Loop.Background = null;
                Shuffle.Background = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                Shuffle.Background = null;
            }
        }

        private void onMuteButtonClick(object sender, RoutedEventArgs e)
        {
            player.setVolume(0);
            volumeSlider.Value = 0;
        }

        private void onMaxVolButtonClick(object sender, RoutedEventArgs e)
        {
            player.setVolume(1);
            volumeSlider.Value = 1;
        }

        private void volumeChanged(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            player.setVolume((float)volumeSlider.Value);
        }

        private void timeSkipDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            player.sliderTime = timeSlider.Value;
            CurrentTime.Content = player.currentTime.ToString().Substring(3, 5);
            player.currentTime = TimeSpan.FromSeconds(player.sliderTime);

        }

        private void timeSkipStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            player.isSliding = true;
            player.isPaused = true;
        }

        private void timeSkipCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            player.isPaused = false;
            player.isSliding = false;
        }

        private void onResetButtonClick(object sender, RoutedEventArgs e)
        {
            string name = "eq";
            for(int i = 0; i < 10; i++)
            {
                var slider = (Slider)this.FindName(name + i);
                slider.Value = 20;
                player.setGain(i, slider.Value-20);
                var label = (Label)this.FindName(name + i + "Label");
                label.Content = "0dB";
            }
        }

        private void eqChange(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var slider = (Slider)sender;
            switch (slider.Name)
            {
                case "eq0":
                    player.setGain(0, slider.Value - 20);
                    eq0Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq1":

                    player.setGain(1, slider.Value - 20);
                    eq1Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq2":

                    player.setGain(2, slider.Value - 20);
                    eq2Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq3":

                    player.setGain(3, slider.Value - 20);
                    eq3Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq4":

                    player.setGain(4, slider.Value - 20);
                    eq4Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq5":

                    player.setGain(5, slider.Value - 20);
                    eq5Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq6":

                    player.setGain(6, slider.Value - 20);
                    eq6Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq7":

                    player.setGain(7, slider.Value - 20);
                    eq7Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq8":

                    player.setGain(8, slider.Value - 20);
                    eq8Label.Content = slider.Value - 20 + "dB";
                    break;
                case "eq9":

                    player.setGain(9, slider.Value - 20);
                    eq9Label.Content = slider.Value - 20 + "dB";
                    break;
            }
        }
    }
}
