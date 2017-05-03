
namespace Eclipse
{
    class Clip
    {
        public string ClipData { get; set; }
        public string ProcessName { get; set; }
        public string ProcessIcon { get; set; } // byte array

        //   SAVING ICON
        //       using (System.IO.MemoryStream ms = new System.IO.MemoryStream()) // ambiguity between System.IO and System.Windows.Shapes
        //{
        //    windowIcon.Save(ms);
        //    currentClip.ExecutableIconPath = ms.ToString();
        //}

        // SAVING ICON
        //var base64 = string.Empty;
        //using (MemoryStream ms = new MemoryStream())
        //{
        //yourImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //base64 = Convert.ToBase64String(ms.ToArray());
        //}

        //   RETRIEVING ICON
        //public static Icon BytesToIcon(byte[] bytes)
        //{
        //    using (MemoryStream ms = new MemoryStream(bytes))
        //    {
        //        return new Icon(ms);
        //    }
        //}

        // RETRIEVING ICON
        //byte[] bytes = Convert.FromBase64String(value);
        //Image image;
        //using (MemoryStream ms = new MemoryStream(bytes))
        //{
        //image = Image.FromStream(ms);
        //}
        //return image;
    }
}
