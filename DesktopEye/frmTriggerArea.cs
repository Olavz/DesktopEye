using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopEye
{
    public partial class frmTriggerArea : Form
    {

        private int screenWidth;
        private int screenHeight;

        public frmTriggerArea()
        {
            InitializeComponent();
        }

        private void frmTriggerArea_Load(object sender, EventArgs e)
        {
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;

            this.Height = screenHeight;
            this.Width = screenWidth;
            this.Top = 0;
            this.Left = 0;
        }
    }
}
