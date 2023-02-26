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
    public partial class UVFormatSelector : Form
    {
        public UVFormatSelector()
        {
            InitializeComponent();
        }

        private void UVFormatSelector_Load(object sender, EventArgs e)
        {
            toolStripComboBox1.Items.Clear();
            foreach (UStaticMeshLOD.UVBinaryFormat format in (UStaticMeshLOD.UVBinaryFormat[])Enum.GetValues(typeof(UStaticMeshLOD.UVBinaryFormat)))
                toolStripComboBox1.Items.Add(format);
            toolStripComboBox1.SelectedIndex = 0;
            RefreshList();
        }

        public void RefreshList()
        {
            listBox1.Items.Clear();
            int count = 0;
            foreach (UStaticMeshLOD.UVBinaryFormat format in UStaticMeshLOD.readerDefaultsUV)
                listBox1.Items.Add("UV" + (count++) + " = " + format);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            toolStripComboBox1.SelectedIndex = (int)UStaticMeshLOD.readerDefaultsUV[n];
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            UStaticMeshLOD.readerDefaultsUV[n] = (UStaticMeshLOD.UVBinaryFormat)toolStripComboBox1.SelectedIndex;
            RefreshList();
        }
    }
}
