using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.MP3;
using CSCore.CoreAudioAPI;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCK_OKIENKO
{
    class Player
    {
        private MainWindow mainWindow;
        private static Player instance;
        private Player(MainWindow window)
        {
            this.mainWindow = window;
            playlist = new List<string>();
            songsTime = new List<TimeSpan>();
            for(int i = 0; i < 10; i++)
            {
                gains[i] = 0d;
            }
        }

        private List<string> playlist;
        private List<TimeSpan> songsTime;

        private ISoundOut _soundOut;
        private static IWaveSource _soundSource;
        private float volume = 0.5f;
        
        public int currentSong = 0;
        public double sliderTime;

        public TimeSpan currentTime;

        //FLAGI DO WPLYWANIA NA DZIAŁANIE WĄTKU
        public Boolean isPlaying = false;
        public Boolean isSliding = false;
        public Boolean isPaused = false;
        public Boolean isLooped = false;
        public Boolean isShuffled = false;
        public Boolean playerWorking = false;
        public Boolean flag = false;

        private Random rnd = new Random();

        private Equalizer eq;
        private double[] gains = new double[10];

        public void setGain(int i, double value)
        {
            gains[i] = value;
            if (eq!=null)
            {
                eq.SampleFilters[i].AverageGainDB = gains[i];
            }
           
        }

        public TimeSpan getSongTime(int i)
        {
            return songsTime[i];
        }

        public void resetEqualizer()
        {
            int i = 0;
            foreach (EqualizerFilter f in eq.SampleFilters)
            {
                f.AverageGainDB = 0;
                gains[i] = 0;
                i++;
            }
        }

        public double getGain(int index)
        {
            return eq.SampleFilters[index].AverageGainDB;
        }

        public static Player getInstance(MainWindow window)
        {
            if (instance == null)
            {
                instance = new Player(window);
            }
            return instance;
        }

        public bool checkTime()
        {
            if (_soundOut != null && _soundOut.WaveSource != null)
            {
                if (isSliding)
                {
                    _soundOut.Pause();
                    _soundOut.WaveSource.SetPosition(currentTime);
                    _soundOut.Resume();
                }
                if (_soundOut.WaveSource.GetPosition() >= _soundOut.WaveSource.GetLength())
                {
                    if (isLooped)
                    {
                        Play();
                    }
                    else
                        playNext();
                    return true;
                }
                mainWindow.Dispatcher.Invoke(() => {
                    currentTime = _soundOut.WaveSource.GetPosition();
                    mainWindow.timeSlider.Value = _soundOut.WaveSource.GetPosition().TotalSeconds;
                    mainWindow.CurrentTime.Content = _soundOut.WaveSource.GetPosition().ToString().Substring(3, 5);
                });
            }
            return false;
        }

        public string getName(int i)
        {
            return Path.GetFileName(playlist[i]);
        }

        #region INICJALIZACJA
        public void initialize(string file)
        {
            playerWorking = true;
            _soundSource = CodecFactory.Instance.GetCodec(file);
            eq = Equalizer.Create10BandEqualizer(_soundSource.ToSampleSource());
            int i = 0;
            foreach (EqualizerFilter f in eq.SampleFilters)
            {
                f.AverageGainDB = gains[i];
                i++;
            }
            _soundOut = GetSoundOut();
            _soundOut.Initialize(eq.ToWaveSource());
            _soundOut.Volume = volume;
        }

        private static ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                return new WasapiOut
                {
                    Device = GetDevice()
                };
            }

            return new DirectSoundOut();
        }

        public static MMDevice GetDevice()
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    return mmdeviceCollection.LastOrDefault();
                }
            }
        }
        #endregion

        #region ODTWARZAANIE
        private void playPause()
        {
            if (_soundOut != null)
            {
                if (isPaused == false)
                {
                    _soundOut.Pause();
                    isPaused = true;
                }
                else
                {
                    _soundOut.Resume();
                    isPaused = false;
                }
            }
        }

        public void choosePlay()
        {
            if (currentSong >= 0 && currentSong < playlist.Count())
            {
                Stop();
                initialize(playlist[currentSong]);
                isPlaying = true;
                mainWindow.Dispatcher.Invoke(() => {
                    mainWindow.timeSlider.Maximum = getSongTime(currentSong).TotalSeconds;
                    mainWindow.SongTime.Content = getSongTime(currentSong).ToString().Substring(3, 5);
                    mainWindow.timeSlider.Value = 0;
                    mainWindow.SongName.Content = getName(currentSong);
                });
                _soundOut.Play();
                flag = true;
            }
        }
        public void Play()
        {
            if (isPlaying == false || isLooped == true)
            {
                initialize(playlist[currentSong]);
                if (_soundOut != null)
                {
                    mainWindow.Dispatcher.Invoke(() => {
                        mainWindow.timeSlider.Maximum = getSongTime(currentSong).TotalSeconds;
                        mainWindow.SongTime.Content = getSongTime(currentSong).ToString().Substring(3, 5);
                        mainWindow.timeSlider.Value = 0;
                        mainWindow.SongName.Content = getName(currentSong);
                    });
                    isPlaying = true;
                    _soundOut.Volume = volume;
                    _soundOut.Play();
                    flag = true;
                }
            }
            else
            {
                playPause();
            }
        }

        public void Stop()
        {
            if (_soundOut != null)
            {
                flag = false;
                _soundOut.Dispose();
                _soundSource.Dispose();
                isPlaying = false;
                isPaused = false;
            }
        }
        #endregion

        #region SKIPOWANIE
        public void playNext()
        {
            if(isShuffled)
            {
                currentSong = rnd.Next();
            }
            else
                currentSong++;
            currentSong = currentSong % playlist.Count;
            mainWindow.Dispatcher.Invoke(() => {
                mainWindow.timeSlider.Maximum = getSongTime(currentSong).TotalSeconds;
                mainWindow.SongTime.Content = getSongTime(currentSong).ToString().Substring(3, 5);
                mainWindow.timeSlider.Value = 0;
                mainWindow.SongName.Content = getName(currentSong);
            });
            if (isPlaying == true)
            {
                choosePlay();
            }

        }

        public void playPrevious()
        {
            if(isShuffled)
            {
                currentSong = rnd.Next() % playlist.Count;
            }
            else
            {
                if (currentSong == 0)
                {
                    currentSong = playlist.Count();
                }
                currentSong--;
            }
            mainWindow.Dispatcher.Invoke(() => {
                mainWindow.timeSlider.Maximum = getSongTime(currentSong).TotalSeconds;
                mainWindow.SongTime.Content = getSongTime(currentSong).ToString().Substring(3, 5);
                mainWindow.timeSlider.Value = 0;
                mainWindow.SongName.Content = getName(currentSong);
            });
            if (isPlaying == true)
            {
                choosePlay();
            }

        }
        #endregion

        #region ZMIANA GŁOŚNOŚCI
        public void setVolume(float volume)
        {
            if (_soundOut != null)
            {
                _soundOut.Volume = volume;
            }
            else
                this.volume = volume;
        }
        #endregion

        #region PLAYLIST
        public List<string> getPlaylist()
        {
            return playlist;
        }
        public int getPlaylistSize()
        {
            return playlist.Count();
        }

        public void savePlaylist(string name)
        {
            File.WriteAllLines(name + ".plr", playlist);
        }

        public void addToPlaylist(string path)
        {
            playlist.Add(path);
            MediaFoundationDecoder decoder = new Mp3MediafoundationDecoder(path);
            songsTime.Add(decoder.GetLength());
        }


        public void loadPlaylist(string path)
        {
            if (Path.GetExtension(path) == ".plr")
            {
                currentSong = 0;
                songsTime.Clear();
                playlist.Clear();
                var files = File.ReadAllLines(path);
                foreach (var s in files)
                {
                    if (File.Exists(s))
                    {
                        playlist.Add(s);
                        MediaFoundationDecoder decoder = new Mp3MediafoundationDecoder(s);
                        songsTime.Add(decoder.GetLength());
                    }
                }
            }
        }
        #endregion
    }
}
