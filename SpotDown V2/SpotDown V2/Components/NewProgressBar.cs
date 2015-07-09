using System;
using System.Drawing;
using System.Windows.Forms;

public enum ProgressBarDisplayText
{
    Percentage,
    CustomText
}

public class NewProgressBar : ProgressBar
{
    /// <summary>
    /// Please give credit to me if you use this or any part of it.
    /// HF: http://www.hackforums.net/member.php?action=profile&uid=1389752
    /// GitHub: https://github.com/DarkN3ss61
    /// Website: http://jlynx.net/
    /// Twitter: https://twitter.com/jLynx_DarkN3ss
    /// </summary>
    //Property to set to decide whether to print a % or Text
    public ProgressBarDisplayText DisplayStyle { get; set; }

    //Property to hold the custom text
    public String CustomText { get; set; }

    public NewProgressBar()
    {
        this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        SolidBrush brush = new SolidBrush(Color.FromArgb(132, 189, 0));
        SolidBrush brush2 = new SolidBrush(Color.FromArgb(62, 62, 64));
        SolidBrush brush3 = new SolidBrush(Color.FromArgb(255, 255, 255));
        Rectangle rec = e.ClipRectangle;
        int width = rec.Width;
        int height = rec.Height;

        rec.Width = (int)(rec.Width * ((double)Value / Maximum));
        if (ProgressBarRenderer.IsSupported)
        {
            ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
        }
        rec.Height = rec.Height - 4;

        e.Graphics.FillRectangle(brush2, 0, 0, width, height);
        e.Graphics.FillRectangle(brush, 0, 0, rec.Width, height);

        // Set the Display text (Either a % amount or our custom text
        string text = DisplayStyle == ProgressBarDisplayText.Percentage ? Value.ToString() + '%' : CustomText;


        using (Font f = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))))
        {

            SizeF len = e.Graphics.MeasureString(text, f);
            // Calculate the location of the text (the middle of progress bar)
            // Point location = new Point(Convert.ToInt32((rect.Width / 2) - (len.Width / 2)), Convert.ToInt32((rect.Height / 2) - (len.Height / 2)));
            Point location = new Point(Convert.ToInt32((Width / 2) - len.Width / 2), Convert.ToInt32((Height / 2) - len.Height / 2));
            // The commented-out code will centre the text into the highlighted area only. This will centre the text regardless of the highlighted area.
            // Draw the custom text
            e.Graphics.DrawString(text, f, brush3, location);
        }

        // Clean up.
        brush.Dispose();
        brush2.Dispose();
        brush3.Dispose();
        e.Graphics.Dispose();
    }
}
