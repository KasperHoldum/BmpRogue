using System;
using System.ComponentModel;
using System.Windows.Forms;
using BitmapRogue;

namespace BmpRogue
{
    public partial class MainForm : Form
    {
        private Bitmap bm = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //this.pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);
            this.label1.Text = "";
        }

        //void pictureBox1_Paint(object sender, PaintEventArgs e)
        //{
        //    if (bm != null)
        //    {
        //        System.Drawing.Pen p;
        //        for (int i = 0; i < bm.BitmapInfo.width; i++)
        //        {
        //            for (int j = 0; j < bm.BitmapInfo.height; j++)
        //            {
        //                p = new System.Drawing.Pen(bm.BitmapData[i,j]);
        //                e.Graphics.DrawLine(p, new System.Drawing.Point(i, j), new System.Drawing.Point(i+1, j+1));
        //            }
        //        }
        //    }
        //}

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                bm = null;

                try
                {
                    bm = Bitmap.Parse(openFileDialog1.FileName);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while loading the bitmap!" + Environment.NewLine + Environment.NewLine + ex.Message);
                    return;
                }

                textBox1.Text = bm.HiddenData;
                textBox1.ReadOnly = false;
                UpdateCharactersLeft();
                

                //pictureBox1.Size = new System.Drawing.Size((int)bm.BitmapInfo.width, (int)bm.BitmapInfo.height);
                //pictureBox1.Invalidate();
            }

        }

        private void UpdateCharactersLeft()
        {
            if (bm != null)
            {
                label1.Text = (bm.maxHiddenSpaceLength - textBox1.Text.Length).ToString() + " chars left";
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bm != null)
            {

            }

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string hiddenString = textBox1.Text;
                    bm.Save(saveFileDialog1.FileName, hiddenString);

                    bm = null;
                    textBox1.Text = "";
                    UpdateCharactersLeft();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while saving the bitmap!" + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This application only works with 32-bit BMP files. Not all images are capable of having data stored within using the current storage algorithm. \n\n\n\n ROFLMAO! / QUA ENTERTAINMENTROXURSINBOXORS", "LEZDOCRAZYALLCAPSTITLE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateCharactersLeft();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented");
        }
    }
}
