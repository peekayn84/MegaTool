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
    public partial class Form3 : Form
    {
        public struct User
        {
            public string mail;
            public string pass;
            public User(string mail, string pass)
            {
                this.mail = mail;
                this.pass = pass;
            }
        }
        public List<User> user = new List<User>();
        public Form3(List<string> tempMails, List<string> tempPass)
        {
            InitializeComponent();
            for (int i = 0; i < tempMails.Count; i++)
            {
                user.Add(new User(tempMails[i], tempPass[i]));
                comboBox1.Items.Add(tempMails[i]);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            user.Add(new User(comboBox1.Text, textBox1.Text));
            reloadUser();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool find = false;
            for (int i = 0; i < user.Count; i++)
            {
                if (comboBox1.Text == user[i].mail)
                {
                    find = true;
                    textBox1.Text = user[i].pass;
                    break;
                }
                    
            }
            if (!find)
            {
                textBox1.Text = "";
            }
           
        }
        public void reloadUser()
        {
            comboBox1.Items.Clear();
            for (int i = 0; i < user.Count; i++)
            {
                comboBox1.Items.Add(user[i].mail);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0 ;
            }
            else
            {
                comboBox1.Text="";
            }
            
        }
        private void button4_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < user.Count; i++)
            {
                if (comboBox1.Text == user[i].mail)
                {
                    user.RemoveAt(i);
                    user.Add(new User(comboBox1.Text, textBox1.Text));
                    break;
                }
            }
            reloadUser();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < user.Count; i++)
            {
                if (comboBox1.Text == user[i].mail)
                {
                    user.RemoveAt(i);
                    break;
                }
            }
            reloadUser();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (StreamWriter sw = new StreamWriter("user.mgtool", false, System.Text.Encoding.Default))
            {
                for(int i = 0; i < user.Count; i++)
                {
                    sw.WriteLine(user[i].mail+" "+user[i].pass);
                }
            }
            this.Close();
        }
    }
}
