using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WatermelonTime2024_editor
{
    public partial class SignMessagesForm : Form
    {
        public string Sign0 { get => tb_sign0.Text; set { tb_sign0.Text = value; } }
        public string Sign1 { get => tb_sign1.Text; set { tb_sign1.Text = value; } }
        public string Sign2 { get => tb_sign2.Text; set { tb_sign2.Text = value; } }
        public string Sign3 { get => tb_sign3.Text; set { tb_sign3.Text = value; } }
        public string Sign4 { get => tb_sign4.Text; set { tb_sign4.Text = value; } }
        public string Sign5 { get => tb_sign5.Text; set { tb_sign5.Text = value; } }
        public string Sign6 { get => tb_sign6.Text; set { tb_sign6.Text = value; } }
        public string Sign7 { get => tb_sign7.Text; set { tb_sign7.Text = value; } }
        public string Sign8 { get => tb_sign8.Text; set { tb_sign8.Text = value; } }
        public string Sign9 { get => tb_sign9.Text; set { tb_sign9.Text = value; } }



        public SignMessagesForm()
        {
            InitializeComponent();
        }
    }
}
