﻿using System;
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
        public UAsset currentAsset = null;
        public MainWindow()
        {
            InitializeComponent();
            toolStripComboBox1.SelectedIndex = 1;
            tabControl1.SelectedTab = tabPage2;
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
                            LoadAsset(file, entry);
                            break;
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
                            LoadAsset(file, entry);
                            break;
                        }
        }

        private void LoadAsset(PAKFile file, PAKFileEntry entry)
        {
            currentAsset = null;
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            listBox4.Items.Clear();
            hb2.ByteProvider = new DynamicByteProvider(new byte[0]);
            byte[] data = file.getEntryData(entry);
            hb1.ByteProvider = new DynamicByteProvider(data);
            string[] assetFiles = { ".uasset", ".umap" };
            bool isAsset = false;
            foreach(string check in assetFiles)
                if (entry.path.ToLower().EndsWith(check))
                {
                    isAsset = true;
                    break;
                }
            if (isAsset)
            {
                UAsset asset = null;
                PAKFileEntry uexp = null;
                string uexpPath = Path.GetDirectoryName(entry.path) + "\\" + Path.GetFileNameWithoutExtension(entry.path) + ".uexp";
                uexpPath = uexpPath.Replace("\\", "/");
                foreach (PAKFileEntry e in file.table.entries)
                    if (e.path == uexpPath)
                    {
                        uexp = e;
                        break;
                    }
                byte[] uexpData = null;
                if (uexp != null)
                {
                    uexpData = file.getEntryData(uexp);
                    asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), null);
                }
                else
                {
                    PAKFileEntry ubulk = null;
                    string ubulkPath = Path.GetDirectoryName(entry.path) + "\\" + Path.GetFileNameWithoutExtension(entry.path) + ".ubulk";
                    ubulkPath = ubulkPath.Replace("\\", "/");
                    foreach (PAKFileEntry e in file.table.entries)
                        if (e.path == ubulkPath)
                        {
                            ubulk = e;
                            break;
                        }
                    byte[] ubulkData = null;
                    if (ubulk != null)
                    {
                        ubulkData = file.getEntryData(uexp);
                        asset = new UAsset(new MemoryStream(data), null, new MemoryStream(ubulkData));
                    }
                    else
                        asset = new UAsset(new MemoryStream(data), null, null);
                }
                if (asset != null && asset._isValid)
                {
                    for (int i = 0; i < asset.nameCount; i++)
                        listBox2.Items.Add(i.ToString("X4") + " : " + asset.nameTable[i]);
                    for (int i = 0; i < asset.importCount; i++)
                        listBox3.Items.Add(i.ToString("X4") + " : Class=" + asset.importTable[i]._className + " Name=" + asset.importTable[i]._name);
                    for (int i = 0; i < asset.exportCount; i++)
                        listBox4.Items.Add(i.ToString("X4") + " : " + asset.exportTable[i]._name + " (" + asset.GetName(asset.exportTable[i].classIdx) + ")");
                    currentAsset = asset;
                }
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
                            if ((j % 7) == 0)
                            {
                                Application.DoEvents();
                                if (ex._exit)
                                {
                                    ex.Close();
                                    return;
                                }
                            }
                            string output = fbd.SelectedPath + "\\" + entry.path.Replace("/", "\\");
                            Directory.CreateDirectory(Path.GetDirectoryName(output));
                            file.ExportData(entry, output);
                        }
                    }
                }
                ex.Close();
                MessageBox.Show("Done.");
            }
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n == -1 || currentAsset == null)
                return;
            UExport ex = currentAsset.exportTable[n];
            hb2.ByteProvider = new DynamicByteProvider(ex._data);
            try
            {
                rtb1.Text = currentAsset.ParseProperties(ex);
            }
            catch (Exception exc)
            {
                rtb1.Text = exc.ToString();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n == -1 || currentAsset == null)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.bin|*.bin";
            d.FileName = currentAsset.exportTable[n]._name + ".bin";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, currentAsset.exportTable[n]._data);
                MessageBox.Show("Done.");
            }
        }
    }
}
