using Lego.Ev3.Core;
using Lego.Ev3.UWP;
using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp__UWP_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Brick _brick;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void TryToConnect(object sender, RoutedEventArgs e)
        {
            string message = string.Empty;
            var conType = CreateConnection();

            if (conType != null)
            {
                _brick = new Brick(conType, true);
                _brick.BrickChanged += _brick_BrickChanged;
                try
                {
                    await _brick.ConnectAsync();
                    message = "Connected!";
                }
                catch (Exception ex)
                {
                    message = $"Unable to connect!\r\n{ex.Message}";
                }
            }
            else
            {
                //   MessageBox.Show("Invalid connection type for this device", "Error", MessageBoxButton.OK);
            }
            MessageDialog md = new MessageDialog("Connected!");
            md.ShowAsync();
        }

        void _brick_BrickChanged(object sender, BrickChangedEventArgs e)
        {

        }

        private ICommunication CreateConnection()
        {
            ICommunication returnType = null;

            //switch (ConnControl.GetConnectionType())
            //{
            //    case ConnectionType.Bluetooth:
            returnType = new BluetoothCommunication("EV3_PINI");
            //        break;
            //    case ConnectionType.Usb:
            //        returnType = new UsbCommunication();
            //        break;
            //    case ConnectionType.WiFi:
            //        returnType = new NetworkCommunication(ConnControl.GetIpAddress());
            //        break;
            //}

            return returnType;
        }

        private async void PlayToneClick(object sender, EventArgs e)
        {
            await _brick.DirectCommand.PlayToneAsync(2, 1000, 400);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PlayToneClick(null, null);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await TestMotor(OutputPort.A);
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await TestMotor(OutputPort.B);
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await TestMotor(OutputPort.C);
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            await TestMotor(OutputPort.D);
        }

        private async Task TestMotor(OutputPort port)
        {
            await _brick.DirectCommand.TurnMotorAtPowerAsync(port, 50);
            await StopAfter(port, TimeSpan.FromSeconds(3));
        }

        private async Task StopAfter(OutputPort port, TimeSpan delay)
        {
            await Task.Delay(delay);
            await _brick.DirectCommand.StopMotorAsync(port, true);
        }
    }
}
