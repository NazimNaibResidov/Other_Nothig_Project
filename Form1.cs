
using CMS.Data;
using CMS.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
          var x=  ProductOrm.Current.Select();
            if (x.State==ResultState.Success)
            {
                dataGridView1.DataSource = x.Data;
            }
        }
    }
}
