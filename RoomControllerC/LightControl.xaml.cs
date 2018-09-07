using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RoomControllerC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LightControl : Page
    {

        Dictionary<string, int> colorChannels;
        int dimValue;
        bool dimLooping;
        private bool lightOn;

        public bool LightOn
        {
            get { return lightOn; }
            set
            {
                if (value)
                {

                }
                else
                {

                }
                lightOn = value;
            }
        }

        public LightControl()
        {
            this.InitializeComponent();
            dimValue = 10;
            dimLooping = false;

            // TODO: Fetch values from Arduino
            LightOn = false;
            colorChannels = new Dictionary<string, int>();
            colorChannels["alpha"] = colorChannels["red"] = colorChannels["green"] = colorChannels["blue"] = 0;
        }

        private void LightButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null) return;

            string buttonColor = button.Tag.ToString();

            colorChannels = new Dictionary<string, int>();
            
            if (buttonColor.Substring(0, 1) == "#")
            {
                if (buttonColor.Length == 9 || buttonColor.Length == 7 || buttonColor.Length == 4) buttonColor = buttonColor.TrimStart('#');

                if (buttonColor.Length == 8)
                {
                    colorChannels["alpha"] = int.Parse(buttonColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["red"] = int.Parse(buttonColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["green"] = int.Parse(buttonColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["blue"] = int.Parse(buttonColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                }
                else if (buttonColor.Length == 6)
                {
                    colorChannels["alpha"] = 255;
                    colorChannels["red"] = int.Parse(buttonColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["green"] = int.Parse(buttonColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["blue"] = int.Parse(buttonColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                }
                else if (buttonColor.Length == 3)
                {
                    colorChannels["alpha"] = 255;
                    colorChannels["red"] = int.Parse(buttonColor.Substring(0, 1) + buttonColor.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["green"] = int.Parse(buttonColor.Substring(1, 1) + buttonColor.Substring(1, 1), System.Globalization.NumberStyles.HexNumber);
                    colorChannels["blue"] = int.Parse(buttonColor.Substring(2, 1) + buttonColor.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                }
                else return;
            }
            else
            {
                colorChannels["alpha"] = -1;
                colorChannels["red"] = -1;
                colorChannels["green"] = -1;
                colorChannels["blue"] = -1;
            }

            if (!(colorChannels.ContainsValue(-1)))
            {
                AlphaSlider.Value = colorChannels["alpha"];
                RedSlider.Value = colorChannels["red"];
                GreenSlider.Value = colorChannels["green"];
                BlueSlider.Value = colorChannels["blue"];
            }
            // TODO: Create errorhandler for invalid values
        }

        private void RedColorSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            colorChannels["red"] = (int)RedSlider.Value;
        }

        private void GreenColorSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            colorChannels["green"] = (int)GreenSlider.Value;
        }

        private void BlueColorSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            colorChannels["blue"] = (int)BlueSlider.Value;
        }

        private void AlphaColorSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            colorChannels["alpha"] = (int)AlphaSlider.Value;
            ColorValue.Text = "A: " + colorChannels["alpha"].ToString();
        }

        private void DimButton_Click(object sender, RoutedEventArgs e)
        {
            if (colorChannels == null || colorChannels.ContainsValue(-1)) return;

            Button button = (Button) sender;

            if (button.Tag.ToString().ToUpper() == "UP")
            {
                if (colorChannels["alpha"] + dimValue <= 255)
                {
                    colorChannels["alpha"] = colorChannels["alpha"] + dimValue;
                }
                else
                {
                    colorChannels["alpha"] = 255;
                }
                AlphaSlider.Value = colorChannels["alpha"];
            }
            else if (button.Tag.ToString().ToUpper() == "DOWN")
            {
                if (colorChannels["alpha"] - dimValue >= 0)
                {
                    colorChannels["alpha"] = colorChannels["alpha"] - dimValue;
                }
                else
                {
                    colorChannels["alpha"] = 0;
                }
                AlphaSlider.Value = colorChannels["alpha"];
            }
        }

        private async void DimButton_Hold(object sender, HoldingRoutedEventArgs e)
        {
           
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                dimLooping = true;
            }
            else
            {
                dimLooping = false;
            }

            if (Monitor.TryEnter(sender))
            {
                while (dimLooping)
                {
                    DimButton_Click(sender, e);
                    await Task.Delay(100);
                }
            }
        }

        private void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null) return;

            if (button.Tag.ToString().ToUpper() == "ON")
            {
                lightOn = true;
                button.Background = new SolidColorBrush(Windows.UI.Colors.Black);
                button.BorderBrush = new SolidColorBrush(Windows.UI.Colors.White);
                button.Content = "Off";
                button.Tag = "Off";
                AlphaSlider.IsEnabled = true;
            }
            else if (button.Tag.ToString().ToUpper() == "OFF")
            {
                lightOn = false;
                button.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                button.BorderBrush = null;
                button.Content = "On";
                button.Tag = "On";
                AlphaSlider.IsEnabled = false;
            }
        }
    }
}
