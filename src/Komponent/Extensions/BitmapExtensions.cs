using System;
using System.Drawing;

namespace Komponent.Extensions
{
    public static class BitmapExtensions
    {
        public static void PutChannel(this Bitmap bitmap, Bitmap channel)
        {
            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var chnData = channel.LockBits(new Rectangle(0, 0, channel.Width, channel.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, channel.PixelFormat);

            IntPtr ptrBmp = bmpData.Scan0;
            IntPtr ptrChn = chnData.Scan0;

            int bytesOfBmp = Math.Abs(bmpData.Stride) * bitmap.Height;
            int bytesOfChn = Math.Abs(chnData.Stride) * channel.Height;
            byte[] bmpPixels = new byte[bytesOfBmp];
            byte[] chnPixels = new byte[bytesOfChn];

            System.Runtime.InteropServices.Marshal.Copy(ptrBmp, bmpPixels, 0, bytesOfBmp);
            System.Runtime.InteropServices.Marshal.Copy(ptrChn, chnPixels, 0, bytesOfChn);

            for (int y = 0; y < channel.Height && y < bitmap.Height; y++)
            {
                for (int x = 0; x < channel.Width && x < bitmap.Width; x++)
                {
                    int bmpPixelIndex = ((y * bitmap.Width) + x) * 4;
                    byte bA = bmpPixels[bmpPixelIndex + 0];
                    byte bR = bmpPixels[bmpPixelIndex + 1];
                    byte bG = bmpPixels[bmpPixelIndex + 2];
                    byte bB = bmpPixels[bmpPixelIndex + 3];

                    int chnPixelIndex = ((y * channel.Width) + x) * 4;
                    byte cA = chnPixels[chnPixelIndex + 0];
                    byte cR = chnPixels[chnPixelIndex + 1];
                    byte cG = chnPixels[chnPixelIndex + 2];
                    byte cB = chnPixels[chnPixelIndex + 3];

                    bmpPixels[bmpPixelIndex + 0] = (byte)(bA | cA);
                    bmpPixels[bmpPixelIndex + 1] = (byte)(bR | cR);
                    bmpPixels[bmpPixelIndex + 2] = (byte)(bG | cG);
                    bmpPixels[bmpPixelIndex + 3] = (byte)(bB | cB);
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bmpPixels, 0, ptrBmp, bytesOfBmp);
            bitmap.UnlockBits(bmpData);
            channel.UnlockBits(chnData);
        }
    }
}
