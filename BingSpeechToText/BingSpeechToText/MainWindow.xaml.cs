// <copyright file="MainWindow.xaml.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Speech-STT-Windows
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.CognitiveServices.SpeechRecognition;
using Microsoft.Win32;

namespace BingSpeechToText
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        /// <summary>
        /// The isolated storage subscription key file name.
        /// </summary>
        private const string IsolatedStorageSubscriptionKeyFileName = "Subscription.txt";

        /// <summary>
        /// The default subscription key prompt message
        /// </summary>
        private const string DefaultSubscriptionKeyPromptMessage = "Paste your subscription key here to start";

        /// <summary>
        /// You can also put the primary key in app.config, instead of using UI.
        /// string subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];
        /// </summary>
        private string _subscriptionKey;

        /// <summary>
        /// The data recognition client
        /// </summary>
        private DataRecognitionClient _dataClient;

        /// <summary>
        /// The microphone client
        /// </summary>
        private MicrophoneRecognitionClient _micClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        #region Events

        /// <summary>
        /// Implement INotifyPropertyChanged interface
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client short phrase.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client short phrase; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientShortPhrase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client dictation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client dictation; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientDictation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is microphone client with intent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is microphone client with intent; otherwise, <c>false</c>.
        /// </value>
        public bool IsMicrophoneClientWithIntent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client short phrase.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client short phrase; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientShortPhrase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client with intent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client with intent; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientWithIntent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is data client dictation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is data client dictation; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataClientDictation { get; set; }

        /// <summary>
        /// Gets or sets subscription key
        /// </summary>
        public string SubscriptionKey
        {
            get
            {
                return _subscriptionKey;
            }
            set
            {
                _subscriptionKey = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the LUIS application identifier.
        /// </summary>
        /// <value>
        /// The LUIS application identifier.
        /// </value>
        private static string LuisAppId => ConfigurationManager.AppSettings["luisAppID"];

        /// <summary>
        /// Gets the LUIS subscription identifier.
        /// </summary>
        /// <value>
        /// The LUIS subscription identifier.
        /// </value>
        private static string LuisSubscriptionId => ConfigurationManager.AppSettings["luisSubscriptionID"];

        /// <summary>
        /// Gets a value indicating whether or not to use the microphone.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use microphone]; otherwise, <c>false</c>.
        /// </value>
        private bool UseMicrophone => IsMicrophoneClientWithIntent ||
                                      IsMicrophoneClientDictation ||
                                      IsMicrophoneClientShortPhrase;

        /// <summary>
        /// Gets a value indicating whether LUIS results are desired.
        /// </summary>
        /// <value>
        ///   <c>true</c> if LUIS results are to be returned otherwise, <c>false</c>.
        /// </value>
        private bool WantIntent => !string.IsNullOrEmpty(LuisAppId) &&
                                   !string.IsNullOrEmpty(LuisSubscriptionId) &&
                                   (IsMicrophoneClientWithIntent || IsDataClientWithIntent);

        /// <summary>
        /// Gets the current speech recognition mode.
        /// </summary>
        /// <value>
        /// The speech recognition mode.
        /// </value>
        private SpeechRecognitionMode Mode
        {
            get
            {
                if (IsMicrophoneClientDictation ||
                    IsDataClientDictation)
                {
                    return SpeechRecognitionMode.LongDictation;
                }

                return SpeechRecognitionMode.ShortPhrase;
            }
        }

        /// <summary>
        /// Gets the default locale.
        /// </summary>
        /// <value>
        /// The default locale.
        /// </value>
        private static string DefaultLocale => "en-US";

        /// <summary>
        /// Gets the short wave file path.
        /// </summary>
        /// <value>
        /// The short wave file.
        /// </value>
        
        public string ShortWaveFile { get; set; }
        /// <summary>
        /// Gets the long wave file path.
        /// </summary>
        /// <value>
        /// The long wave file.
        /// </value>
        private string LongWaveFile { get; set; }

        /// <summary>
        /// Gets the Cognitive Service Authentication Uri.
        /// </summary>
        /// <value>
        /// The Cognitive Service Authentication Uri.  Empty if the global default is to be used.
        /// </value>
        private static string AuthenticationUri => ConfigurationManager.AppSettings["AuthenticationUri"];

        /// <summary>
        /// Raises the System.Windows.Window.Closed event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            _dataClient?.Dispose();
            _micClient?.Dispose();

            base.OnClosed(e);
        }

        /// <summary>
        /// Saves the subscription key to isolated storage.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        private static void SaveSubscriptionKeyToIsolatedStorage(string subscriptionKey)
        {
            using (var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                using (var oStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName, FileMode.Create, isoStore))
                {
                    using (var writer = new StreamWriter(oStream))
                    {
                        writer.WriteLine(subscriptionKey);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a fresh audio session.
        /// </summary>
        private void Initialize()
        {
            IsMicrophoneClientShortPhrase = true;
            IsMicrophoneClientWithIntent = false;
            IsMicrophoneClientDictation = false;
            IsDataClientShortPhrase = false;
            IsDataClientWithIntent = false;
            IsDataClientDictation = false;

            // Set the default choice for the group of checkbox.
            _micRadioButton.IsChecked = true;

            _fileLabel.Visibility = Visibility.Hidden;
            _fileNameLabel.Visibility = Visibility.Hidden;
            _uploadButton.IsEnabled = false;

            SubscriptionKey = GetSubscriptionKeyFromIsolatedStorage();
        }

        /// <summary>
        /// Creates a new microphone reco client without LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClient()
        {
            _micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            _micClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            _micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            //micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;

            switch (Mode)
            {
                case SpeechRecognitionMode.ShortPhrase:
                    _micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
                    break;
                case SpeechRecognitionMode.LongDictation:
                    _micClient.OnResponseReceived += OnMicDictationResponseReceivedHandler;
                    break;
            }

            _micClient.OnConversationError += OnConversationErrorHandler;
        }

        /// <summary>
        /// Creates a new microphone reco client with LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClientWithIntent()
        {
            WriteLine("--- Start microphone dictation with Intent detection ----");

            _micClient =
                SpeechRecognitionServiceFactory.CreateMicrophoneClientWithIntent(
                DefaultLocale,
                SubscriptionKey,
                LuisAppId,
                LuisSubscriptionId);
            _micClient.AuthenticationUri = AuthenticationUri;
            _micClient.OnIntent += OnIntentHandler;

            // Event handlers for speech recognition results
            _micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            //micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            _micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            _micClient.OnConversationError += OnConversationErrorHandler;
        }

        /// <summary>
        /// Handles the Click event of the HelpButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.projectoxford.ai/doc/general/subscription-key-mgmt");
        }

        /// <summary>
        /// Creates a data client without LUIS intent support.
        /// Speech recognition with data (for example from a file or audio source).  
        /// The data is broken up into buffers and each buffer is sent to the Speech Recognition Service.
        /// No modification is done to the buffers, so the user can apply their
        /// own Silence Detection if desired.
        /// </summary>
        private void CreateDataRecoClient()
        {
            _dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            _dataClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            if (Mode == SpeechRecognitionMode.ShortPhrase)
            {
                _dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;
            }
            else
            {
                _dataClient.OnResponseReceived += OnDataDictationResponseReceivedHandler;
            }

            //dataClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            _dataClient.OnConversationError += OnConversationErrorHandler;
        }

        /// <summary>
        /// Creates a data client with LUIS intent support.
        /// Speech recognition with data (for example from a file or audio source).  
        /// The data is broken up into buffers and each buffer is sent to the Speech Recognition Service.
        /// No modification is done to the buffers, so the user can apply their
        /// own Silence Detection if desired.
        /// </summary>
        private void CreateDataRecoClientWithIntent()
        {
            _dataClient = SpeechRecognitionServiceFactory.CreateDataClientWithIntent(
                DefaultLocale,
                SubscriptionKey,
                LuisAppId,
                LuisSubscriptionId);
            _dataClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            _dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;
            //dataClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            _dataClient.OnConversationError += OnConversationErrorHandler;

            // Event handler for intent result
            _dataClient.OnIntent += OnIntentHandler;
        }

        /// <summary>
        /// Sends the audio helper.
        /// </summary>
        /// <param name="wavFileName">Name of the wav file.</param>
        private void SendAudioHelper(string wavFileName)
        {
            using (var fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                var buffer = new byte[1024];

                try
                {
                    int bytesRead;
                    do
                    {
                        // Get more Audio data to send into byte buffer.
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        // Send of audio data to service. 
                        _dataClient.SendAudio(buffer, bytesRead);
                    }
                    while (bytesRead > 0);
                }
                finally
                {
                    // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                    _dataClient.EndAudio();
                }
            }
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                _micClient.EndMicAndRecognition();

                WriteResponseResult(e);

                _startButton.IsEnabled = true;
                _radioGroup.IsEnabled = true;
            });
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //WriteLine("--- OnDataShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                WriteResponseResult(e);

                _startButton.IsEnabled = true;
                _radioGroup.IsEnabled = true;
            });
        }

        /// <summary>
        /// Writes the response result.
        /// </summary>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                WriteLine("No phrase response is available.");
            }
            else
            {
                var phrases = new List<RecognizedPhrase>(e.PhraseResponse.Results);
                var results = from element in phrases
                             group element by element.Confidence
                             into groups
                             select groups.First();

                foreach(var result in results)
                {
                    WriteLine("Confidence: {0}", result.Confidence);
                    WriteLine("\"{0}\"\n", result.DisplayText);
                }

                WriteLine();
            }
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            //WriteLine("--- OnMicDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            { 
                Dispatcher.Invoke(() => 
                {
                    // we got the final result, so it we can end the mic reco.  No need to do this
                    // for dataReco, since we already called endAudio() on it as soon as we were done
                    // sending all the data.
                    _micClient.EndMicAndRecognition();

                    _startButton.IsEnabled = true;
                    _radioGroup.IsEnabled = true;
                });                
            }

            WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            //WriteLine("--- OnDataDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(() => 
                {
                    _startButton.IsEnabled = true;
                    _radioGroup.IsEnabled = true;

                    // we got the final result, so it we can end the mic reco.  No need to do this
                    // for dataReco, since we already called endAudio() on it as soon as we were done
                    // sending all the data.
                });
            }

            WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received and its intent is parsed
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechIntentEventArgs"/> instance containing the event data.</param>
        private void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            WriteLine("--- Intent received by OnIntentHandler() ---");
            WriteLine("{0}", e.Payload);
            WriteLine();
        }

        /// <summary>
        /// Called when an error is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechErrorEventArgs"/> instance containing the event data.</param>
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
           Dispatcher.Invoke(() =>
           {
               _startButton.IsEnabled = true;
               _radioGroup.IsEnabled = true;
           });

            WriteLine("--- Error received by OnConversationErrorHandler() ---");
            WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            WriteLine("Error text: {0}", e.SpeechErrorText);
            WriteLine();
        }

        /// <summary>
        /// Called when the microphone status has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MicrophoneEventArgs"/> instance containing the event data.</param>
        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
                WriteLine("********* Microphone status: {0} *********", e.Recording);

                if (e.Recording)
                {
                    WriteLine("Please start speaking.");
                }

                WriteLine();
            });
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        private void WriteLine()
        {
            WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);

            Dispatcher.Invoke(() =>
            {
                _logText.Text += (formattedStr + "\n");
                _logText.ScrollToEnd();
            });
        }

        /// <summary>
        /// Gets the subscription key from isolated storage.
        /// </summary>
        /// <returns>The subscription key.</returns>
        private static string GetSubscriptionKeyFromIsolatedStorage()
        {
            string subscriptionKey;

            using (var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(IsolatedStorageSubscriptionKeyFileName, FileMode.Open, isoStore))
                    {
                        using (var reader = new StreamReader(iStream))
                        {
                            subscriptionKey = reader.ReadLine();
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    subscriptionKey = null;
                }
            }

            if (string.IsNullOrEmpty(subscriptionKey))
            {
                subscriptionKey = DefaultSubscriptionKeyPromptMessage;
            }

            return subscriptionKey;
        }

        /// <summary>
        /// Handles the Click event of the subscription key save button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSubscriptionKeyToIsolatedStorage(SubscriptionKey);
                MessageBox.Show("Subscription key is saved in your disk.\nYou do not need to paste the key next time.", "Subscription Key");
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Fail to save subscription key. Error message: " + exception.Message,
                    "Subscription Key", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the DeleteKey control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DeleteKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SubscriptionKey = DefaultSubscriptionKeyPromptMessage;
                SaveSubscriptionKeyToIsolatedStorage(string.Empty);
                MessageBox.Show("Subscription key is deleted from your disk.", "Subscription Key");
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Fail to delete subscription key. Error message: " + exception.Message,
                    "Subscription Key", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Helper function for INotifyPropertyChanged interface 
        /// </summary>
        /// <param name="caller">Property name</param>
        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        /// <summary>
        /// Handles the Click event of the RadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset everything
            if (_micClient != null)
            {
                _micClient.EndMicAndRecognition();
                _micClient.Dispose();
                _micClient = null;
            }

            if (_dataClient != null)
            {
                _dataClient.Dispose();
                _dataClient = null;
            }

            _logText.Text = string.Empty;
            _startButton.IsEnabled = true;
            _radioGroup.IsEnabled = true;
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            //configure save file dialog box
            var dlg = new OpenFileDialog()
            {
                FileName = "Audio", //default file name
                DefaultExt = ".wav", //default file extension
                Filter = "Audio files (.wav)|*.wav" //filter files by extension
            };

            // Show save file dialog box
            var result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result != true) return;

            var fileName = dlg.FileName;

            _fileLabel.Visibility = Visibility.Visible;
            _fileNameLabel.Visibility = Visibility.Visible;

            _fileNameLabel.Content = Path.GetFileName(fileName);

            ShortWaveFile = fileName;
            LongWaveFile = fileName;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShortWaveFile == null || LongWaveFile == null)
            {
                MessageBox.Show("Please select an audio file (.wav).");
                return;
            }

            _startButton.IsEnabled = false;
            _radioGroup.IsEnabled = false;

            //LogRecognitionStart();

            if (UseMicrophone)
            {
                if (_micClient == null)
                {
                    if (WantIntent)
                    {
                        CreateMicrophoneRecoClientWithIntent();
                    }
                    else
                    {
                        CreateMicrophoneRecoClient();
                    }
                }

                _micClient?.StartMicAndRecognition();
            }
            else
            {
                if (null == _dataClient)
                {
                    if (WantIntent)
                    {
                        CreateDataRecoClientWithIntent();
                    }
                    else
                    {
                        CreateDataRecoClient();
                    }
                }

                SendAudioHelper((Mode == SpeechRecognitionMode.ShortPhrase) ? ShortWaveFile : LongWaveFile);
            }
        }

        private void _dataShortRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = true;
        }

        private void _dataLongRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = true;
        }

        private void _dataShortIntentRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = true;
        }

        private void _micIntentRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = false;
        }

        private void _micDictationRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = false;
        }

        private void _micRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _uploadButton.IsEnabled = false;
        }
    }
}