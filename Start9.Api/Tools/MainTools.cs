﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Start9.Api.Plex;
using Point = System.Drawing.Point;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;
using BitmapCacheOption = System.Windows.Media.Imaging.BitmapCacheOption;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace Start9.Api.Tools
{
	public static class MainTools
	{
        public static PlexWindow SettingsWindow;

		public static void ShowSettings()
		{
			SettingsWindow.Show();
			if (!SettingsWindow.IsActive)
				SettingsWindow.Focus();
		}
		
		static float GetScalingFactor()
		{
			var g = Graphics.FromHwnd(IntPtr.Zero);
			return g.DpiY / 96;
		}


		public static Point GetDpiScaledCursorPosition()
		{
			var p = Cursor.Position;
			p.X = DpiManager.ConvertPixelsToWpfUnits(p.X);
			p.Y = DpiManager.ConvertPixelsToWpfUnits(p.Y);
			return p;
		}

		public static Point GetDpiScaledGlobalControlPosition(UIElement uiElement)
		{
			var uiPoint = uiElement.PointToScreen(new System.Windows.Point(0, 0));

			var uiDrawPoint = new Point(DpiManager.ConvertPixelsToWpfUnits(uiPoint.X),
										DpiManager.ConvertPixelsToWpfUnits(uiPoint.Y));
			return uiDrawPoint;
		}

		public enum DeviceCap
		{
			Vertres = 10,
			Desktopvertres = 117,
			Logpixelsy = 90
		}
	}

    public static class MiscTools
    {
        public static Color GetColorFromImage(string sourceImagePath)
        {
            var outputColor = Color.Gray;
            var sourceBitmap = new Bitmap(1, 1);

            if (sourceImagePath == Environment.ExpandEnvironmentVariables(@"%windir%\Explorer.exe"))
            {
                outputColor = Color.FromArgb(255, 0, 130, 153);
            }
            else if (File.Exists(sourceImagePath) || Directory.Exists(sourceImagePath))
            {
                if (File.Exists(sourceImagePath))
                {
                    sourceBitmap = Bitmap.FromHicon(Icon.ExtractAssociatedIcon(sourceImagePath).Handle);
                }
                else if (Directory.Exists(sourceImagePath))
                {
                    var shinfo = new WinApi.ShFileInfo();
                    WinApi.SHGetFileInfo(sourceImagePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), 256);
                    sourceBitmap = Bitmap.FromHicon(shinfo.hIcon);
                }

                Color destColor;
                using (var destBitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(destBitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(sourceBitmap, 0, 0, 1, 1);

                    destColor = destBitmap.GetPixel(0, 0);
                }
                outputColor = Color.FromArgb(255, destColor.R, destColor.G, destColor.B);

            }

            return outputColor;
        }

        public static BitmapSource GetBitmapSourceFromSysDrawingBitmap(System.Drawing.Bitmap SourceSysDrawingBitmap)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            SourceSysDrawingBitmap.GetHbitmap(),
            IntPtr.Zero,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(SourceSysDrawingBitmap.Width, SourceSysDrawingBitmap.Height));
        }

        public static BitmapImage GetBitmapImageFromBitmapSource(BitmapSource bitmapSource)
        {
            //from https://stackoverflow.com/questions/5338253/bitmapsource-to-bitmapimage
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bImg.BeginInit();
            bImg.StreamSource = memoryStream;
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }

        public static BitmapImage GetBitmapImageFromSysDrawingBitmap(System.Drawing.Bitmap SourceSysDrawingBitmap)
        {
            MemoryStream memory = new MemoryStream()
            {
                Position = 0
            };
            SourceSysDrawingBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage bitmap = new BitmapImage()
            {
                StreamSource = memory,
                CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.Default
            };
            bitmap.BeginInit();
            bitmap.EndInit();
            return bitmap;
        }

        public static Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream()
            {
                Position = 0
            })
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage.CloneCurrentValue()));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
            /*try
            {
                return new Bitmap(bitmapImage.StreamSource);
            }
            catch
            {
                return (Bitmap)(Bitmap.FromFile(bitmapImage.UriSource.AbsolutePath));
            }*/
        }

        public static System.Drawing.Bitmap GetSysDrawingBitmapFromImageSource(System.Windows.Media.ImageSource src)
        {
            //https://stackoverflow.com/questions/32073767/convert-system-windows-media-imagesource-to-system-drawing-bitmap
            BitmapImage source = src as BitmapImage;
            int width = (int)src.Width;
            int height = (int)src.Height;
            int stride = width * ((source.Format.BitsPerPixel + 7) / 8);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(height * stride);
                source.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using (var bmp = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format1bppIndexed, ptr))
                {
                    return new System.Drawing.Bitmap(bmp);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public static System.Windows.Media.ImageSource GetIconFromFilePath(string path)
        {
            WinApi.ShFileInfo shInfo = new WinApi.ShFileInfo();
            WinApi.SHGetFileInfo(path, 0, ref shInfo, (uint)Marshal.SizeOf(shInfo), 0x000000001 | 0x100);
            System.Drawing.Icon entryIcon = System.Drawing.Icon.FromHandle(shInfo.hIcon);
            System.Windows.Media.ImageSource entryIconImageSource = Imaging.CreateBitmapSourceFromHIcon(
            entryIcon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(Convert.ToInt32(DpiManager.ConvertPixelsToWpfUnits(16)), Convert.ToInt32(Convert.ToInt32(DpiManager.ConvertPixelsToWpfUnits(16))))
            );
            return entryIconImageSource;
        }
    }
}