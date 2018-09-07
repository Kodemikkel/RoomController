using System.ComponentModel;
using Windows.Devices.Enumeration;

namespace RoomControllerC
{
    public class DeviceInfoDisplay : INotifyPropertyChanged
    {
        public DeviceInfoDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }
        public DeviceInformation DeviceInformation { get; set; }
        public string Id
        {
            get
            {
                return DeviceInformation.Id;
            }
        }
        public string Name
        {
            get
            {
                return DeviceInformation.Name;
            }
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
