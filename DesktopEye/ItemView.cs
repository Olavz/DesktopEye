using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DesktopEye
{
    public partial class ItemView : UserControl
    {
        [DllImport("user32.dll")]
        static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        private Process process;
        private frmMain m;

        public ItemView()
        {
            InitializeComponent();
        }


        public void setLabel(string desc) {
            label1.Text = desc;
        }

        public void setImage(Bitmap image)
        {
            pictureBox1.Image = image;
        }

        public void setProcess(Process process)
        {
            this.process = process;
        }
        public void setCallback(frmMain m)
        {
            this.m = m;
        }

        public void setAppFocus(bool focus)
        {
            if (focus) {
                // TODO: Should detect window state and not force windowstate as normal.
                // http://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
                //ShowWindow(process.MainWindowHandle, 3);
                //SetForegroundWindow(process.MainWindowHandle);
                this.BackColor = System.Drawing.SystemColors.Highlight;
                m.updateSelectedProcess(process);
            }
            else
            {
                this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            }
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void ItemView_Load(object sender, EventArgs e)
        {

        }


    }
}
