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
using Microsoft.VisualBasic;
using System.Drawing.Imaging;

namespace PUBGLiteExplorerWV
{
    public partial class MainWindow : Form
    {
        public List<PAKFile> files = new List<PAKFile>();
        public UAsset currentAsset = null;
        public UTexture2D currentTex = null;
        public UStaticMesh currentStatMesh = null;
        public ULevel currentLevel = null;
        public UHirarchicalInstancedStaticMeshComponent currentHISMC;
        public UInstancedFoliageActor currentIFA;
        public URandomPositionPlayerStart currentRPPS;
        public string currentStatMeshName = "";
        public string currentAssetPath = "";
        public MainWindow()
        {
            InitializeComponent();
            toolStripComboBox1.SelectedIndex = 1;
            tabControl1.SelectedTab = tabPage2;
            tabControl2.TabPages.Remove(tabPage6);
        }

        private void loadSinglePAKFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.pak|*.pak";
            if (d.ShowDialog() == DialogResult.OK)
            {
                files = new List<PAKFile>();
                files.Add(new PAKFile(d.FileName));
                RefreshFiles();
            }
        }

        private void loadFolderOfPAKFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (File.Exists("last_path.txt"))
                fbd.SelectedPath = File.ReadAllText("last_path.txt").Trim();
            else
                fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText("last_path.txt", fbd.SelectedPath);
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
                    if (file.header.isValid)
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
                List<string> list = new List<string>();
                listBox1.Visible = false;
                pb1.Value = 0;
                pb1.Maximum = files.Count;
                foreach (PAKFile file in files)
                {
                    status.Text = "Adding paths from " + Path.GetFileName(file.myPath) + " to list...";
                    pb1.Value++;
                    Application.DoEvents();
                    if (file.header.isValid)
                        foreach (PAKFileEntry entry in file.table.entries)
                            list.Add(entry.path);
                }
                pb1.Value = 0;
                if (alphabeticalSortingToolStripMenuItem.Checked)
                {
                    status.Text = "Sorting list...";
                    Application.DoEvents();
                    list.Sort();
                }
                status.Text = "Insert in listbox...";
                Application.DoEvents();
                listBox1.Items.AddRange(list.ToArray());
                listBox1.Visible = true;
            }
            if (n == 1)
            {
                TreeNode root = new TreeNode("ROOT");
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(root);
                pb1.Value = 0;
                pb1.Maximum = files.Count;
                foreach (PAKFile file in files)
                {
                    status.Text = "Adding paths from " + Path.GetFileName(file.myPath) + " to tree...";
                    pb1.Value++;
                    Application.DoEvents();
                    if (file.header.isValid)
                        foreach (PAKFileEntry entry in file.table.entries)
                            AddPathToNode(root, entry.path);
                }
                pb1.Value = 0;
                if (alphabeticalSortingToolStripMenuItem.Checked)
                {
                    status.Text = "Sorting tree...";
                    Application.DoEvents();
                    treeView1.Sort();
                }
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
                if (file.header.isValid)
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == path)
                        {
                            currentAssetPath = path;
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
                if (file.header.isValid)
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == path)
                        {
                            LoadAsset(file, entry);
                            break;
                        }
        }

        private UAsset LoadAsset(PAKFile file, PAKFileEntry entry, bool onlyLoad = false)
        {
            byte[] data = file.getEntryData(entry);
            if (!onlyLoad)
            {
                currentAsset = null;
                label2.Text = "PAK Path   = " + file.myPath + "\nAsset Path = " + entry.path;
                listBox2.Items.Clear();
                listBox3.Items.Clear();
                listBox4.Items.Clear();
                listBox5.Items.Clear();
                rtb1.Text = "";
                hb2.ByteProvider = new DynamicByteProvider(new byte[0]);
                hb3.ByteProvider = new DynamicByteProvider(new byte[0]);
                if (tabControl2.TabPages.Contains(tabPage6))
                    tabControl2.TabPages.Remove(tabPage6);
                hb1.ByteProvider = new DynamicByteProvider(data);
            }
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
                    uexpData = file.getEntryData(uexp);
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
                    ubulkData = file.getEntryData(ubulk);
                if(uexpData != null && ubulkData != null)
                    asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), new MemoryStream(ubulkData));
                else if (uexpData != null)
                    asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), null);
                else
                    asset = new UAsset(new MemoryStream(data), null, null);
                if (asset != null && asset._isValid && !onlyLoad)
                {
                    for (int i = 0; i < asset.nameCount; i++)
                        listBox2.Items.Add(i.ToString("X4") + " : " + asset.nameTable[i]);
                    for (int i = 0; i < asset.importCount; i++)
                        listBox3.Items.Add(i.ToString("X4") + " : Class=" + asset.importTable[i]._className + " Name=" + asset.importTable[i]._name);
                    for (int i = 0; i < asset.exportCount; i++)
                        listBox4.Items.Add(i.ToString("X4") + " : " + asset.exportTable[i]._name + " (" + asset.GetName(asset.exportTable[i].classIdx) + ")");
                    currentAsset = asset;
                }
                return asset;
            }
            return null;
        }

        private void LoadAssetFile(string assetPath)
        {
            currentAsset = null;
            label2.Text = "Asset Path = " + assetPath;
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            listBox4.Items.Clear();
            listBox5.Items.Clear();
            rtb1.Text = "";
            hb2.ByteProvider = new DynamicByteProvider(new byte[0]);
            hb3.ByteProvider = new DynamicByteProvider(new byte[0]);
            if (tabControl2.TabPages.Contains(tabPage6))
                tabControl2.TabPages.Remove(tabPage6);
            byte[] data = File.ReadAllBytes(assetPath);
            hb1.ByteProvider = new DynamicByteProvider(data);
            UAsset asset = null;
            string fileBase = Path.GetDirectoryName(assetPath) + "\\" + Path.GetFileNameWithoutExtension(assetPath);
            byte[] uexpData = null; 
            if (File.Exists(fileBase + ".uexp"))
                uexpData = File.ReadAllBytes(fileBase + ".uexp");
            byte[] ubulkData = null;
            if (File.Exists(fileBase + ".ubulk"))
                ubulkData = File.ReadAllBytes(fileBase + ".ubulk");
            if (uexpData != null && ubulkData != null)
                asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), new MemoryStream(ubulkData));
            else if (uexpData != null)
                asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), null);
            else
                asset = new UAsset(new MemoryStream(data), null, null);
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
                if (file.header.isValid)
                    foreach (PAKFileEntry entry in file.table.entries)
                        if (entry.path == exportName)
                            if (d.ShowDialog() == DialogResult.OK)
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
            if (fbd.ShowDialog() == DialogResult.OK)
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
                    if (file.header.isValid)
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
            if (tabControl2.TabPages.Contains(tabPage6))
                tabControl2.TabPages.Remove(tabPage6);
            currentTex = null;
            int n = listBox4.SelectedIndex;
            if (n == -1 || currentAsset == null)
                return;
            UExport ex = currentAsset.exportTable[n];
            try
            {
                hb2.ByteProvider = new DynamicByteProvider(ex._data);
                rtb1.Text = currentAsset.ParseProperties(ex);
                if (currentAsset.GetName(ex.classIdx) == "Texture2D")
                {
                    byte[] ubulkData = currentAsset._ubulkData;
                    if (ubulkData != null)
                        currentTex = new UTexture2D(new MemoryStream(ex._data), currentAsset, new MemoryStream(ubulkData));
                    else
                        currentTex = new UTexture2D(new MemoryStream(ex._data), currentAsset, null);
                    if (currentTex.mips.Count > 0)
                    {
                        tabControl2.TabPages.Add(tabPage6);
                        listBox5.Items.Clear();
                        foreach (UTexture2DMipMap mip in currentTex.mips)
                            listBox5.Items.Add("Mip " + mip.width + "x" + mip.height);
                        label1.Text = n.ToString("X4") + " : " + ex._name;
                    }
                } 
                else if (currentAsset.GetName(ex.classIdx) == "StaticMesh")
                {
                    byte[] ubulkData = currentAsset._ubulkData;
                    if (ubulkData != null)
                        currentStatMesh = new UStaticMesh(new MemoryStream(ex._data), currentAsset, new MemoryStream(ubulkData));
                    else
                        currentStatMesh = new UStaticMesh(new MemoryStream(ex._data), currentAsset, null);
                    currentStatMeshName = ex._name;
                }
                else if (currentAsset.GetName(ex.classIdx) == "Level")
                {
                    byte[] ubulkData = currentAsset._ubulkData;
                    if (ubulkData != null)
                        currentLevel = new ULevel(new MemoryStream(ex._data), currentAsset, new MemoryStream(ubulkData));
                    else
                        currentLevel = new ULevel(new MemoryStream(ex._data), currentAsset, null);
                    rtb1.Text += "\n\n" + currentLevel.GetDetails();                    
                }
                else if (currentAsset.GetName(ex.classIdx) == "FoliageInstancedStaticMeshComponent")
                {
                    byte[] ubulkData = currentAsset._ubulkData;
                    if (ubulkData != null)
                        currentHISMC = new UHirarchicalInstancedStaticMeshComponent(new MemoryStream(ex._data), currentAsset, new MemoryStream(ubulkData));
                    else
                        currentHISMC = new UHirarchicalInstancedStaticMeshComponent(new MemoryStream(ex._data), currentAsset, null);
                    rtb1.Text += "\n\n" + currentHISMC.GetDetails();
                }
                else if (currentAsset.GetName(ex.classIdx) == "InstancedFoliageActor")
                {
                    currentIFA = new UInstancedFoliageActor(new MemoryStream(ex._data), currentAsset, null);
                    rtb1.Text += "\n\n" + currentIFA.GetDetails(false);
                }
                else if (currentAsset.GetName(ex.classIdx) == "RandomPositionPlayerStart")
                {
                    byte[] ubulkData = currentAsset._ubulkData;
                    if (ubulkData != null)
                        currentRPPS = new URandomPositionPlayerStart(new MemoryStream(ex._data), currentAsset, new MemoryStream(ubulkData));
                    else
                        currentRPPS = new URandomPositionPlayerStart(new MemoryStream(ex._data), currentAsset, null);
                    rtb1.Text += "\n\n" + currentRPPS.GetDetails();
                }
            }
            catch (Exception exc)
            {
                rtb1.Text = exc.ToString();
            }
        }

        private void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            int m = listBox5.SelectedIndex;
            if (n == -1 || m == -1 || currentAsset == null || currentTex == null)
                return;
            hb3.ByteProvider = new DynamicByteProvider(currentTex.mips[m].data);
            try
            {
                pic1.Image = currentTex.mips[m].MakeBitmap();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                try
                {
                    byte[] data = currentTex.mips[m].MakeDDS(currentTex, currentAsset.exportTable[n]);
                    pic1.Image = Helper.DDS2BMP(data);
                }
                catch (Exception ex2)
                {
                    Console.Write(ex2.Message);
                }
            }
        }

        private void previewInExportTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n == -1 || currentAsset == null)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.bin|*.bin";
            d.FileName = currentAsset.exportTable[n]._name + ".bin";
            if (d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, currentAsset.exportTable[n]._data);
                MessageBox.Show("Done.");
            }
        }

        private void mipsInTexture2DTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentTex == null)
                return;
            FolderBrowserDialog d = new FolderBrowserDialog();
            if (d.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < currentTex.mips.Count; i++)
                    File.WriteAllBytes(d.SelectedPath + "\\mip_" + i + ".bin", currentTex.mips[i].data);
                MessageBox.Show("Done.");
            }
        }
              
        private void landscapeToTerrainRawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null)
                return;
            List<ULandscapeComponent> lcs = new List<ULandscapeComponent>();
            foreach (UExport exp in currentAsset.exportTable)
                if (currentAsset.GetName(exp.classIdx) == "LandscapeComponent")
                    lcs.Add(new ULandscapeComponent(new MemoryStream(exp._data), currentAsset));
            if ((lcs.Count != 25 && lcs.Count != 16 && lcs.Count != 4) ||
                (lcs[0].data.Length != 0x7E02 && lcs[0].data.Length != 0x1F02))
            {
                if(lcs.Count > 0)
                    MessageBox.Show("Cant export,\nexpected lcs=16 or 4, found=" + lcs.Count + "\n, actual size=0x" + lcs[0].data.Length.ToString("X4"));
                else
                    MessageBox.Show("Cant export,\nexpected lcs=16 or 4, found=" + lcs.Count);
                return;
            }
            int tileSideLen = 4;
            if (lcs.Count == 4)
                tileSideLen = 2;
            if (lcs.Count == 25)
                tileSideLen = 5;
            int subLen = 127;
            if (lcs[0].data.Length == 0x1F02)
                subLen = 63;
            while (true)
            {
                bool found = false;
                for (int i = 0; i < lcs.Count - 1; i++)
                {
                    UProp x1 = Helper.FindPropByName(lcs[i].props, "SectionBaseX");
                    UProp y1 = Helper.FindPropByName(lcs[i].props, "SectionBaseY");
                    UProp x2 = Helper.FindPropByName(lcs[i + 1].props, "SectionBaseX");
                    UProp y2 = Helper.FindPropByName(lcs[i + 1].props, "SectionBaseY");
                    int posX1, posX2, posY1, posY2;
                    posX1 = posX2 = posY1 = posY2 = 0;
                    if (x1 != null) posX1 = ((UIntProperty)x1).value;
                    if (y1 != null) posY1 = ((UIntProperty)y1).value;
                    if (x2 != null) posX2 = ((UIntProperty)x2).value;
                    if (y2 != null) posY2 = ((UIntProperty)y2).value;
                    if (posY1 > posY2 || (posY1 == posY2 && posX1 > posX2))
                    {
                        ULandscapeComponent tmp = lcs[i];
                        lcs[i] = lcs[i + 1];
                        lcs[i + 1] = tmp;
                        found = true;
                    }
                }
                if (!found)
                    break;
            }
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.raw|*.raw";
            int start = currentAssetPath.IndexOf("_x");
            if (start != -1)
            {
                string s = currentAssetPath.Substring(start + 1);
                string[] parts = s.Split('_');
                d.FileName = parts[0].Substring(1) + "_" + parts[1].Substring(1) + ".raw";
            }
            if (d.ShowDialog() == DialogResult.OK)
            {
                MemoryStream result = new MemoryStream();
                for (int tileY = 0; tileY < tileSideLen; tileY++)
                {
                    int rowCount = subLen;
                    if (tileY < tileSideLen - 1)
                        rowCount--;
                    for (int y = 0; y < rowCount; y++)
                        for (int tileX = 0; tileX < tileSideLen; tileX++)
                        {
                            ULandscapeComponent lc = lcs[tileY * tileSideLen + tileX];
                            if (tileX < tileSideLen - 1)
                                result.Write(lc.data, subLen * 2 * y, (subLen - 1) * 2);
                            else
                                result.Write(lc.data, subLen * 2 * y, subLen * 2);
                        }
                }
                File.WriteAllBytes(d.FileName, result.ToArray());
                MessageBox.Show("Done.");
            }
        }

        private void staticMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentStatMesh == null)
                return;
            FolderBrowserDialog d = new FolderBrowserDialog();
            if (d.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < currentStatMesh.lodRawData.Count; i++)
                    File.WriteAllBytes(d.SelectedPath + "\\lod_" + i + "_static_mesh.bin", currentStatMesh.lodRawData[i]);
                MessageBox.Show("Done.");
            }
        }

        private void staticMeshLODsAsPSKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentStatMesh == null || currentStatMesh.lods.Count == 0)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.psk|*.psk";
            d.FileName = currentStatMeshName + ".psk";
            if (d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, currentStatMesh.lods[0].MakePSK());
                MessageBox.Show("Done.");                
            }
        }

        private void exportStaticMeshesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                ExportDialog ex = new ExportDialog();
                ex.Show();
                ex.pb1.Value = 0;
                ex.pb1.Maximum = files.Count + 1;
                for (int i = 0; i < files.Count; i++)
                    try
                    {
                        PAKFile file = files[i];
                        ex.pb1.Value = i + 1;
                        ex.label1.Text = "PAK file " + (i + 1) + "/" + files.Count + " : " + Path.GetFileName(file.myPath);
                        Application.DoEvents();
                        if (file.header.isValid)
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
                                if (entry.path.EndsWith(".uasset"))
                                {
                                    string exportPath = fbd.SelectedPath + "\\" + Path.GetDirectoryName(entry.path) + "\\";
                                    byte[] data = file.getEntryData(entry);
                                    UAsset asset = null;
                                    PAKFileEntry uexp = null;
                                    string uexpPath = Path.GetDirectoryName(entry.path) + "\\" + Path.GetFileNameWithoutExtension(entry.path) + ".uexp";
                                    uexpPath = uexpPath.Replace("\\", "/");
                                    foreach (PAKFileEntry en in file.table.entries)
                                        if (en.path == uexpPath)
                                        {
                                            uexp = en;
                                            break;
                                        }
                                    byte[] uexpData = null;
                                    if (uexp != null)
                                        uexpData = file.getEntryData(uexp);
                                    PAKFileEntry ubulk = null;
                                    string ubulkPath = Path.GetDirectoryName(entry.path) + "\\" + Path.GetFileNameWithoutExtension(entry.path) + ".ubulk";
                                    ubulkPath = ubulkPath.Replace("\\", "/");
                                    foreach (PAKFileEntry en in file.table.entries)
                                        if (en.path == ubulkPath)
                                        {
                                            ubulk = en;
                                            break;
                                        }
                                    byte[] ubulkData = null;
                                    if (ubulk != null)
                                        ubulkData = file.getEntryData(ubulk);
                                    if (uexpData != null && ubulkData != null)
                                        asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), new MemoryStream(ubulkData));
                                    else if (uexpData != null)
                                        asset = new UAsset(new MemoryStream(data), new MemoryStream(uexpData), null);
                                    else
                                        asset = new UAsset(new MemoryStream(data), null, null);
                                    if (asset != null && asset._isValid)
                                    {
                                        foreach (UExport exp in asset.exportTable)
                                            if (asset.GetName(exp.classIdx) == "StaticMesh")
                                                try
                                                {

                                                    UStaticMesh mesh;
                                                    if (ubulkData == null)
                                                        mesh = new UStaticMesh(new MemoryStream(exp._data), asset, null);
                                                    else
                                                        mesh = new UStaticMesh(new MemoryStream(exp._data), asset, new MemoryStream(ubulkData));
                                                    if (mesh.lods == null || mesh.lods.Count == 0)
                                                        return;
                                                    byte[] pskData = mesh.lods[0].MakePSK();
                                                    if (pskData.Length == 0)
                                                        return;
                                                    if (!Directory.Exists(exportPath))
                                                        Directory.CreateDirectory(exportPath);
                                                    string name = exp._name + ".psk";
                                                    File.WriteAllBytes(exportPath + name, pskData);
                                                }
                                                catch { }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                ex.Close();
                MessageBox.Show("Done.");
            }
        }

        private void dumpScriptSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<ULandscapeComponent> lcs = new List<ULandscapeComponent>();
            foreach (UExport exp in currentAsset.exportTable)
                if (currentAsset.GetName(exp.classIdx) == "ScriptBlueprintGeneratedClass")
                {
                    

                    SaveFileDialog d = new SaveFileDialog();
                    d.Filter = "*.lua|*.lua";
                    d.FileName = treeView1.SelectedNode.Text.Split('.')[0] + ".lua";
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        List<UProperty> props = new List<UProperty>();
                        MemoryStream s = new MemoryStream(exp._data);
                        while (true)
                        {
                            UProperty p = new UProperty(s, currentAsset);
                            if (p.name == "None")
                                break;
                            props.Add(p);
                        }
                        string text = ((UStrProperty)props[0].prop).value;
                        File.WriteAllText(d.FileName, text);
                    }
                    return;
                }
        }

        private void exportAllLevelInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == DialogResult.OK)
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
                    if (file.header.isValid)
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
                            if (entry.path.EndsWith(".umap"))
                            {
                                UAsset asset = LoadAsset(file, entry, true);
                                ULevel level = null;
                                foreach (UExport export in asset.exportTable)
                                    if (asset.GetName(export.classIdx) == "Level")
                                    {
                                        byte[] ubulkData = asset._ubulkData;
                                        if (ubulkData != null)
                                            level = new ULevel(new MemoryStream(export._data), asset, new MemoryStream(ubulkData));
                                        else
                                            level = new ULevel(new MemoryStream(export._data), asset, null);
                                        break;
                                    }
                                if (level != null)
                                {
                                    string output = fbd.SelectedPath + "\\" + entry.path.Replace("/", "\\");
                                    output = output.Replace(".umap", ".txt");
                                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                                    try
                                    {
                                        File.WriteAllText(output, level.GetDetails());
                                        output = output.Replace(".txt", ".wvm");
                                        File.WriteAllBytes(output, level.rawData.ToArray());
                                    }
                                    catch
                                    {
                                        File.WriteAllText(output, "ERROR");
                                    }
                                }
                            }
                        }
                    }
                }
                ex.Close();
                MessageBox.Show("Done.");
            }
        }

        private void findPersistenLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null)
                return;
            for(int i = 0; i < currentAsset.exportCount; i++)
                if(currentAsset.exportTable[i]._name == "PersistentLevel")
                {
                    listBox4.SelectedIndex = i;
                    break;
                }
        }

        private void loadUAssetFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.uasset|*.uasset";
            if(d.ShowDialog() == DialogResult.OK)
                LoadAssetFile(d.FileName);
        }

        private void dumpPersistentMapDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null || currentLevel == null)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.wvm|*.wvm";
            if(d.ShowDialog() ==  DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, currentLevel.rawData.ToArray());
                MessageBox.Show("Done.");
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string t = toolStripTextBox1.Text.ToLower();
            int start = listBox2.SelectedIndex + 1;
            for(int i = start; i < listBox2.Items.Count; i++)
            {
                if(listBox2.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox2.SelectedIndex = i;
                    break;
                }
            }
            for (int i = 0; i < start; i++)
            {
                if (listBox2.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox2.SelectedIndex = i;
                    return;
                }
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            string t = toolStripTextBox2.Text.ToLower();
            int start = listBox3.SelectedIndex + 1;
            for (int i = start; i < listBox3.Items.Count; i++)
            {
                if (listBox3.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox3.SelectedIndex = i;
                    return;
                }
            }
            for (int i = 0; i < start; i++)
            {
                if (listBox3.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox3.SelectedIndex = i;
                    break;
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            string t = toolStripTextBox3.Text.ToLower();
            int start = listBox4.SelectedIndex + 1;
            for (int i = start; i < listBox4.Items.Count; i++)
            {
                if (listBox4.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox4.SelectedIndex = i;
                    return;
                }
            }
            for (int i = 0; i < start; i++)
            {
                if (listBox4.Items[i].ToString().ToLower().Contains(t))
                {
                    listBox4.SelectedIndex = i;
                    break;
                }
            }
        }

        private void listBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n != -1)
                Clipboard.SetText(listBox2.SelectedItem.ToString());
        }

        private void listBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int n = listBox3.SelectedIndex;
            if (n != -1)
                Clipboard.SetText(listBox3.SelectedItem.ToString());
        }

        private void listBox4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int n = listBox4.SelectedIndex;
            if (n != -1)
                Clipboard.SetText(listBox4.SelectedItem.ToString());
        }

        private List<string> Splat_FindAllLayerNames(List<ULandscapeComponent> lcs)
        {
            List<string> allLayerNames = new List<string>();
            foreach(ULandscapeComponent lc in lcs)
            {
                UProp wMapLayerAllocations = Helper.FindPropByName(lc.props, "WeightmapLayerAllocations");
                if (wMapLayerAllocations != null)
                {
                    UArrayProperty allocs = (UArrayProperty)wMapLayerAllocations;
                    int layerCount = allocs.subProps.Count;
                    foreach (UProperty sp in allocs.subProps)
                    {
                        UStructProperty struc = (UStructProperty)sp.prop;
                        UObjectProperty objInfo = (UObjectProperty)struc.subProps[0].prop;
                        if (!allLayerNames.Contains(objInfo.objName))
                            allLayerNames.Add(objInfo.objName);
                    }
                }
            }
            return allLayerNames;
        }

        private int Splat_GetTextureSize(ULandscapeComponent lc, MemoryStream ubulkStream)
        {
            UArrayProperty wMapTextures = (UArrayProperty)Helper.FindPropByName(lc.props, "WeightmapTextures");
            uint texIdx = BitConverter.ToUInt32(wMapTextures.data, 4) - 1;
            UTexture2D texObj = new UTexture2D(new MemoryStream(currentAsset.exportTable[(int)texIdx]._data), currentAsset, ubulkStream);
            return (int)texObj.height;
        }

        private void Splat_ApplyWeights(ULandscapeComponent lc, Bitmap resultWeightMap, List<string> layerNames, int baseSize, int sideLen, MemoryStream ubulkStream)
        {
            UProp sectionBaseX = Helper.FindPropByName(lc.props, "SectionBaseX");
            UProp sectionBaseY = Helper.FindPropByName(lc.props, "SectionBaseY");
            UArrayProperty wMapTextures = (UArrayProperty)Helper.FindPropByName(lc.props, "WeightmapTextures");
            UArrayProperty wMapLayerAllocations = (UArrayProperty)Helper.FindPropByName(lc.props, "WeightmapLayerAllocations");
            UArrayProperty wMats = (UArrayProperty)Helper.FindPropByName(lc.props, "MaterialInstances");
            int offsetX = 0;
            if (sectionBaseX != null)
                offsetX = ((UIntProperty)sectionBaseX).value / (baseSize - 2);
            int offsetY = 0;
            if (sectionBaseY != null)
                offsetY = ((UIntProperty)sectionBaseY).value / (baseSize - 2);
            offsetX = offsetX % sideLen;
            offsetY = offsetY % sideLen;
            byte[,,] resultMap = new byte[baseSize, baseSize, 4];
            if (wMapTextures != null && wMapLayerAllocations != null && wMats != null)
            {
                uint materialIndex = BitConverter.ToUInt32(wMats.data, 4) - 1;
                ULandscapeMaterialInstanceConstant lmic = new ULandscapeMaterialInstanceConstant(new MemoryStream(currentAsset.exportTable[(int)materialIndex]._data), currentAsset);
                List<Bitmap> weightTextures = new List<Bitmap>();
                for (int i = 1; i < wMapTextures.data.Length / 4; i++)
                {
                    uint texIdx = BitConverter.ToUInt32(wMapTextures.data, i * 4) - 1;
                    UTexture2D texObj = new UTexture2D(new MemoryStream(currentAsset.exportTable[(int)texIdx]._data), currentAsset, ubulkStream);
                    weightTextures.Add(texObj.mips[0].MakeBitmap());
                }
                int currentChannel = 0;
                foreach (string layerName in layerNames)
                {
                    int layerCount = wMapLayerAllocations.subProps.Count;
                    for (int i = 0; i < layerCount; i++)
                    {
                        UStructProperty struc = (UStructProperty)wMapLayerAllocations.subProps[i].prop;
                        UObjectProperty objInfo = (UObjectProperty)struc.subProps[0].prop;
                        if (objInfo.objName == layerName)
                        {
                            byte texIndex = ((UByteProperty)struc.subProps[1].prop).value;
                            int test = lmic.FindChannelIndex(layerName);
                            if (test == -1)
                                return;
                            byte texChannel = (byte)test;
                            for (int y = 0; y < baseSize; y++)
                                for (int x = 0; x < baseSize; x++)
                                {
                                    Color c = weightTextures[texIndex].GetPixel(x, y);
                                    byte[] ca = new byte[] { c.A, c.R, c.G, c.B};
                                    resultMap[x, y, currentChannel] = ca[texChannel];
                                }
                            break;
                        }
                    }
                    currentChannel++;
                }
                for (int y = 0; y < baseSize; y++)
                    for (int x = 0; x < baseSize; x++)
                    {
                        Color c = Color.FromArgb(resultMap[x, y, 0], resultMap[x, y, 1], resultMap[x, y, 2], resultMap[x, y, 3]);
                        resultWeightMap.SetPixel(offsetX * baseSize + x, offsetY * baseSize + y, c);
                    }
            }
        }

        private void dumpSplatMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null)
                return;
            try
            {
                byte[] ubulkData = currentAsset._ubulkData;
                MemoryStream ubulkStream = null;
                if (ubulkData != null)
                    ubulkStream = new MemoryStream(ubulkData);
                List<ULandscapeComponent> landscapeComponents = new List<ULandscapeComponent>();
                foreach (UExport export in currentAsset.exportTable)
                    if (currentAsset.GetClassName(export.classIdx) == "LandscapeComponent")
                        landscapeComponents.Add(new ULandscapeComponent(new MemoryStream(export._data), currentAsset));
                int sideLen;
                if (landscapeComponents.Count == 4)
                    sideLen = 2;
                else if (landscapeComponents.Count == 16)
                    sideLen = 4;
                else if (landscapeComponents.Count == 25)
                    sideLen = 5;
                else
                    return;
                List<string> allLayerNames = Splat_FindAllLayerNames(landscapeComponents);
                int baseSize = Splat_GetTextureSize(landscapeComponents[0], ubulkStream);
                Bitmap resultWeightMap = new Bitmap(sideLen * baseSize, sideLen * baseSize);
                Graphics gWeights = Graphics.FromImage(resultWeightMap);
                gWeights.Clear(Color.FromArgb(0, 0, 0, 0));
                LayerSelector lSel = new LayerSelector();
                lSel.checkedListBox1.Items.Clear();
                foreach (string name in allLayerNames)
                    lSel.checkedListBox1.Items.Add(name);
                List<string> selectedLayerNames;
                while (true)
                {
                    selectedLayerNames = new List<string>();
                    if (lSel.ShowDialog() != DialogResult.OK)
                        return;
                    foreach (string sel in lSel.checkedListBox1.CheckedItems)
                        selectedLayerNames.Add(sel);
                    if (selectedLayerNames.Count > 0 && selectedLayerNames.Count <= 4)
                        break;
                    if (selectedLayerNames.Count == 0)
                        MessageBox.Show("Please select at least 1 layer");
                    else
                        MessageBox.Show("Please select max 4 layers");
                }
                foreach (ULandscapeComponent lc in landscapeComponents)
                    Splat_ApplyWeights(lc, resultWeightMap, selectedLayerNames, baseSize, sideLen, ubulkStream);
                SaveFileDialog d = new SaveFileDialog();
                d.Filter = "*.png|*.png";
                if(d.ShowDialog() == DialogResult.OK)
                {
                    resultWeightMap.Save(d.FileName);
                    MessageBox.Show("Done.");
                }
            }
            catch { }
        }

        private void saveAsDdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox4.SelectedIndex;
            int m = listBox5.SelectedIndex;
            if (n == -1 || m == -1 || currentAsset == null || currentTex == null)
                return;
            UExport ex = currentAsset.exportTable[n];
            byte[] result = currentTex.mips[m].MakeDDS(currentTex, ex);
            if (result.Length == 0)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.dds|*.dds";
            d.FileName = ex._name + ".dds";
            if(d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, result.ToArray());
                MessageBox.Show("Done.");
            }
        }

        private void materialGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null)
                return;
            try
            {
                UMaterial mat = null;
                foreach (UExport export in currentAsset.exportTable)
                    if (currentAsset.GetClassName(export.classIdx) == "Material")
                    {
                        mat = new UMaterial(new MemoryStream(export._data), currentAsset);
                        break;
                    }
                if (mat == null)
                    return;                
                SaveFileDialog d = new SaveFileDialog();
                d.Filter = "*.dot|*.dot";
                d.FileName = "Material.dot";
                if(d.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(d.FileName, mat.MakeDotGraph());
                    MessageBox.Show("Done.");
                }
            }
            catch { }
        }
        public void DumpMaterialExpressionNodes(StringBuilder sb, UAsset asset, uint exportID)
        {
            sb.Append("N" + exportID + "[shape=box label=\"");
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch(cname)
            {
                default:
                    sb.Append(cname);
                    break;
            }
            sb.AppendLine("\"]");
        }
        public void DumpMaterialExpressionEdges(StringBuilder sb, UAsset asset, uint exportID)
        {
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch (cname)
            {
                default:
                    //sb.Append(cname);
                    break;
            }
        }

        private void findReferencedTexturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentStatMesh == null || currentStatMesh.lods.Count == 0)
                return;
            StringBuilder sb = new StringBuilder();
            List<string> pkgsToDo = Helper.GetAllImported(currentAsset.importTable);
            List<string> pkgsDone = new List<string>();
            while (pkgsToDo.Count > 0)
            {
                string pkg = pkgsToDo[0];
                pkgsToDo.RemoveAt(0);
                pkgsDone.Add(pkg);
                bool found = false;
                UAsset asset = null;
                foreach (PAKFile file in files)
                {
                    string mountedPath = pkg + ".uasset";
                    foreach (PAKFileEntry entry in file.table.entries)
                    {
                        if (entry.path == mountedPath)
                        {
                            asset = LoadAsset(file, entry, true);
                            if(asset != null && asset._isValid)
                                found = true;
                            break;
                        }
                    }
                }
                sb.AppendLine("Processing " + pkg + " -> " + (found ? "found" : "not found"));
                if (found)
                {
                    List<string> pkgs = Helper.GetAllImported(asset.importTable);
                    foreach (string p in pkgs)
                        if (!pkgsDone.Contains(p))
                            pkgsToDo.Add(p);
                    List<string> textures = Helper.GetAllImported(asset.importTable, "Texture2D", false);
                    foreach (string tex in textures)
                        sb.AppendLine(" -> Found referenced Texture2D " + tex);
                }
            }
            AnalysisResult ar = new AnalysisResult();
            ar.rtb1.Text = sb.ToString();
            ar.ShowDialog();
        }

        private void uVFormatSelectorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new UVFormatSelector().ShowDialog();
        }

        private int[] FindMaterialAndUVChannels()
        {
            int[] result = new int[] { 1, 2, 3, 16, 24, 25, 22, 27 };
            try
            {
                UArrayProperty arr = (UArrayProperty)currentStatMesh.props[0].prop;
                UStructProperty str = (UStructProperty)arr.subProps[0].prop;
                UObjectProperty matInterface = (UObjectProperty)str.subProps[0].prop;
                UImport import0 = currentAsset.importTable[-matInterface.value - 1];
                string name = import0._name;
                UImport import1 = null;
                foreach(UImport imp in currentAsset.importTable)
                    if(imp._className == "Package" && imp._name.Contains(name))
                    {
                        import1 = imp;
                        break;
                    }
                string path1 = "ShadowTrackerExtra/Content/" + import1._name.Substring(6) + ".uasset";
                string path2 = "ShadowTrackerExtra/Content/" + import1._name.Substring(6) + ".uexp";
                PAKFile pakfile = null;
                PAKFileEntry pakFileAsset = null;
                PAKFileEntry pakFileExp = null;
                foreach (PAKFile file in files)
                    if (file.header.isValid)
                        foreach (PAKFileEntry entry in file.table.entries)
                            if (entry.path == path1)
                            {
                                pakfile = file;
                                pakFileAsset = entry;
                                if (pakFileAsset != null && pakFileExp != null)
                                    break;
                            }
                            else if (entry.path == path2)
                            {
                                pakfile = file;
                                pakFileExp = entry;
                                if (pakFileAsset != null && pakFileExp != null)
                                    break;
                            }
                UAsset asset = new UAsset(new MemoryStream(pakfile.getEntryData(pakFileAsset)), new MemoryStream(pakfile.getEntryData(pakFileExp)), null);
                foreach (UExport exp in asset.exportTable)
                    if (exp._name == name)
                    {
                        MemoryStream s = new MemoryStream(exp._data);
                        List<UProperty> props = new List<UProperty>();
                        while (true)
                        {
                            UProperty p = new UProperty(s, asset);
                            if (p.name == "None")
                                break;
                            props.Add(p);
                        }
                        UArrayProperty vpv = null;
                        foreach(UProperty p in props)
                            if(p.name == "VectorParameterValues")
                            {
                                vpv = (UArrayProperty)p.prop;
                                break;
                            }
                        for (int i = 0; i < 2; i++)
                        {
                            UStructProperty substr = (UStructProperty)vpv.subProps[i].prop;
                            UStructProperty linearColor = (UStructProperty)substr.subProps[1].prop;
                            MemoryStream s2 = new MemoryStream(linearColor.data);
                            for (int j = 0; j < 4; j++)
                                result[i * 4 + j] = (int)Helper.ReadFloat(s2);
                        }
                        break;
                    }
            }
            catch { }
            return result;
        }

        private void staticMesh4x8AsPSKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentStatMesh == null || currentStatMesh.lods.Count == 0)
                return;
            UVChannelSelector uvsel = new UVChannelSelector();
            uvsel.channels = FindMaterialAndUVChannels();
            uvsel.ShowDialog();
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.psk|*.psk";
            d.FileName = currentStatMeshName + ".psk";
            if (d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(d.FileName, currentStatMesh.lods[0].MakePSK(uvsel.channels));
                MessageBox.Show("Done.");
            }
        }

        private void dumpVehicleDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (File.Exists("last_path.txt"))
                fbd.SelectedPath = File.ReadAllText("last_path.txt").Trim();
            else
                fbd.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string baseFolder = fbd.SelectedPath;
                if (!baseFolder.EndsWith("\\"))
                    baseFolder += "\\";
                StringBuilder sb = new StringBuilder();                
                UAsset asset;
                int count = 0;
                foreach (PAKFile file in files)
                    foreach (PAKFileEntry entry in file.table.entries)
                        count++;
                pb1.Maximum = count;
                int idx = 0;
                foreach (PAKFile file in files)
                    foreach (PAKFileEntry entry in file.table.entries)
                    {
                        idx++;
                        if (!entry.path.EndsWith(".uasset") || !entry.path.Contains("Vehicle"))
                            continue;
                        if (idx % 100 == 0)
                        {
                            status.Text = "Processing... " + entry.path;
                            pb1.Value = idx;
                            Application.DoEvents();
                        }
                        asset = LoadAsset(file, entry, true);
                        if (asset != null && asset._isValid)
                            foreach (UExport export in asset.exportTable)
                                if (asset.GetName(export.classIdx) == "STExtraVehicleMovementComponent4W")
                                {
                                    MemoryStream s = new MemoryStream(export._data);
                                    List<UProperty> props = new List<UProperty>();
                                    while (true)
                                    {
                                        UProperty p = new UProperty(s, asset);
                                        if (p.name == "None")
                                            break;
                                        props.Add(p);
                                    }
                                    string details = asset.ParseProperties(export);
                                    string path = baseFolder + entry.path.Replace("/", "\\").Replace(".uasset", ".txt");
                                    string dir = Path.GetDirectoryName(path);
                                    if (!Directory.Exists(dir))
                                        Directory.CreateDirectory(dir);
                                    File.WriteAllText(path, details);
                                    sb.AppendLine(path);
                                }
                    }
                pb1.Value = 0;
                status.Text = "";
                AnalysisResult ar = new AnalysisResult();
                ar.rtb1.Text = sb.ToString();
                ar.ShowDialog();
            }
        }
    }
}
