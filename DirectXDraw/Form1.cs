using System;
using System.Drawing;
using System.Windows.Forms;
namespace DirectXDraw {
    public partial class Form1 : Form {

        DirectXManager dx;
        string filename;
        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {

            dx = new DirectXManager();
            dx.Initialize(Handle);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e) {

            string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
            filename = data[0];
            Draw(filename);
        }

        private void Draw(string file) {

            dx.Reset(Handle);
            using (Bitmap bmp = new Bitmap(file)) {

                Size sz = bmp.Size;
                if (ClientSize.Width < sz.Width || ClientSize.Height < sz.Height) {

                    double min = Math.Min((double)ClientSize.Width / (double)sz.Width, (double)ClientSize.Height / (double)sz.Height);
                    double w = sz.Width * min;
                    double h = sz.Height * min;
                    sz = new Size((int)w, (int)h);
                }
                dx.InvalidateMemory(bmp, sz, ClientSize);
            }
        }
        private void Form1_Resize(object sender, EventArgs e) {
            if (filename != null)
                Draw(filename);
        }
    }
}