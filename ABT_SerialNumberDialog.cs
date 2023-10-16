using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using Windows.Security.Cryptography;

namespace SerialNumber {
    public sealed partial class ABT_SerialNumberDialog : Form {
        // NOTE: Honeywell Voyager 1200G Scanner must be programmed into USB HID mode to work correctly with TestExecutive to read ABT Serial #s.
        //       - Scan PAP131 label from "Honeywell Voyager 1200G User's Guide ReadMe.pdf" to program 1200 into USB HID mode.
        //       - Both "ReadMe" & "User's Guides" documents reside in this folder for convenience.
        // NOTE: Voyager 1200G won't scan ABT Serial #s into Notepad/Wordpad/Text Editor of Choice when in USB HID mode:
        //       - It will only deliver scanned data to a USB HID application like TestExecutive's SerialNumberDialog class.
        //       - You must scan the Voyager 1200G's PAP124 barcodes to restore "normal" keyboard wedge mode.
        // NOTE: Honeywell Voyager 1200G USB Barcode Scanner is a Microsoft supported Point of Service peripheral.
        //  - https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/pos-device-support
        // NOTE: The 1200G must also be programmed to read the Barcode Symbology of ABT's Serial #s, which at the time of this writing is Code39.
        public static ABT_SerialNumberDialog Only { get; } = new ABT_SerialNumberDialog();

        private BarcodeScanner _scanner = null;
        private ClaimedBarcodeScanner _claimedScanner = null;

        static ABT_SerialNumberDialog() { }
        // Singleton pattern requires explicit static constructor to tell C# compiler not to mark type as beforefieldinit.
        // https://csharpindepth.com/articles/singleton

        private ABT_SerialNumberDialog() {
            GetBarcodeScanner();
            InitializeComponent();
            FormUpdate(String.Empty);
        }

        public void Set(String SerialNumber) { FormUpdate(SerialNumber); }

        public String Get() { return Only.BarCodeText.Text; }

        private async void GetBarcodeScanner() {
            DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(BarcodeScanner.GetDeviceSelector(PosConnectionTypes.Local));
            foreach (DeviceInformation di in dic) { Debug.Print(di.Id); Debug.Print(di.Name); Debug.Print(di.Kind.ToString()); }
            // NOTE: If ever change Barcode Scanners from current Voyager 1200g with ID "\\?\HID#VID_0C2E&PID_0A07&MI_00#7&1f27e379&0&0000#{c243ffbd-3afc-45e9-b3d3-2ba18bc7ebc5}\posbarcodescanner"
            // Can discover new Scanner's ID by running above code in Visual Studio in Debug Configuration.
            String deviceID = @"\\?\HID#VID_0C2E&PID_0A07&MI_00#7&1f27e379&0&0000#{c243ffbd-3afc-45e9-b3d3-2ba18bc7ebc5}\posbarcodescanner";
            DeviceInformation DI = await DeviceInformation.CreateFromIdAsync(deviceID);
            _scanner = await BarcodeScanner.FromIdAsync(DI.Id);
            if (_scanner == null) throw new InvalidOperationException($"Barcode scanner Device ID:{Environment.NewLine}{Environment.NewLine}'{deviceID}'{Environment.NewLine}{Environment.NewLine}not found.");
            _claimedScanner = await _scanner.ClaimScannerAsync(); // Claim exclusively.
            if (_claimedScanner == null) throw new InvalidOperationException("Barcode scanner cannot be claimed.");
            _claimedScanner.DataReceived += ClaimedScanner_DataReceived;
            _claimedScanner.ErrorOccurred += ClaimedScanner_ErrorOccurred;
            _claimedScanner.ReleaseDeviceRequested += ClaimedScanner_ReleaseDeviceRequested;
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
    }
}
