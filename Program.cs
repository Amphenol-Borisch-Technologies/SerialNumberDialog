using System;
using System.Windows.Forms;

namespace SerialNumber {
    internal static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            String serialNumber;
            while (true) {
                try {
                     ABT_SerialNumberDialog.Only.Set("01BB2-12345");
                    serialNumber = ABT_SerialNumberDialog.Only.ShowDialog().Equals(DialogResult.OK) ? ABT_SerialNumberDialog.Only.Get() : String.Empty;
                    ABT_SerialNumberDialog.Only.Hide();
                    _ = MessageBox.Show($"Serial # is '{serialNumber}'.", "Serial #", MessageBoxButtons.OK);
                } catch (Exception e) {
                    _ = MessageBox.Show(e.InnerException.Message, "Oops!", MessageBoxButtons.OK);
                    Environment.Exit(1);
                }
            }
        }
    }
}
