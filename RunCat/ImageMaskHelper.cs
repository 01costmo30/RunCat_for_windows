using System.Drawing;
using System.Drawing.Imaging;

namespace RunCat;
public static class ImageMaskHelper
{
    /// <summary>
    /// 將黑底白圖的白色區域轉換成指定顏色
    /// 適用於：黑底白圖 (Black Background, White Shape)
    /// </summary>
    public static Bitmap ApplyColorMask(Bitmap img, Color maskColor)
    {
        Bitmap bmp = new Bitmap(img.Width, img.Height);
        using Graphics g = Graphics.FromImage(bmp);
        ColorMatrix colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 }, // Alpha 通道保持不變
            new float[] { maskColor.R / 255f, maskColor.G / 255f, maskColor.B / 255f, 0, 1 }
        });

        ImageAttributes attributes = new ImageAttributes();
        attributes.SetColorMatrix(colorMatrix);

        g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height),
            0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attributes);
        return bmp;
    }

    /// <summary>
    /// 將黑底白圖的黑色變成透明，並填充新顏色
    /// 適用於：黑底白圖 (Black Background, White Shape)
    /// </summary>
    public static Bitmap ReplaceBlackWithColor(Bitmap img, Color newColor)
    {
        Bitmap newBmp = new Bitmap(img);
        newBmp.MakeTransparent(Color.Black); // 讓黑色變透明

        using Graphics g = Graphics.FromImage(newBmp);
        using SolidBrush brush = new SolidBrush(newColor);
        g.FillRectangle(brush, 0, 0, newBmp.Width, newBmp.Height);

        return newBmp;
    }
}