using Lego.Ev3.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;
using ThreadPool = Windows.System.Threading.ThreadPool;

namespace Lego.Ev3.UWP
{
	/// <summary>
	/// Communicate with EV3 brick over Bluetooth.
	/// </summary>
	public sealed class BluetoothCommunication : ICommunication
	{
		/// <summary>
		/// Event fired when a complete report is received from the EV3 brick.
		/// </summary>
		public event EventHandler<ReportReceivedEventArgs> ReportReceived;

		private StreamSocket _socket;
		private DataReader _reader;
		private CancellationTokenSource _tokenSource;

		private readonly string _deviceName = "EV3";

		/// <summary>
		/// Create a new BluetoothCommunication object
		/// </summary>
		public BluetoothCommunication()
		{
		}

		/// <summary>
		/// Create a new BluetoothCommunication object
		/// </summary>
		/// <param name="device">Devicename of the EV3 brick</param>
		public BluetoothCommunication(string device)
		{
			_deviceName = device;
		}

		/// <summary>
		/// Connect to the EV3 brick.
		/// </summary>
		/// <returns></returns>
		public Task ConnectAsync()
		{
			return ConnectAsyncInternal();
		}

        public async Task<string> ComPort(DeviceInformation deviceInfo)
        {
            var serialDevices = new Dictionary<string, SerialDevice>();
            var serialSelector = SerialDevice.GetDeviceSelector();
            var serialDeviceInformations = (await DeviceInformation.FindAllAsync(serialSelector)).ToList();
            var hostNames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().Select(hostName => hostName.DisplayName.ToUpper()).ToList(); // So we can ignore inbuilt ports
            foreach (var serialDeviceInformation in serialDeviceInformations)
            {
                if (hostNames.FirstOrDefault(hostName => hostName.StartsWith(serialDeviceInformation.Name.ToUpper())) == null)
                {
                    try
                    {
                        var serialDevice = await SerialDevice.FromIdAsync(serialDeviceInformation.Id);
                        if (serialDevice != null)
                        {
                            serialDevices.Add(deviceInfo.Id, serialDevice);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
            }
            // Example Bluetooth DeviceInfo.Id: "Bluetooth#Bluetooth9c:b6:d0:d6:d7:56-00:07:80:cb:56:6d"
            // from device with Association Endpoint Address: "00:07:80:cb:56:6d"
            var lengthOfTrailingAssociationEndpointAddresss = (2 * 6) + 5;
            var bluetoothDeviceAddress = deviceInfo.Id.Substring(deviceInfo.Id.Length - lengthOfTrailingAssociationEndpointAddresss, lengthOfTrailingAssociationEndpointAddresss).Replace(":", "").ToUpper();
            var matchingKey = serialDevices.Keys.FirstOrDefault(id => id.Contains(bluetoothDeviceAddress));
            if (matchingKey != null)
            {
                return serialDevices[matchingKey].PortName;
            }
            return "";
        }

        private async Task ConnectAsyncInternal()
		{
			_tokenSource = new CancellationTokenSource();

            var qry = "System.Devices.InterfaceClassGuid:=\"{b142fc3e-fa4e-460b-8abc-072b628b3c70}\"";

            var all = await DeviceInformation.FindAllAsync(qry);
            var device = all.FirstOrDefault(aa => aa.Name == _deviceName);


            if (device == null)
                throw new Exception("LEGO EV3 brick named '" + _deviceName + "' not found.");

            RfcommDeviceService service = await RfcommDeviceService.FromIdAsync(device.Id);
            if (service == null)
                throw new Exception("Unable to connect to LEGO EV3 brick...is the manifest set properly?");

            var a = device.Properties;

            _socket = new StreamSocket();
            await _socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName,
                 SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            _reader = new DataReader(_socket.InputStream);
            _reader.ByteOrder = ByteOrder.LittleEndian;

            await ThreadPool.RunAsync(PollInput);		
		}

		private async void PollInput(IAsyncAction operation)
		{
			while(_socket != null)
			{
				try
				{
					DataReaderLoadOperation drlo = _reader.LoadAsync(2);
					await drlo.AsTask(_tokenSource.Token);
					short size = _reader.ReadInt16();
					byte[] data = new byte[size];

					drlo = _reader.LoadAsync((uint)size);
					await drlo.AsTask(_tokenSource.Token);
					_reader.ReadBytes(data);

					if(ReportReceived != null)
						ReportReceived(this, new ReportReceivedEventArgs { Report = data });
				}
				catch (TaskCanceledException)
				{
					return;
				}
			}
		}

		/// <summary>
		/// Disconnect from the EV3 brick.
		/// </summary>
		public void Disconnect()
		{
			_tokenSource.Cancel();
			if(_reader != null)
			{
				_reader.DetachStream();
				_reader = null;
			}

			if(_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}
		}

		/// <summary>
		/// Write data to the EV3 brick.
		/// </summary>
		/// <param name="data">Byte array to write to the EV3 brick.</param>
		/// <returns></returns>
		public Task WriteAsync([ReadOnlyArray]byte[] data)
		{
			return WriteAsyncInternal(data);
		}

		private async Task WriteAsyncInternal(byte[] data)
		{
			if(_socket != null)
				await _socket.OutputStream.WriteAsync(data.AsBuffer());
		}
	}
}
