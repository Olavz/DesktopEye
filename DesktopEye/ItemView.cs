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

        private Process process;
        private frmMenu frmMenu;

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
        public void setCallback(frmMenu frmMenu)
        {
            this.frmMenu = frmMenu;
        }

        public void setAppFocus(bool focus)
        {
            if (focus) {
                this.BackColor = System.Drawing.SystemColors.Highlight;
                frmMenu.updateSelectedProcess(process);
            }
            else
            {
                this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            }
        }


    }
}
