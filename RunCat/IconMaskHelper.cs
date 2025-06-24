using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RunCat;
public static class IconMaskHelper
{
    /// <summary>
    /// 將 Icon 轉為 Bitmap
    /// </summary>
    public static Bitmap IconToBitmap(Icon icon)
    {
        return icon.ToBitmap();
    }

    /// <summary>
    /// 將 Icon 的白色區域轉換成指定顏色，並保持 Icon 格式
    /// </summary>
    public static Icon ApplyColorMaskToIcon(Icon icon, Color maskColor)
    {
        Bitmap bitmap = IconToBitmap(icon);
        Bitmap maskedBitmap = ImageMaskHelper.ApplyColorMask(bitmap, maskColor);

        return SaveBitmapAsIcon(maskedBitmap);
    }

    /// <summary>
    /// 將處理後的 Bitmap 儲存為 Icon
    /// </summary>
    public static Icon SaveBitmapAsIcon(Bitmap bitmap)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bitmap.Save(ms, ImageFormat.Png); // 先存成 PNG (保留透明度)
            using (Bitmap pngBitmap = new Bitmap(ms))
            {
                return Icon.FromHandle(pngBitmap.GetHicon()); // 轉換為 Icon
            }
        }
    }
}