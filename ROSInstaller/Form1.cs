using SevenZip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ROSInstaller
{
    public partial class Form1 : Form
    {
        private ROSInstallTask _InstallTask;

        public bool ProgressGUIVisible
        {
            set
            {
                label1.Enabled = textBox1.Enabled = btnInstall.Visible = cbOpenCygwin.Enabled = !value;
                btnCancel.Visible = value;
            }
        }

        public Form1()
        {
            InitializeComponent();
#if ROS_INDIGO
            textBox1.Text = @"c:\cygwin.indigo";
#elif ROS_JADE
            textBox1.Text = @"c:\cygwin.jade";
#endif
        }

        class ProgressTracker : ICodeProgress
        {
            private long _Done;
            private readonly long _Total;

            public ProgressTracker(long total)
            {
                _Total = total;
            }

            public void SetProgress(long inSize, long outSize)
            {
                _Done = inSize;
            }

            public void Apply(ProgressBar bar)
            {
                if (_Total == 0)
                    return;
                int val = (int)((_Done * bar.Maximum) / _Total);
                bar.Value = Math.Min(val, bar.Maximum);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            string destDir = textBox1.Text;
            if (Directory.Exists(destDir))
            {
                MessageBox.Show("The target directory already exists. Please remove it first.", "ROS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ProgressGUIVisible = true;

            var oldTask = Interlocked.Exchange(ref _InstallTask, new ROSInstallTask(destDir, cbLog.Checked ? Path.Combine(destDir, "ROSInstall.log") : null));
            if (oldTask != null)
            {
                oldTask.Abort();
                oldTask.Dispose();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var task = _InstallTask;
            if (task == null)
            {
                progressBar1.Value = 0;
                ProgressGUIVisible = false;
            }
            else
            {
                task.ApplyProgress(progressBar1);
                if (task.HasExited)
                {
                    _InstallTask = null;
                    var ex = task.ErrorMessage;
                    if (ex == null)
                    {
                        MessageBox.Show("Installation complete", "ROS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (cbOpenCygwin.Checked)
                            Process.Start(Path.Combine(task.DestDir, "cygwin.bat"));
                        CreateDesktopShortcut(task.DestDir);
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(ex, "ROS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CreateDesktopShortcut(string destDir)
        {
            try
            {
                IShellLink link = (IShellLink)new ShellLink();
                string distroName = "";
#if ROS_INDIGO
            distroName = @"Indigo";
#elif ROS_JADE
            distroName = @"Jade";
#endif

                link.SetPath(Path.Combine(destDir, "cygwin.bat"));
                link.SetIconLocation(Path.Combine(destDir, "cygwin.ico"), 0);

                IPersistFile file = (IPersistFile)link;
                file.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ROS " + distroName + " Cygwin Shell.lnk"), false);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "ROS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            var oldTask = Interlocked.Exchange(ref _InstallTask, null);
            if (oldTask != null)
            {
                oldTask.Abort();
                oldTask.Dispose();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            var oldTask = Interlocked.Exchange(ref _InstallTask, null);
            if (oldTask != null)
            {
                oldTask.Abort();
                oldTask.Dispose();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/codenotes/ros_cygwin/wiki/Release-notes");
        }
    }
}
