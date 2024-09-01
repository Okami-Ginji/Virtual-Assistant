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
using System.Speech;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.IO;
using System.Reflection.Emit;
using System.Globalization;
using AIMLbot;
using Google.Cloud.Translation.V2;
using System.Net;
using System.Web;
using static Google.Apis.Requests.BatchRequest;
using Microsoft.CognitiveServices.Speech;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json.Linq;
using System.Web.UI.WebControls;
using System.Net.Http;
using RestSharp;
using System.Text.RegularExpressions;
using Bogus.DataSets;
using XamlAnimatedGif;
using System.Windows.Threading;

namespace VirtualAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SpeechRecognitionEngine recEngine =  new SpeechRecognitionEngine();
        System.Speech.Synthesis.SpeechSynthesizer speech =  new System.Speech.Synthesis.SpeechSynthesizer();
        System.Media.SoundPlayer music = new System.Media.SoundPlayer();
        private DispatcherTimer _dispatcherTimer;
        private int _counter = 0;
        private const string ApiKey = "89fd93095607a248283be133f5405724"; 
        private Timer recognitionTimer;
        private bool isListening = false;

        public MainWindow()
        {
            InitializeComponent();
            Choices choices = new Choices();
            string[] text = File.ReadAllLines(Environment.CurrentDirectory + "//grammar.txt");
            choices.Add(text);
            System.Speech.Recognition.Grammar grammar = new System.Speech.Recognition.Grammar(new GrammarBuilder(choices));
            recEngine.LoadGrammar(grammar);

            // Tạo một GrammarBuilder với từ khóa "Search"
            GrammarBuilder gb = new GrammarBuilder("Search");

            // Thêm DictationGrammar để nhận diện bất kỳ chuỗi nào sau "Search"
            gb.AppendDictation();

            // Tạo một grammar và load nó vào recognizer
            System.Speech.Recognition.Grammar searchGrammar = new System.Speech.Recognition.Grammar(gb);
            recEngine.LoadGrammar(searchGrammar);

            //DictationGrammar dictationGrammar = new DictationGrammar();
            //recEngine.LoadGrammar(dictationGrammar);

            recEngine.SetInputToDefaultAudioDevice();
            recEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recEngne_SpeechRecognized);
            CultureInfo japaneseCulture = new CultureInfo("ja-JP");
            speech.SelectVoiceByHints(VoiceGender.Female, VoiceAge.NotSet,0, japaneseCulture);

            // Đăng ký sự kiện SpeakStarted và SpeakCompleted
            speech.SpeakStarted += new EventHandler<SpeakStartedEventArgs>(speech_SpeakStarted);
            speech.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(speech_SpeakCompleted);          

            //recEngine.RecognizeAsync(RecognizeMode.Multiple);
            Welcome();

        }

        private void StartListeningButton_Click(object sender, RoutedEventArgs e)
        {
            if (isListening)
            {
                MessageBox.Show("Already listening.");
                return;
            }

            // Khởi động nhận diện
            recEngine.RecognizeAsync(RecognizeMode.Multiple);
            isListening = true;

            // Khởi tạo và bắt đầu bộ đếm thời gian để tự động dừng nhận diện sau 5 giây
            recognitionTimer = new Timer(3000); // 3 giây
            recognitionTimer.Elapsed += RecognitionTimer_Elapsed;
            recognitionTimer.AutoReset = false;
            recognitionTimer.Start();

            // Đổi biểu tượng thành biểu tượng khác, ví dụ: Kind="MicrophoneOff"
            MicrophoneIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.DotsHorizontal;

            // Vô hiệu hóa nút bằng cách thay đổi thuộc tính IsEnabled
            StartListeningButton.IsEnabled = false;

            // Nếu muốn thay đổi màu viền khi nút bị vô hiệu hóa
            StartListeningButton.Background = Brushes.LightGray;
        }

        
        private void RecognitionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Dừng nhận diện sau 5 giây
            Dispatcher.Invoke(() => // Sử dụng Dispatcher để đảm bảo thay đổi giao diện người dùng trên luồng giao diện chính
            {
                recEngine.RecognizeAsyncStop();
                isListening = false;
                // Đổi biểu tượng thành biểu tượng khác, ví dụ: Kind="MicrophoneOff"
                MicrophoneIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Microphone;

                // Vô hiệu hóa nút bằng cách thay đổi thuộc tính IsEnabled
                StartListeningButton.IsEnabled = true;

                // Nếu muốn thay đổi màu viền khi nút bị vô hiệu hóa
                StartListeningButton.Background = Brushes.Black;
                
            });
        }



        private void recEngne_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            
            string result = e.Result.Text;
            AddChatMessage(result, true);
            response_Answer(result);
        }

        private async void response_Answer(string question)
        {
            
            string result = "";
            DateTime dateTime = DateTime.Now;

            Bot Ai = new Bot();
            Ai.loadSettings();
            Ai.loadAIMLFromFiles();
            Ai.isAcceptingUserInput = false;
            User user = new User("user", Ai);
            Ai.isAcceptingUserInput = true;

            question = translate(question, "vi", "en");
            Console.WriteLine("1:"+ question);
            if (question.ToLower().Contains("hello"))
            {
                int hour = dateTime.Hour;
                if (hour >= 6 && hour < 12)
                {
                    result = "Good morning";
                }
                else if (hour >= 12 && hour < 18)
                {
                    result = "Good afternoon";
                }
                else if (hour >= 18 && hour < 24)
                {
                    result = "Good evening";
                }              
            }
            else if(question.ToLower().Contains("time"))               
            {              
                result = "It is currently " + dateTime.ToLongTimeString();             
            }
            else if (question.ToLower().Contains("day"))
            {
                result = GetDate(dateTime);
            }         
            else if (question.ToLower().Contains("open google"))                
            {
                System.Diagnostics.Process.Start("https://www.google.com/");
                result = "Opening Google";                       
            }
            else if (question.ToLower().Contains("close google"))                
            {
                System.Diagnostics.Process[] close = System.Diagnostics.Process.GetProcessesByName("chrome");
                foreach (System.Diagnostics.Process p in close)
                    p.Kill();
                result = "Closing Google Chrome";                       
            }
            else if (question.ToLower().Contains("bye"))           
            {             
                System.Windows.Application.Current.Shutdown();
            }
            else if (question.ToLower().Contains("search about"))              
            {
                int index = question.IndexOf("about ");
                if (index != -1)
                {
                    string keyword = question.Substring(index + "about ".Length).Trim();
                    // Thực hiện mở trình duyệt và tìm kiếm từ khóa
                    System.Diagnostics.Process.Start("https://www.google.com/search?q=" + Uri.EscapeDataString(keyword));
                    result = "This is a search for " + keyword + " on Google.";
                }
                else
                {
                    result = "Nothing to search";
                }               
            }
            else if (question.ToLower().Contains("sing a song"))
            {
                int number = GetRandomNumber(1, 5);
                music.SoundLocation =  number + ".wav"; 
                music.Play();
                result = "Of course";
            }
            else if (question.ToLower().Contains("information"))
            {
                int index = question.IndexOf("about ");
                if (index != -1)
                {
                    string keyword = question.Substring(index + "about ".Length).Trim();
                    
                    result = SearchWikipedia(keyword);
                }
                else
                {
                    result = "Nothing to search";
                }
            }
            else if(question.ToLower().Contains("weather"))
            {
                result = await GetWeatherInfoAsync("Hoi An,Vietnam");
                
            }
            else
            {
                Request r = new Request(question, user, Ai);
                Result resultstr = Ai.Chat(r);
                result = resultstr.Output;
                        
            }           
            AddChatMessage(translate(result,"en","vi")+" Master", false);

            speech.SpeakAsync(translate(RemoveTrailingPeriod(result), "en", "ja")+" Master");

            //string resulttrans = translate(result, "en", "ja");
            //string ssmlText = $@"
            //    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='ja-JP'>
            //        <voice name='Microsoft Haruka Desktop'>
            //            <prosody pitch='+100%' rate='-20%' volume='80'>
            //                {resulttrans}
            //            </prosody>
            //        </voice>
            //    </speak>";
            //speech.SpeakSsml(ssmlText);
        }

        private String GetDate(DateTime dateTime)
        {
            ChineseLunisolarCalendar lunarCalendar = new ChineseLunisolarCalendar();

            int lunarYear = lunarCalendar.GetYear(dateTime);
            int lunarMonth = lunarCalendar.GetMonth(dateTime);
            int lunarDay = lunarCalendar.GetDayOfMonth(dateTime);

            string message = $"Today's date is {dateTime.DayOfWeek} {dateTime.Year}/{dateTime.Month}/{dateTime.Day}\n"
               + $"Lunar Date: {lunarYear}/{lunarMonth}/{lunarDay}\n";

            if (lunarCalendar.GetDayOfMonth(dateTime.AddDays(2)) == 1 || lunarCalendar.GetDayOfMonth(dateTime.AddDays(2)) == 15)
            {
                message += "Remember to send the message to your friend.";
            }
            return message;
            
        }

        private async void Welcome()
        {
            await WelcomeAsync();
        }

        private async Task WelcomeAsync()
        {
            // Chờ cho việc tải dữ liệu hoàn tất
            await LoadDataAsync();

            // Sau khi hoàn tất tải, phát lời chào
            response_Answer("chào");
        }

        private async Task LoadDataAsync()
        {           
            await Task.Delay(1000); 
        }

        // Thêm tin nhắn vào khung chat
        private void AddChatMessage(string message, bool isUserMessage)
        {
            // Tạo Border cho tin nhắn
            Border border = new Border
            {
                Background = isUserMessage ? Brushes.Black : Brushes.DarkGreen,
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(isUserMessage ? 5 : 10, 5, isUserMessage ? 10 : 5, 5),
                Padding = new Thickness(10),
                MaxWidth = 200 // Giới hạn chiều rộng tối đa của tin nhắn hệ thống
            };

            // Tạo TextBlock để hiển thị tin nhắn
            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = isUserMessage ? Brushes.Green : Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            // Đặt căn lề cho Border
            border.HorizontalAlignment = isUserMessage ? HorizontalAlignment.Left : HorizontalAlignment.Right;

            // Thêm TextBlock vào Border
            border.Child = textBlock;

            // Thêm Border vào StackPanel trong ChatBox
            ChatBox.Children.Add(border);

            // Cuộn tới nội dung mới nhất
            ChatScrollViewer.ScrollToEnd();

        }

        private string SearchWikipedia(string searchTerm)
        {
            try
            {
                // Tạo URL truy vấn với từ khóa tìm kiếm
                string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&titles={Uri.EscapeDataString(searchTerm)}&format=json";

                using (WebClient client = new WebClient())
                using (Stream stream = client.OpenRead(url))
                using (StreamReader reader = new StreamReader(stream))
                {
                    // Đọc dữ liệu JSON
                    string jsonResponse = reader.ReadToEnd();

                    // Phân tích dữ liệu JSON
                    JObject json = JObject.Parse(jsonResponse);

                    // Lấy dữ liệu từ đối tượng JSON
                    var pages = json["query"]["pages"] as JObject;

                    if (pages != null)
                    {
                        foreach (var page in pages.Properties())
                        {
                            var pageObject = page.Value as JObject;
                            if (pageObject != null)
                            {
                                string extract = pageObject["extract"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(extract))
                                {
                                    // Tìm vị trí của mục tiếp theo bằng cách tìm dấu hiệu ==
                                    int endIndex = extract.IndexOf("==");
                                    if (endIndex != -1)
                                    {
                                        // Trả về nội dung từ đầu cho đến trước mục tiếp theo
                                        return extract.Substring(0, endIndex).Trim();
                                    }
                                    else
                                    {
                                        // Nếu không tìm thấy ==, trả về toàn bộ nội dung
                                        return extract;
                                    }
                                }
                            }
                        }
                    }

                    return "No information found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> GetWeatherInfoAsync(string location)
        {
            var client = new RestClient($"https://api.openweathermap.org/data/2.5/weather?q={WebUtility.UrlEncode(location)}&appid={ApiKey}&units=metric");
            var request = new RestRequest();

            try
            {
                var response = await client.ExecuteAsync(request);            

                if (response.IsSuccessful)
                {
                    // Phân tích phản hồi JSON
                    dynamic weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);

                    string weather = weatherData["weather"]?[0]?["description"]?.ToString() ?? "N/A";
                    string temperature = weatherData["main"]?["temp"]?.ToString() ?? "N/A";
                    string humidity = weatherData["main"]?["humidity"]?.ToString() ?? "N/A";
                    string windSpeed = weatherData["wind"]?["speed"]?.ToString() ?? "N/A";
                    string pressure = weatherData["main"]?["pressure"]?.ToString() ?? "N/A";

                    return $"Weather: {weather}\n" +
                           $"Temperature: {temperature}°C\n" +
                           $"Humidity: {humidity}%\n" +
                           $"Wind Speed: {windSpeed} m/s\n" +
                           $"Pressure: {pressure}hPa\n";
                }
                else
                {
                    return $"Failed to get weather data. Status Code: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        private void speech_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            // Đổi nguồn của GIF khi bắt đầu đọc
            ChangeGifSource("/talk.gif");
        }

        private void speech_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // Đổi nguồn của GIF khi hoàn tất việc đọc
            ChangeGifSource("/normal.gif");
            DispatcherTimer();

        }
        private void ChangeGifSource(string source)
        {
            var gifSource = new Uri(source, UriKind.Relative);
            AnimationBehavior.SetSourceUri(image, gifSource);
        }

        private void DispatcherTimer()
        {
            if (_dispatcherTimer == null)
            {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                _dispatcherTimer.Tick += DispatcherTimer_Tick;
            }

            // Reset the counter and start the timer
            _counter = 0;
            _dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            _counter++;

            if(_counter == 50)
            {
                StopTimer();
                ChangeGifSource("/rest.gif");
            }
        }

        private void StopTimer()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Stop();
            }
            
        }

        private int GetRandomNumber(int minValue, int maxValue)
        {
            // Tạo đối tượng Random
            Random random = new Random();

            // Sinh số ngẫu nhiên trong khoảng từ minValue đến maxValue
            return random.Next(minValue, maxValue + 1);
        }

        public string RemoveTrailingPeriod(string input)
        {
            // Kiểm tra nếu chuỗi không rỗng và kết thúc bằng dấu chấm
            if (!string.IsNullOrEmpty(input) && input.EndsWith("."))
            {
                // Loại bỏ dấu chấm cuối cùng
                return input.Substring(0, input.Length - 1);
            }
            return input;
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            string question = InputBox.Text;  
            if(!string.IsNullOrWhiteSpace(question))
            {
                AddChatMessage(question, true);
                response_Answer(question);
                InputBox.Clear();
            }
           
        }

        private void StopSpeakingButton_Click(object sender, RoutedEventArgs e)
        {
            music.Stop();
            speech.SpeakAsyncCancelAll();
        }



        private String translate(String input, string from, string to)
        {
            var fromLanguage = from;
            var toLanguage = to;
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";
            var webclient = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            };
            var result = webclient.DownloadString(url);
            try
            {
                // Phân tích cú pháp JSON
                JArray jsonArray = JArray.Parse(result);

                // Ghép nối toàn bộ các đoạn dịch lại với nhau
                string translatedText = string.Join("", jsonArray[0].Select(x => x[0].ToString()));

                return translatedText;
            }
            catch (Exception e1)
            {
                Console.WriteLine("Error: " + e1.Message);
                return "error";
            }
        }

        private void ChangeImageSource(string newImagePath)
        {
            // Tạo một đối tượng BitmapImage mới
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(newImagePath, UriKind.RelativeOrAbsolute);
            bitmap.EndInit();

            // Cập nhật thuộc tính Source của Image
            image.Source = bitmap;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Di chuyển cửa sổ khi nhấn chuột trái và kéo
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string question = InputBox.Text;
                if (!string.IsNullOrWhiteSpace(question))
                {
                    AddChatMessage(question, true);
                    response_Answer(question);
                    InputBox.Clear();
                }
            }
        }

       
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

    }
}
