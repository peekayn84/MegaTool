using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MegaTool
{
    public partial class Form2 : Form
    {
        List<string> users;
        public Form2(List<string> usersTemp, string path, string pass)
        {
            InitializeComponent();
            users = usersTemp;
            comboBox1.Text=users[0];
            for (int i = 1; i < users.Count; i++)
            {
                comboBox1.Items.Add(users[i]);
            }
            label3.Text = path;
            textBox1.Text = pass;


        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool goodMail = false;
            for (int i = 1; i < users.Count; i++)
            {
                if (users[i] == comboBox1.Text)
                {
                    goodMail = true;
                }
            }
            if (goodMail)
            {
                using (StreamWriter sw = new StreamWriter("config.mgtool", false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(comboBox1.Text);
                    sw.WriteLine(label3.Text);
                    sw.WriteLine(textBox1.Text);
                }
                this.Close();
                //MessageBox.Show("Please, restart app", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //Application.Exit();
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.SelectedPath = label3.Text;
            folderDlg.ShowNewFolderButton = true;
            // Show the FolderBrowserDialog.  
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                label3.Text = folderDlg.SelectedPath;
                label3.Text+=@"\";
            }
        }
    }
}
