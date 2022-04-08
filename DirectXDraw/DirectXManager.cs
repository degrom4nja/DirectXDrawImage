using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Drawing.Imaging;

namespace DirectXDraw {
    class DirectXManager {

        private SharpDX.Direct3D11.Device device;
        private SwapChain swapChain;
        private bool working;
        private Texture2D texture;
        RenderTarget renderTarget;

        public void Reset(IntPtr handle) {
            Close();
            Initialize(handle);
        }

        public void Initialize(IntPtr handle) {
            var swapChainDesc = new SwapChainDescription() {
                BufferCount = 1,
                ModeDescription = new ModeDescription(Format.B8G8R8A8_UNorm),
                IsWindowed = true,
                OutputHandle = handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                swapChainDesc,
                out device, out swapChain);

            //var factory = swapChain.GetParent<SharpDX.DXGI.Factory>();
            //factory.MakeWindowAssociation(handle, WindowAssociationFlags.IgnoreAll);

            texture = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            InitializeDirect2D();
        }

        public bool CanDraw { get { return !working && texture != null && renderTarget != null && swapChain != null && device != null; } }

        public void Close() {
            if (texture != null)
                texture.Dispose();

            if (renderTarget != null)
                renderTarget.Dispose();

            if (swapChain != null)
                swapChain.Dispose();

            if (device != null)
                device.Dispose();
        }

        public void InvalidateMemory(System.Drawing.Bitmap source, Size dstSize, Size clientSize) {
            working = true;
            byte[] pixels = ScalingImageMemory(source, dstSize);
            if (pixels == null) {
                Close();
                return;
            }

            SharpDX.Direct2D1.Bitmap bmp = CreateBitmap(pixels, dstSize.Width, dstSize.Height);
            if (bmp != null) {
                Draw(bmp, clientSize);
                bmp.Dispose();
            }
            working = false;
        }

        private void InitializeDirect2D() {
            using (var factory = new SharpDX.Direct2D1.Factory()) {
                using (var surface = texture.QueryInterface<Surface>()) {
                    renderTarget = new RenderTarget(factory, surface, new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)));
                }
            }
            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;
        }

        private SharpDX.Direct2D1.Bitmap CreateBitmap(byte[] bytes, int width, int height) {
            using (DataStream ds = new DataStream(bytes.Length, true, true)) {
                ds.WriteRange(bytes);
                ds.Position = 0;

                var size = new Size2(width, height);
                var bitmapProperties = new BitmapProperties(new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied));
                if (!renderTarget.IsDisposed)
                    return new SharpDX.Direct2D1.Bitmap(renderTarget, size, ds, width * 4, bitmapProperties);
            }
            return null;
        }

        public void Draw(SharpDX.Direct2D1.Bitmap image, Size clientSize) {
            renderTarget?.BeginDraw();
            renderTarget?.Clear(SharpDX.Color.FromBgra(0xff333333));

            float left = (clientSize.Width - image.Size.Width) / 2.0f;
            float top = (clientSize.Height - image.Size.Height) / 2.0f;
            float right = image.Size.Width + left;
            float bottom = image.Size.Height + top;
            RawRectangleF rect = new RawRectangleF(left, top, right, bottom);
            renderTarget.DrawBitmap(image, rect, 1.0f, BitmapInterpolationMode.Linear);

            renderTarget?.EndDraw();
            swapChain?.Present(0, PresentFlags.None);
        }

        private unsafe byte[] ScalingImageMemory(System.Drawing.Bitmap source, Size dstSize) {

            byte[] memory = new byte[dstSize.Width * dstSize.Height * 4];
            BitmapData bmpData = source.LockBits(new System.Drawing.Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

            IntPtr src_ptr = bmpData.Scan0;
            IntPtr dst_ptr;
            fixed (byte* d_ptr = memory)
                dst_ptr = (IntPtr)d_ptr;

            source.UnlockBits(bmpData);

            using (System.Drawing.Bitmap dst_bmp = new System.Drawing.Bitmap(dstSize.Width, dstSize.Height, dstSize.Width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, dst_ptr)) {
                using (Graphics g = Graphics.FromImage(dst_bmp)) {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                    g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                    g.DrawImage(source, 0, 0, dstSize.Width, dstSize.Height);
                }
            }
            return memory;
        }
    }
}