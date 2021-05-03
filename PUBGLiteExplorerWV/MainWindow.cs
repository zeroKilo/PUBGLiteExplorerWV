using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace PUBGLiteExplorerWV
{
    public partial class MainWindow : Form
    {
        public List<PAKFile> files = new List<PAKFile>();
        public MainWindow()
        {
            InitializeComponent();
            toolStripComboBox1.SelectedIndex = 1;
        }

        private void loadSinglePAKFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.pak|*.pak";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                files = new List<PAKFile>();
                files.Add(new PAKFile(d.FileName));
                RefreshFiles();
            }
        }

        private void loadFolderOfPAKFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] paths = Directory.GetFiles(fbd.SelectedPath, "*.pak", SearchOption.TopDirectoryOnly);
                files = new List<PAKFile>();
                pb1.Value = 0;
                pb1.Maximum = paths.Length;
                foreach (string path in paths)
                {
                    status.Text = "Loading " + path + " ...";
                    pb1.Value++;
                    Application.DoEvents();
                    PAKFile file = new PAKFile(path);
                    if (file.isValid())
                        files.Add(file);
                }
                pb1.Value = 0;
                RefreshFiles();
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = toolStripComboBox1.SelectedIndex;
            listBox1.Visible = n == 0;
            treeView1.Visible = n == 1;
            RefreshFiles();
        }

        private void RefreshFiles()
        {
            status.Text = "Refreshing files...";
            Application.DoEvents();
            int n = toolStripComboBox1.SelectedIndex;
            if (n == 0)
            {
                listBox1.Items.Clear();
                foreach (PAKFile file in files)
                    if (file.isValid())
                        foreach (PAKFileEntry entry in file.table.entries)
                            listBox1.Items.Add(entry.path);
            }
            if (n == 1)
            {
                TreeNode root = new TreeNode("ROOT");
                foreach (PAKFile file in files)
                    if (file.isValid())
                        foreach (PAKFileEntry entry in file.table.entries)
                            AddPathToNode(root, entry.path);
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(root);
            }
            status.Text = "";
        }

        private void AddPathToNode(TreeNode node, string path)
        {
            string[] parts = path.Split('/');
            string part = parts[0];
            if (parts.Length > 1)
            {
                TreeNode subNode = null;
                foreach (TreeNode n in node.Nodes)
                    if (n.Text == part)
                    {
                        subNode = n;
                        break;
                    }
                if (subNode == null)
                {
                    subNode = new TreeNode(part);
                    node.Nodes.Add(subNode);
                }
                AddPathToNode(subNode, path.Substring(part.Length + 1));
            }
            else
            {
                TreeNode subNode = null;
                foreach (TreeNode n in node.Nodes)
                    if (n.Text == part)
                    {
                        subNode = n;
                        break;
                    }
                if (subNode == null)
                {
                    subNode = new TreeNode(part);
                    node.Nodes.Add(subNode);
                }
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode sel = treeView1.SelectedNode;
            if (sel == null || sel.Nodes.Count != 0 || sel.Text == "ROOT")
                return;
            string path = sel.Text;
            TreeNode parent = sel.Parent;
            while (parent != null && parent.Text != "ROOT")
            {
                path = parent.Text + "/" + path;
                parent = parent.Parent;
            }
            foreach (PAKFile file in files)
                if (file.isValid())
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == path)
                        {
                            byte[] data = file.getEntryData(entry);
                            hb1.ByteProvider = new DynamicByteProvider(data);
                        }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1) return;
            string path = listBox1.Items[n].ToString();
            foreach (PAKFile file in files)
                if (file.isValid())
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == path)
                        {
                            byte[] data = file.getEntryData(entry);
                            hb1.ByteProvider = new DynamicByteProvider(data);
                        }

        }

        private string getSelectedPath()
        {
            int n = toolStripComboBox1.SelectedIndex;
            if (n == 0)
            {
                int m = listBox1.SelectedIndex;
                if (m == -1) return "";
                return listBox1.Items[m].ToString();
            }
            if (n == 1)
            {
                TreeNode sel = treeView1.SelectedNode;
                if (sel == null || sel.Nodes.Count != 0 || sel.Text == "ROOT")
                    return "";
                string path = sel.Text;
                TreeNode parent = sel.Parent;
                while (parent != null && parent.Text != "ROOT")
                {
                    path = parent.Text + "/" + path;
                    parent = parent.Parent;
                }
                return path;
            }
            return "";
        }

        private void exportSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string exportName = getSelectedPath();
            if (exportName == "")
                return;
            string fileName = Path.GetFileName(exportName.Replace("/", "\\"));
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.*|*.*";
            d.FileName = fileName;
            foreach (PAKFile file in files)
                if (file.isValid())
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == exportName)
                            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                File.WriteAllBytes(d.FileName, file.getEntryData(entry));
                                MessageBox.Show("Done.");
                                return;
                            }
        }

        private void exportAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportDialog ex = new ExportDialog();
                ex.Show();
                ex.pb1.Value = 0;
                ex.pb1.Maximum = files.Count + 1;
                for (int i = 0; i < files.Count; i++)
                {
                    PAKFile file = files[i];
                    ex.pb1.Value = i + 1;
                    ex.label1.Text = "PAK file " + (i + 1) + "/" + files.Count + " : " + Path.GetFileName(file.myPath);
                    Application.DoEvents();
                    if (file.isValid())
                    {
                        int count = file.table.entries.Count;
                        ex.pb2.Value = 0;                        
                        ex.pb2.Maximum = count + 1;
                        for (int j = 0; j < count; j++)
                        {
                            PAKFileEntry entry = file.table.entries[j];
                            ex.pb2.Value = j + 1;
                            ex.label2.Text = "Current file " + (j + 1) + "/" + count + " : " + entry.path;
                            if ((j % 77) == 0)
                                Application.DoEvents();
                            string output = fbd.SelectedPath + "\\" + entry.path.Replace("/", "\\");
                            Directory.CreateDirectory(Path.GetDirectoryName(output));
                            File.WriteAllBytes(output, file.getEntryData(entry));
                        }
                    }
                }
                ex.Close();
                MessageBox.Show("Done.");
            }
        }
    }
}
