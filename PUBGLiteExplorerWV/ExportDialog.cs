using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PUBGLiteExplorerWV
{
    public partial class ExportDialog : Form
    {
        public bool _exit = false;

        public ExportDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _exit = true;
        }

        private void ExportDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            _exit = true;
        }
    }
}
