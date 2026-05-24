using System;
using System.Windows.Forms;
using ImageMagick;
using System.Drawing;
using System.IO;
using System.Linq;
using Ookii.Dialogs.WinForms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FolderIcon
{
    public partial class Form1 : Form
    {
        private string imageName = "";  //image name w/ path stored by drag and drop to preview box
        private string rootPath;       //root folder path
        private string icoPath;     //icon file name with directory path.
        private string icoName;     //icon image name without path.

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
        }
        //
        //"Finish" button
        //
        private void finishBtn_Click(object sender, EventArgs e)
        {
            if (imageName == "" || imageName == null)
            {
                MessageBox.Show("Please add an image.", "Tip");
            }
            else if (rootPath == "" || rootPath == null)
            {
                MessageBox.Show("Please add a folder path.", "Tip");
            }
            else
            {
                GenerateIco();

                //set ico attribute
                File.SetAttributes(icoPath, File.GetAttributes(icoPath) | FileAttributes.Hidden);

                ChangeFolderSettings();

                //set rootFolder attributes
                File.SetAttributes(rootPath, File.GetAttributes(rootPath) | FileAttributes.ReadOnly | FileAttributes.System);
            }
        }
        //
        //"Clear" button
        //
        private void ClearBtn_Click(object sender, EventArgs e)
        {
            folderPathBox.Clear();
        }
        //
        //"Add" Button
        //
        private void addFolderBtn_Click(object sender, EventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                folderPathBox.Text = dialog.SelectedPath;
                rootPath = dialog.SelectedPath;
            }
        }
        //
        //Textbox
        //
        //Change folder path manually by changing textbox text
        private void folderPathBox_TextChanged(object sender, EventArgs e)
        {
            rootPath = folderPathBox.Text;
        }
        //Select all text
        private void folderPathBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            folderPathBox.SelectAll();  // select all text on mouse double click.
        }
        //Show folder path on drop
        private void folderPathBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] temp = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (temp.Length > 0 && !PathHelper.IsFolderPath(temp[0]))
            {
                MessageBox.Show("Only folder path is allowed in this field.", "Warning!");
            }
            else
            {
                rootPath = temp[0];
                folderPathBox.Text = rootPath;
            }
        }
        private void folderPathBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }       
        //
        //Preview Box
        //
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            imageName = "";
            string[] imageNames = (string[])e.Data.GetData(DataFormats.FileDrop);     //get image name with path

            if (imageNames.Length > 1)                            //check only 1 file is dropped
            {
                MessageBox.Show("Only 1 image allowed.");
            }
            else
            {
                if (Path.HasExtension(imageNames[0])) //verity the dropped item is a file, not a folder or without extension
                {
                    if (!IsSupported(imageNames[0].ToLower()))  //verify the file is supported image format
                    {
                        MessageBox.Show("Unsupported format.", "Warning!");
                    }
                    else
                    {
                        imageName = imageNames[0];
                        Image image = Image.FromFile(imageNames[0]);
                        Image bmp = new Bitmap(image);
                        image.Dispose();        //释放对象，解除文件占用问题
                        if (previewBox.Image != null)
                        {
                            previewBox.Image.Dispose();    //清除picturebox之前图片占用的内存
                        }
                        previewBox.Image = bmp;    //show image preview
                    }
                }
                else
                {
                    MessageBox.Show("Only image file supported in this field!", "Warning!");
                }
            }
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        //
        //Other functions
        //
        //Generate Icon image
        private void GenerateIco()
        {
            //generate ico image file with MagickImage library
            using (var image = new MagickImage(imageName))
            {
                image.BackgroundColor = new MagickColor("transparent");
                image.Resize(new MagickGeometry(256, 256));    //resize image with the given size
                image.Extent(256, 256, Gravity.Center);     //set extent size and back color

                //Save ico image to rootPath
                string randomString = RandomString(6);
                string icoName_ini = IniProcessor.ReadValue(rootPath + "\\" + "desktop.ini", ".ShellClassInfo", "IconResource").Split(',')[0];

                //Check if image already exists
                if (File.Exists(rootPath + "\\" + icoName_ini))
                {
                    File.Delete(rootPath + "\\" + icoName_ini);

                    image.Write($@"{rootPath}\icon-{randomString}.ico");
                    icoPath = $@"{rootPath}\icon-{randomString}.ico";
                    icoName = $"icon-{randomString}.ico";
                }
                else
                {
                    image.Write($@"{rootPath}\icon-{randomString}.ico");
                    icoPath = $@"{rootPath}\icon-{randomString}.ico";
                    icoName = $"icon-{randomString}.ico";
                }
            }
        }

        //Change folder settings
        private void ChangeFolderSettings()
        {
            if (File.Exists(rootPath + @"\desktop.ini"))
            {
                File.Delete(rootPath + @"\desktop.ini");

                LPSHFOLDERCUSTOMSETTINGS FolderSettings = new LPSHFOLDERCUSTOMSETTINGS();
                FolderSettings.dwMask = 0x10;
                FolderSettings.pszIconFile = icoName;
                FolderSettings.iIconIndex = 0;

                //UInt32 FCS_READ = 0x00000001;
                UInt32 FCS_FORCEWRITE = 0x00000002;
                //UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;
                UInt32 FCS_WRITE = FCS_FORCEWRITE;

                string pszPath = rootPath;
                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, pszPath, FCS_WRITE);
            }
            else
            {
                LPSHFOLDERCUSTOMSETTINGS FolderSettings = new LPSHFOLDERCUSTOMSETTINGS();
                FolderSettings.dwMask = 0x10;
                FolderSettings.pszIconFile = icoName;
                FolderSettings.iIconIndex = 0;

                //UInt32 FCS_READ = 0x00000001;
                UInt32 FCS_FORCEWRITE = 0x00000002;
                //UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;
                UInt32 FCS_WRITE = FCS_FORCEWRITE;

                string pszPath = rootPath;
                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, pszPath, FCS_WRITE);
            }
        }

        //Import system LPSHFOLDERCUSTOMSETTINGS 
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern UInt32 SHGetSetFolderCustomSettings(ref LPSHFOLDERCUSTOMSETTINGS pfcs, string pszPath, UInt32 dwReadWrite);
        //Folder custome setting properties
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct LPSHFOLDERCUSTOMSETTINGS
        {
            public UInt32 dwSize;
            public UInt32 dwMask;
            public IntPtr pvid;
            public string pszWebViewTemplate;
            public UInt32 cchWebViewTemplate;
            public string pszWebViewTemplateVersion;
            public string pszInfoTip;
            public UInt32 cchInfoTip;
            public IntPtr pclsid;
            public UInt32 dwFlags;
            public string pszIconFile;
            public UInt32 cchIconFile;
            public int iIconIndex;
            public string pszLogo;
            public UInt32 cchLogo;
        }

        //Validate formats
        private bool IsSupported(string filePath)
        {
            switch (Path.GetExtension(filePath))
            {
                case ".jpeg":
                    return true;
                case ".jpg":
                    return true;
                case ".png":
                    return true;
                case ".ico":
                    return true;
                default:
                    return false;
            }
        }

        //Random String generator
        private string RandomString(int digits)
        {
            //define character range
            var chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";

            var random = new Random();
            string randomString = new string(Enumerable.Repeat(chars, digits).Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }

        //Restore folder read-only attribute which could cause icons not showing after moving location
        private void restoreButton_Click(object sender, EventArgs e)
        {
            if(rootPath == null)
            {
                MessageBox.Show("Please select a folder path.", "Warning:");
            }
            else
            {
                DialogResult result = MessageBox.Show("确认修复文件夹封面吗?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    string command = $"/c attrib /s /d +r \"{rootPath}\\*\"";
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c attrib /s /d +r \"{rootPath}\\.\""; //应用于当前文件夹
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    startInfo.Arguments = $"/c attrib /s /d +r \"{rootPath}\\*\""; //应用于其所有子文件和子文件夹
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    MessageBox.Show("修复完成!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否按下了Ctrl+W
            if (e.Control && e.KeyCode == Keys.W)
            {
                Application.Exit(); // 关闭应用程序
            }
        }
    }
}
