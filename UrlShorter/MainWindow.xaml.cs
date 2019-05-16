using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace UrlShorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BlurWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ((App)Application.Current).UpdateSkin(SkinType.Dark);
        }

        private readonly List<myList> list = new List<myList>();

        private const string BitlyApiKey = "R_c597e397b606436fa6a9179626da61bb";
        private const string BitlyApiLoginKey = "o_1i6m8a9v55";
        private const string OpizoAPIKey = "6A37B853E1105BF9FE67239731DA1EE8";

        public class myList
        {
            public string Link { get; set; }
            public string ShotLink { get; set; }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (tgTop.IsChecked == true)
                this.Topmost = true;
            else
                Topmost = false;
        }

        private void ToggleButton_Checked_1(object sender, RoutedEventArgs e)
        {
            if (tgDark.IsChecked == true)
                ((App)Application.Current).UpdateSkin(SkinType.Dark);
            else
                ((App)Application.Current).UpdateSkin(SkinType.Default);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Growl.Info("Coded by Mahdi Hosseini");

        }

        public string BitlyShorter(string LongUrl)
        {
            var url = string.Format("http://api.bitly.ly/shortern?format=json&version2.0.1&longUrl={0}&login={1}&apiKey={2}", HttpUtility.UrlEncode(LongUrl), BitlyApiLoginKey, BitlyApiKey);
            var request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                var response = request.GetResponse();
                using (var responsStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responsStream, Encoding.UTF8);
                    var js = new JavaScriptSerializer();
                    var jsonResponse = js.Deserialize<dynamic>(reader.ReadToEnd());
                    string s = jsonResponse["results"][LongUrl]["shortUrl"];
                    return s;
                }
            }
            catch (WebException ex)
            {
                var errorResponse = ex.Response;
                using (var responseStream = errorResponse.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    var errorText = reader.ReadToEnd();
                    Growl.Error(errorText);
                }
                throw;
            }
            catch (RuntimeBinderException ex)
            {
                Growl.Error(ex.Message);
                return "";
            }
        }

        private void TxtUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl.Text))
                btn.IsEnabled = false;

            Uri uriResult;
            var result = Uri.TryCreate(txtUrl.Text, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result)
                btn.IsEnabled = true;
            else
                btn.IsEnabled = false;

            stack.Visibility = Visibility.Hidden;
        }

        public class OpizoData
        {
            public string url { get; set; }
        }
        public class OpizoRootObject
        {
            public string status { get; set; }
            public OpizoData data { get; set; }
        }

        public string OpizoShorter(string LongUrl)
        {
            var link = string.Empty;

            using (var wb = new WebClient())
            {
                wb.Headers.Add("X-API-KEY", OpizoAPIKey);
                var data = new NameValueCollection();
                data["url"] = LongUrl;

                var response = wb.UploadValues("https://opizo.com/api/v1/shrink/", "POST", data);

                var responseinString = Encoding.UTF8.GetString(response);

                var root = JsonConvert.DeserializeObject<OpizoRootObject>(responseinString);

                if (root.status.Equals("success"))
                    link = root.data.url;
                else
                    Growl.Error("Error!");
            }
            return link;
        }

        public class Yon
        {
            public bool status { get; set; }
            public string output { get; set; }
        }

        public string YonShorter(string longUrl, string customURL = "")
        {
            var link = string.Empty;

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["url"] = longUrl;
                data["wish"] = customURL;

                var response = wb.UploadValues("http://api.yon.ir", "POST", data);
                var responseInString = Encoding.UTF8.GetString(response);

                var result = JsonConvert.DeserializeObject<Yon>(responseInString);

                if (result.status)
                    link = "http://yon.ir/" + result.output;
                else
                    Growl.Error("error");
            }

            return link;
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            switch (cmbService.SelectedIndex)
            {
                case 0:
                    txtUrl.Text = YonShorter(txtUrl.Text, txtCustom.Text);
                    stack.Visibility = Visibility.Visible;
                    break;

                case 1:
                    txtUrl.Text = OpizoShorter(txtUrl.Text);
                    stack.Visibility = Visibility.Visible;
                    break;

                case 2:
                    txtUrl.Text = BitlyShorter(txtUrl.Text);
                    stack.Visibility = Visibility.Visible;
                    break;

            }
            Clipboard.SetText(txtUrl.Text);
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            list.Clear();

            var dialog = new OpenFileDialog();
            dialog.Title = "Open Text File";
            dialog.Filter = "Text file|*.txt";
            if(dialog.ShowDialog() == true)
            {
                var filename = dialog.FileName;
                var filelines = File.ReadAllLines(filename);

                foreach (var item in filelines)
                {
                    list.Add(new myList { Link = item });
                }
                dataGrid.ItemsSource = list;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    list.Clear();

                    for (int i = 0; i < dataGrid.SelectedItems.Count; i++)
                    {
                        dynamic selectedItem = dataGrid.SelectedItems[i];
                        var longLink = selectedItem.Link;

                        switch (cmbList.SelectedIndex)
                        {
                            case 0:
                                list.Add(new myList { ShotLink = YonShorter(longLink) });
                                break;

                        }
                    }
                }
                catch (Exception)
                {

                }

                var Sdialog = new SaveFileDialog();
                Sdialog.Title = "Save Text File";
                Sdialog.Filter = "Txt|*.txt";
                if (Sdialog.ShowDialog() == true)
                    File.WriteAllLines(Sdialog.FileName, list.Select(x => x.ShotLink));
                dataGrid.ItemsSource = null;
            }), DispatcherPriority.Background);
        }
    }
}
