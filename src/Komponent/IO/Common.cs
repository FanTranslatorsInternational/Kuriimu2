using System.IO;
using System.Windows.Forms;

namespace Komponent.IO
{
    public enum ByteOrder : ushort
    {
        LittleEndian = 0xFEFF,
        BigEndian = 0xFFFE
    }

    public static class Common
    {
        public static bool PrepareFiles(string openCaption, string saveCaption, string saveExtension, out FileStream openFile, out FileStream saveFile, bool isOut = false)
        {
            openFile = null;
            saveFile = null;

            var ofd = new OpenFileDialog
            {
                Title = openCaption,
                Filter = "All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return false;
            openFile = File.OpenRead(ofd.FileName);

            var sfd = new SaveFileDialog()
            {
                Title = saveCaption,
                FileName = !isOut ? Path.GetFileNameWithoutExtension(ofd.FileName) + saveExtension + Path.GetExtension(ofd.FileName) : Path.GetFileName(ofd.FileName.Replace(saveExtension, string.Empty)),
                Filter = "All Files (*.*)|*.*"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                openFile.Dispose();
                return false;
            }
            saveFile = File.Create(sfd.FileName);

            return true;
        }
    }
}