using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static RoomControllerC.MainPage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

// The interesting bits are ConnectionTestButton_Click, ConnectionTest, ConnectToBtDevice and Disconnect
// Neither SendData nor ReceiveData are called until a connection has been made
// The code including the watcher etc is not really relevant, although it does work

namespace RoomControllerC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public ObservableCollection<DeviceInfoDisplay> DevicesCollection
        {
            get;
            private set;
        }

        private MainPage rootPage = Current;

        private DeviceWatcher deviceWatcher = null;
        private StreamSocket socket = null;
        private DataWriter writer = null;
        private RfcommDeviceService service = null;
        private BluetoothDevice bluetoothDevice = null;
        private CancellationTokenSource connectionCts = null;

        public Settings()
        {
            InitializeComponent();
            App.Current.Suspending += App_Suspending;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = Current;
            DevicesCollection = new ObservableCollection<DeviceInfoDisplay>();
            DataContext = this;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopWatcher();
            Disconnect();
        }

        void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            Disconnect("App suspension disconnect");
        }

        private void ListDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher == null)
            {
                StartWatcher();
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (BluetoothDevicesView.SelectedItem == null)
            {
                ConnectionTest();
                return;
            }

            DeviceInfoDisplay deviceInfoDisplay = BluetoothDevicesView.SelectedItem as DeviceInfoDisplay;
            await ConnectToBtDevice(deviceInfoDisplay.Id);
        }

        private void StartWatcher()
        {
            string[] requestedProperties = new string[] { "System.Devices.AepService.AepId" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, serviceInfo) =>
            {
                try
                {
                    //var deviceInfo = await DeviceInformation.CreateFromIdAsync((string)serviceInfo.Properties["System.Devices.AepService.AepId"]);
                    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (serviceInfo.Name != "")
                        {
                            DevicesCollection.Add(new DeviceInfoDisplay(serviceInfo));
                            rootPage.NotifyUser(string.Format("{0} devices found. Enumeration completed. Watching for updates...", DevicesCollection.Count),
                                                NotifyType.StatusMessage);
                        }
                    });
                }
                catch (Exception ex)
                {
                    rootPage.NotifyUser("Exception: " + ex.Message, NotifyType.ErrorMessage);
                }
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (DeviceInfoDisplay infoDisplay in DevicesCollection)
                    {
                        if (infoDisplay.Id == deviceInfoUpdate.Id)
                        {
                            infoDisplay.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                });
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    rootPage.NotifyUser(string.Format("{0} devices found. Enumeration completed. Watching for updates...", DevicesCollection.Count),
                                        NotifyType.StatusMessage);
                });
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoRemoved) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (DeviceInfoDisplay infoDisplay in DevicesCollection)
                    {
                        if (infoDisplay.Id == deviceInfoRemoved.Id)
                        {
                            DevicesCollection.Remove(infoDisplay);
                        }
                    }
                });
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, object>(async (watcher, obj) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    DevicesCollection.Clear();
                });
            });

            deviceWatcher.Start();
        }

        private void StopWatcher()
        {
            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                    deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }

        // Below is the code in question

        private void ConnectionTestButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionTest();
        }

        private async void ConnectionTest()
        {
            rootPage.NotifyUser("Connection test in progress...", NotifyType.StatusMessage);

            Disconnect();

            // Test the connection for each of the devices listed in Main.xaml.cs -> Constants.BtDevices
            // We want to actually wait for the connection to be made before disconnecting ( hence the await Task.Run()) )
            foreach (KeyValuePair<string, string> device in Constants.BtDevices)
            {
                await Task.Run(() => ConnectToBtDevice(device.Value, true));
                await Task.Delay(2000);
                Disconnect("Connection test");
                await Task.Delay(1000);
            }
        }

        private async Task ConnectToBtDevice(string btId, bool connectionTest = false)
        {
            // Get information about the bluetooth device with the bluetooth address passed as a parameter
            // If the device returns null, notify the user and cancel
            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(btId);
                rootPage.NotifyUser(bluetoothDevice.Name + " found, trying to connect...", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                rootPage.NotifyUser("An exception was thrown when trying to receive the bluetooth device " + ex.Message, NotifyType.ErrorMessage);
                return;
            }

            if (bluetoothDevice == null)
            {
                rootPage.NotifyUser("Bluetooth device returned null", NotifyType.ErrorMessage);
                return;
            }

            // Get the rfcomm services available on the device and cancel if no service is found
            var rfcommServices = await bluetoothDevice.GetRfcommServicesAsync();
            if (rfcommServices.Services.Count > 0)
            {
                service = rfcommServices.Services[0];
            }
            else
            {
                rootPage.NotifyUser("No RFCOMM services found on the device", NotifyType.ErrorMessage);
                return;
            }

            lock (this)
            {
                socket = new StreamSocket();
            }
            try
            {
                // Create the cancellation token and set the expiry time to equal what is set in Main.xaml.cs -> Constants.ConnectionTimeout
                connectionCts = new CancellationTokenSource();
                connectionCts.CancelAfter(Constants.ConnectionTimeout);

                // Try connecting to the bluetooth device ------- This is where it stops
                await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName).AsTask(connectionCts.Token);

                // This line is never reached when the ConnectAsync apparently just freezes
                // It is however, reached when a connection is successful

                // Set the encoding for the reader and writer to be utf8 - Not really sure if this is needed(?)
                writer = new DataWriter(socket.OutputStream)
                {
                    UnicodeEncoding = UnicodeEncoding.Utf8
                };
                DataReader reader = new DataReader(socket.InputStream)
                {
                    UnicodeEncoding = UnicodeEncoding.Utf8
                };

                rootPage.NotifyUser("Connected to " + bluetoothDevice.Name, NotifyType.SuccessMessage);

                // Start the data reader loop
                ReceiveData(reader, connectionTest);

                // If the connection is just for testing purposes, write some data
                if (connectionTest)
                {
                    await SendData("Test");
                }
            }
            // The exception that should be thrown when the cancellation token expires - Cancels the operation and notifies the user
            catch (TaskCanceledException tEx)
            {
                rootPage.NotifyUser("Connection timed out.", NotifyType.ErrorMessage);
                Disconnect();
            }
            catch (Exception ex)
            {
                rootPage.NotifyUser("An exception was thrown when trying to connect to " + bluetoothDevice.Name + " " + ex.Message, NotifyType.ErrorMessage);
                Disconnect();
            }
        }

        private async Task SendData(string dataToSend)
        {
            try
            {
                if (dataToSend.Length != 0)
                {
                    writer.WriteString(Constants.SOP.ToString() + dataToSend + Constants.EOP.ToString());
                    
                    await writer.StoreAsync();

                }
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
            {
                // The remote device has closed the connection
                rootPage.NotifyUser("Remote device has disconnected " + ex.HResult.ToString() + " - " + ex.Message, NotifyType.ErrorMessage);
                Disconnect();
            }
        }

        private async void ReceiveData(DataReader reader, bool connectionTest = false)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 8;
            try
            {
                reader.InputStreamOptions = InputStreamOptions.Partial;
                loadAsyncTask = reader.LoadAsync(ReadBufferLength).AsTask();

                uint bytesRead = await loadAsyncTask;

                if (bytesRead > 0)
                {
                    rootPage.NotifyUser("Received: " + reader.ReadString(bytesRead), NotifyType.SuccessMessage);
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (socket == null)
                    {
                        if ((uint)ex.HResult == 0x80072745)
                            rootPage.NotifyUser("Disconnect triggered by remote device", NotifyType.StatusMessage);
                    }
                    else
                    {
                        rootPage.NotifyUser("Exception was thrown when trying to read data: " + ex.Message, NotifyType.ErrorMessage);
                        Disconnect();
                    }
                }
            }
        }

        private void Disconnect(string disconnectReason = null)
        {
            // Sets all the bluetooth specific variables to null and disposes what is disposable
            if (writer != null)
            {
                writer.DetachStream();
                writer = null;
            }

            if (service != null)
            {
                service.Dispose();
                service = null;
            }
            if (connectionCts != null)
            {
                connectionCts.Dispose();
                connectionCts = null;
            }
            if (bluetoothDevice != null) bluetoothDevice = null;
            lock (this)
            {
                if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }
            }
            if (disconnectReason != null)
            {
                rootPage.NotifyUser("Disconnected. Reason: " + disconnectReason, NotifyType.CautionMessage);
            }
        }
    }
}
