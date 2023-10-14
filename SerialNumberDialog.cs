using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using Windows.Security.Cryptography;

namespace SerialNumber {
    public sealed partial class SerialNumberDialog : Form {
        // NOTE: If invoked at ABT, IS must first "whitelist" any Barcode Scanner utilized, as ABT's security disallows USB HID mode.
        //       - Just as USB drives must be "whitelisted" prior to use on ABT's Borisch Domain, Barcode Scanners in USB HID mode
        //         must also be whitelisted.
        //       - Most USB Barcode Scanners default to USB PC Keyboard, or keyboard wedge mode, where they emulate keyboard
        //         input, and Scanned Barcodes are treated similar to typed data.
        //       - This is why Scanners in keyboard wedge mode don't require whitelisting, as they're acting as additional keyboards.
        // NOTE: Honeywell Voyager 1200G Scanner must be programmed into USB HID mode to work correctly with TestExecutive to read ABT Serial #s.
        //       - Scan PAP131 label from "Honeywell Voyager 1200G User's Guide ReadMe.pdf" to program 1200 into USB HID mode.
        //       - Both "ReadMe" & "User's Guides" documents reside in this folder for convenience.
        // NOTE: Voyager 1200G won't scan ABT Serial #s into Notepad/Wordpad/Text Editor of Choice when in USB HID mode:
        //       - It will only deliver scanned data to a USB HID application like TestExecutive's SerialNumberDialog class.
        //       - You must scan the Voyager 1200G's PAP124 barcodes to restore "normal" keyboard wedge mode.
        // NOTE: Honeywell Voyager 1200G USB Barcode Scanner is a Microsoft supported Point of Service peripheral.
        //  - https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/pos-device-support
        // NOTE: The 1200G must also be programmed to read the Barcode Symbology of ABT's Serial #s, which at the time of this writing is Code39.
        public static SerialNumberDialog Only { get; } = new SerialNumberDialog();

        private BarcodeScanner _scanner = null;
        private ClaimedBarcodeScanner _claimedScanner = null;

        static SerialNumberDialog() { }
        // Singleton pattern requires explicit static constructor to tell C# compiler not to mark type as beforefieldinit.
        // https://csharpindepth.com/articles/singleton

        private SerialNumberDialog() {
            InitializeComponent();
            GetBarcodeScanner();
            FormUpdate(String.Empty);
        }

        public void Set(String SerialNumber) { FormUpdate(SerialNumber); }

        public String Get() { return Only.BarCodeText.Text; }

        private async void GetBarcodeScanner() {
            DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(BarcodeScanner.GetDeviceSelector());
            DeviceInformation di = dic.FirstOrDefault();
            if (di != null)  _scanner = await BarcodeScanner.FromIdAsync(di.Id);
            if (_scanner == null) throw new InvalidOperationException("Barcode scanner not found.");
            _claimedScanner = await _scanner.ClaimScannerAsync(); // Claim exclusively.
            if (_claimedScanner == null) throw new InvalidOperationException("Barcode scanner not found.");
            _claimedScanner.ReleaseDeviceRequested += ClaimedScanner_ReleaseDeviceRequested;
            _claimedScanner.DataReceived += ClaimedScanner_DataReceived;
            _claimedScanner.ErrorOccurred += ClaimedScanner_ErrorOccurred;
            _claimedScanner.IsDecodeDataEnabled = true; // Decode raw data from scanner and sends the ScanDataLabel and ScanDataType in the DataReceived event.
            await _claimedScanner.EnableAsync(); // Scanner must be enabled in order to receive the DataReceived event.
        }

        private void ClaimedScanner_ReleaseDeviceRequested(Object sender, ClaimedBarcodeScanner e) { e.RetainDevice(); } // Mine, don't touch!  Prevent other apps claiming scanner.

        private void ClaimedScanner_ErrorOccurred(ClaimedBarcodeScanner sender, BarcodeScannerErrorOccurredEventArgs args) {
            _ = MessageBox.Show("ErrorOccurred!", "ErrorOccurred!", MessageBoxButtons.OK);
        }

        private void ClaimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args) { Only.Invoke(new DataReceived(DelegateMethod), args); }

        private delegate void DataReceived(BarcodeScannerDataReceivedEventArgs args);

        private void DelegateMethod(BarcodeScannerDataReceivedEventArgs args) {
            if (args.Report.ScanDataLabel == null) return;
            Only.FormUpdate(CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, args.Report.ScanDataLabel));
        }

        private void OK_Clicked(Object sender, EventArgs e) { Only.DialogResult = DialogResult.OK; }

        private void Cancel_Clicked(Object sender, EventArgs e) { Only.DialogResult = DialogResult.Cancel; }

        private void FormUpdate(String text) {
            BarCodeText.Text = text;
            if (Regex.IsMatch(text, "^01BB2-[0-9]{5}$")) {
                OK.Enabled = true;
                OK.BackColor = System.Drawing.Color.Green;
            } else {
                OK.Enabled = false;
                OK.BackColor = System.Drawing.Color.DimGray;
            }
        }

//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Security.Policy;
//using System.Threading.Tasks;

        //private async void GetBarcodeScannerOld() {
        //    _scanner = await GetFirstBarcodeScannerAsync();
        //    if (_scanner == null) throw new InvalidOperationException("Barcode scanner not found.");
        //    _claimedScanner = await _scanner.ClaimScannerAsync(); // Claim exclusively.
        //    if (_claimedScanner == null) throw new InvalidOperationException("Barcode scanner not found.");
        //    _claimedScanner.ReleaseDeviceRequested += ClaimedScanner_ReleaseDeviceRequested;
        //    _claimedScanner.DataReceived += ClaimedScanner_DataReceived;
        //    _claimedScanner.ErrorOccurred += ClaimedScanner_ErrorOccurred;
        //    _claimedScanner.IsDecodeDataEnabled = true; // Decode raw data from scanner and sends the ScanDataLabel and ScanDataType in the DataReceived event.
        //    await _claimedScanner.EnableAsync(); // Scanner must be enabled in order to receive the DataReceived event.
        //}

        //private static async Task<BarcodeScanner> GetFirstBarcodeScannerAsync(PosConnectionTypes connectionTypes = PosConnectionTypes.Local) {
        //    return await GetFirstDeviceAsync(BarcodeScanner.GetDeviceSelector(connectionTypes), async (id) => await BarcodeScanner.FromIdAsync(id));
        //}

        //private static async Task<T> GetFirstDeviceAsync<T>(String selector, Func<String, Task<T>> convertAsync) where T : class {
        //    TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
        //    List<Task> pendingTasks = new List<Task>();
        //    DeviceWatcher watcher = DeviceInformation.CreateWatcher(selector);

        //    watcher.Added += (DeviceWatcher sender, DeviceInformation device) => {
        //        pendingTasks.Add(((Func<String, Task>)(async (id) => {
        //            T t = await convertAsync(id);
        //            if (t != null) completionSource.TrySetResult(t);
        //        }))(device.Id));
        //    };

        //    watcher.EnumerationCompleted += async (DeviceWatcher sender, Object args) => {
        //        await Task.WhenAll(pendingTasks);
        //        completionSource.TrySetResult(null);
        //    };

        //    watcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate args) => { }; // Event must be "handled" to enable realtime updates; empty block suffices.
        //    watcher.Updated += (DeviceWatcher sender, DeviceInformationUpdate args) => { }; // Ditto.
        //    watcher.Start();
        //    T result = await completionSource.Task;
        //    watcher.Stop();
        //    return result;
        //}
    }
}
