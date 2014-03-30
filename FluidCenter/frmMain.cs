using System;
using System.Windows.Forms;
using NetFluidService;

namespace FluidCenter
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Program.InstallService();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Program.UninstallService();
        }
    }
}
