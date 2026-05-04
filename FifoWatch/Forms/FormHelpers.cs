using System.Drawing;
using System.Windows.Forms;

namespace FifoWatch.Forms
{
    internal static class FormHelpers
    {
        internal static void AddSelectionRow(
            Panel container,
            Label lbl,       string labelText,
            TextBox txt,
            Button btnBrowse, Button btnClear,
            int row,
            System.EventHandler browseHandler,
            System.EventHandler clearHandler)
        {
            const int rowH = 30;
            const int top  = 22;
            int y = top + row * rowH;

            lbl.Text     = labelText;
            lbl.AutoSize = true;
            lbl.Location = new Point(8, y + 4);
            lbl.Width    = 130;

            txt.Location  = new Point(144, y);
            txt.Width     = 370;
            txt.ReadOnly  = true;
            txt.BackColor = SystemColors.Window;

            btnBrowse.Text     = "Browse...";
            btnBrowse.Width    = 75;
            btnBrowse.Height   = 22;
            btnBrowse.Location = new Point(520, y);
            btnBrowse.Click   += browseHandler;

            btnClear.Text     = "Clear";
            btnClear.Width    = 50;
            btnClear.Height   = 22;
            btnClear.Location = new Point(601, y);
            btnClear.Click   += clearHandler;

            container.Controls.AddRange(new Control[] { lbl, txt, btnBrowse, btnClear });
        }
    }
}
