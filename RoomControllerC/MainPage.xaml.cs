using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
// Most of this is copied from the sample (https://github.com/Microsoft/Windows-universal-samples)
// Although I have focused more on the connection problem than this
// The BtDevices constant at the bottom needs to be filled out (blanked for privacy)

namespace RoomControllerC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        public List<Scenario> Scenarios { get; } = new List<Scenario>
        {
            new Scenario()
            {
                Title = "Lights",
                ClassType = typeof(LightControl)
            },
            new Scenario()
            {
                Title = "Settings",
                ClassType = typeof(Settings)
            },
        };

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
        }

        public enum NotifyType
        {
            SuccessMessage,
            StatusMessage,
            CautionMessage,
            ErrorMessage
        };

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Populate the scenario list
            ScenarioControl.ItemsSource = Scenarios;
            if (Window.Current.Bounds.Width < 640)
            {
                ScenarioControl.SelectedIndex = -1;
            }
            else
            {
                ScenarioControl.SelectedIndex = 1;
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }

        private void ScenarioControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear the status block when navigating scenarios.
            NotifyUser(String.Empty, NotifyType.StatusMessage);

            ListBox scenarioListBox = sender as ListBox;
            if (scenarioListBox.SelectedItem is Scenario s)
            {
                ScenarioFrame.Navigate(s.ClassType);
                Splitter.IsPaneOpen = false;
            }
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.SuccessMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Blue);
                    break;
                case NotifyType.CautionMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Orange);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }

    // Not really sure how this works, need to look into that (copied from https://github.com/Microsoft/Windows-universal-samples)
    public class ScenarioBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Scenario s = value as Scenario;
            return (MainPage.Current.Scenarios.IndexOf(s) + 1) + ") " + s.Title;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }

    static public class Constants
    {
        // A dictionary with the ids of all the Bluetooth receivers in use
        public static readonly Dictionary<string, string> BtDevices = new Dictionary<string, string>
        {
            { "Device1", "Btaddress1" }, // Format: Bluetooth#BluetoothbX:XX:XX:XX:XX:XX-XX:XX:XX:XX:XX:XX
            { "Device2", "Btaddress2" }
        };
        // Constants used for data transfer (WIP)
        public const char SOP = '<';
        public const char EOP = '>';
        // A constant defining how much time, in ms, to wait before aborting the connection attempt
        public const int ConnectionTimeout = 5000; // ms
    }
}
