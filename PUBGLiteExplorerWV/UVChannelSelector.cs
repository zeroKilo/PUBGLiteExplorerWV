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
    public partial class UVChannelSelector : Form
    {
        public int[] channels;
        public UVChannelSelector()
        {
            InitializeComponent();
        }

        private void UVChannelSelector_Load(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 8; i++)
                listBox1.Items.Add("UV Channel " + i + " = " + channels[i]);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            channels[n] = trackBar1.Value;
            RefreshList();
            listBox1.SelectedIndex = n;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            trackBar1.Value = channels[n];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
