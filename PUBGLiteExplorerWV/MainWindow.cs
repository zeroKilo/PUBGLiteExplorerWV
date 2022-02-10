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
using Microsoft.VisualBasic;

namespace PUBGLiteExplorerWV
{
    public partial class MainWindow : Form
    {
        public List<PAKFile> files = new List<PAKFile>();
        public UAsset currentAsset = null;
        public UTexture2D currentTex = null;
        public UStaticMesh currentStatMesh = null;
        public ULevel currentLevel = null;
        public string currentStatMeshName = "";
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
                foreach (PAKFile file in files)
                    if (file.header.isValid)
                        foreach (PAKFileEntry entry in file.table.entries)
                            listBox1.Items.Add(entry.path);
            }
            if (n == 1)
            {
                TreeNode root = new TreeNode("ROOT");
                foreach (PAKFile file in files)
                    if (file.header.isValid)
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
                if (file.header.isValid)
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
                if (file.header.isValid)
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
                if (file.header.isValid)
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
            }
            catch (Exception exc)
            {
                rtb1.Text = exc.ToString();
            }
        }

        private void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox5.SelectedIndex;
            if (n == -1 || currentTex == null)
                return;
            hb3.ByteProvider = new DynamicByteProvider(currentTex.mips[n].data);
            try
            {
                pic1.Image = currentTex.mips[n].MakeBitmap();
            }
            catch { }
        }

        private void previewInExportTableToolStripMenuItem_Click(object sender, EventArgs e)
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

        private UProp findPropByName(List<UProperty>list, string name)
        {
            foreach (UProperty p in list)
                if (p.name == name)
                    return p.prop;
            return null;
        }

        private void landscapeToTerrainRawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAsset == null)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.raw|*.raw";
            if (d.ShowDialog() == DialogResult.OK)
            {
                List<ULandscapeComponent> lcs = new List<ULandscapeComponent>();
                foreach (UExport exp in currentAsset.exportTable)
                    if (currentAsset.GetName(exp.classIdx) == "LandscapeComponent")
                        lcs.Add(new ULandscapeComponent(new MemoryStream(exp._data), currentAsset));
                if (lcs.Count != 16 || lcs[0].data.Length != 0x7E02)
                {
                    MessageBox.Show("Cant export,\nexpected lcs=16, found=" + lcs.Count + "\nexpected datasize=0x7E02, actual size=0x" + lcs[0].data.Length.ToString("X4"));
                    return;
                }
                while(true)
                {
                    bool found = false;
                    for(int i = 0; i < 15; i++)
                    {
                        UProp x1 = findPropByName(lcs[i].props, "SectionBaseX");
                        UProp y1 = findPropByName(lcs[i].props, "SectionBaseY");
                        UProp x2 = findPropByName(lcs[i + 1].props, "SectionBaseX");
                        UProp y2 = findPropByName(lcs[i + 1].props, "SectionBaseY");
                        int posX1, posX2, posY1, posY2;
                        posX1 = posX2 = posY1 = posY2 = 0;
                        if (x1 != null) posX1 = ((UIntProperty)x1).value;
                        if (y1 != null) posY1 = ((UIntProperty)y1).value;
                        if (x2 != null) posX2 = ((UIntProperty)x2).value;
                        if (y2 != null) posY2 = ((UIntProperty)y2).value;
                        if (posY1 > posY2 || ( posY1 == posY2 && posX1 > posX2))
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
                MemoryStream result = new MemoryStream();
                for (int ty = 0; ty < 4; ty++)
                    for (int y = 0; y < 127; y++)
                    {
                        for (int tx = 0; tx < 4; tx++)
                        {
                            ULandscapeComponent lc = lcs[ty * 4 + tx];
                            result.Write(lc.data, 254 * y, 254);
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
                string uvSet = Interaction.InputBox("Which set of UV to export? (0-" + ((currentStatMesh.lods[0].uvs[0].Length / 2) - 1) + ")", "UV Set", "2");
                if (uvSet != "")
                {
                    File.WriteAllBytes(d.FileName, currentStatMesh.lods[0].MakePSK(Convert.ToInt32(uvSet)));
                    MessageBox.Show("Done.");
                }
            }
        }

        private void exportStaticMeshesToolStripMenuItem_Click(object sender, EventArgs e)
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
                                    foreach(UExport exp in asset.exportTable)
                                        if(asset.GetName(exp.classIdx) == "StaticMesh")
                                        {
                                            UStaticMesh mesh;
                                            if(ubulkData == null)
                                                mesh = new UStaticMesh(new MemoryStream(exp._data), asset, null);
                                            else
                                                mesh = new UStaticMesh(new MemoryStream(exp._data), asset, new MemoryStream(ubulkData));
                                            if (mesh.lods == null || mesh.lods.Count == 0)
                                                return;
                                            byte[] pskData = mesh.lods[0].MakePSK(2);
                                            if(pskData.Length == 0)
                                                return;
                                            if (!Directory.Exists(exportPath))
                                                Directory.CreateDirectory(exportPath);
                                            string name = exp._name + ".psk";
                                            File.WriteAllBytes(exportPath + name, pskData);
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
                                        if (asset.GetName(exp.classIdx) == "Level")
                                            try
                                            {
                                                ULevel level;
                                                if (ubulkData == null)
                                                    level = new ULevel(new MemoryStream(exp._data), asset, null);
                                                else
                                                    level = new ULevel(new MemoryStream(exp._data), asset, new MemoryStream(ubulkData));
                                                string fileName = fbd.SelectedPath + "\\" + entry.path.Replace("/", "\\") + ".txt";
                                                string dir = Path.GetDirectoryName(fileName);
                                                if (!Directory.Exists(dir))
                                                    Directory.CreateDirectory(dir);
                                                try
                                                {
                                                    File.WriteAllText(fileName, level.GetDetails());
                                                }
                                                catch { }
                                            }
                                            catch { }
                                }
                            }
                        }
                    }
                }
                ex.Close();
                MessageBox.Show("Done.");
            }
        }
    }
}
