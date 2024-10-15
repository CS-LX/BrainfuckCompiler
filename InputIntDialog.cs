using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrainfuckCompiler
{
    public partial class InputIntDialog : Form
    {
        public int num;
        public InputIntDialog()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            num = (int)numericUpDown1.Value;
            DialogResult = DialogResult.OK;
            this.Hide();
        }
    }
}
