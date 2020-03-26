using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
namespace Finance
{
    #region Snap Form     

    ///// <summary>
    ///// Form which exists as a 'shadow' of parent form and provides edge-snapping properties
    ///// </summary>
    //public class SnapEdgeForm : Form
    //{
    //    /*
    //     *  Borderless, transparent form which tracks behind parent form and draws an edge line when activated
    //     */
    //    private new Form ParentForm { get; }
    //    public static int SnapDistance { get; set; } = 12;

    //    private ControlEdge SnapEdge { get; set; }
    //    private Form SnappedTo { get; set; }
    //    private bool Snapped { get; set; }

    //    public SnapEdgeForm(Form parent)
    //    {
    //        ParentForm = parent ?? throw new ArgumentNullException(nameof(parent));
    //        this.InitializeMe();
    //    }

    //    [Initializer]
    //    private void InitializeStyle()
    //    {
    //        FormBorderStyle = FormBorderStyle.None;
    //        TransparencyKey = SystemColors.Control;
    //        ShowInTaskbar = false;
    //    }
    //    [Initializer]
    //    private void InitializeLocationAndSize()
    //    {
    //        ParentForm.SizeChanged += (s, e) => UpdateSize();
    //        ParentForm.LocationChanged += (s, e) => UpdateLocation();

    //        UpdateSize();
    //        UpdateLocation();
    //    }
    //    [Initializer]
    //    private void InitializePainting()
    //    {
    //        this.Paint += HighlightEdgeForm_Paint;
    //        this.Move += HighlightEdgeForm_Move;
    //    }
    //    [Initializer]
    //    private void InitializeSnapping()
    //    {
    //        this.Move += SnapEdgeForm_Move;
    //    }

    //    private void SnapEdgeForm_Move(object sender, EventArgs e)
    //    {
    //        if (SnapEdge != ControlEdge.None)
    //            Snap();
    //        else
    //            Snapped = false;
    //    }
    //    private ControlEdge InRange(CustomForm other)
    //    {
    //        // Check each edge of this form against corresponding edge of other form to see if it should be snapped

    //        // Check this.Right with other.Left
    //        if (this.EdgesAreClose(ControlEdge.Right, other, SnapDistance))
    //            return ControlEdge.Right;

    //        // Check this.Left with other.Right
    //        if (this.EdgesAreClose(ControlEdge.Left, other, SnapDistance))
    //            return ControlEdge.Left;

    //        // Check this.Top with other.Bottom
    //        if (this.EdgesAreClose(ControlEdge.Top, other, SnapDistance))
    //            return ControlEdge.Top;

    //        // Check this.Bottom with other.Top
    //        if (this.EdgesAreClose(ControlEdge.Bottom, other, SnapDistance))
    //            return ControlEdge.Bottom;

    //        return ControlEdge.None;
    //    }
    //    public void SetHighlightEdge(ControlEdge edge)
    //    {
    //        SnapEdge = edge;
    //        Invalidate();
    //    }
    //    private void HighlightEdgeForm_Move(object sender, EventArgs e)
    //    {
    //        // Check to see if this form is within range of another CustomForm and set highlight edge if so
    //        foreach (Form form in Application.OpenForms)
    //        {
    //            if (form is CustomForm cf)
    //            {
    //                var edge = InRange(cf);
    //                if (edge != ControlEdge.None)
    //                {
    //                    SetHighlightEdge(edge);
    //                    SnappedTo = cf;
    //                    return;
    //                }
    //            }
    //        }

    //        SetHighlightEdge(ControlEdge.None);
    //        SnappedTo = null;
    //    }
    //    private void HighlightEdgeForm_Paint(object sender, PaintEventArgs e)
    //    {
    //        if (SnapEdge == ControlEdge.None)
    //            return;

    //        using (Pen pen = new Pen(Color.Red, 4))
    //        {
    //            switch (SnapEdge)
    //            {
    //                case ControlEdge.Left:
    //                    e.Graphics.DrawLine(pen, 2, 4, 2, this.ClientRectangle.Bottom - 4);
    //                    break;
    //                case ControlEdge.Right:
    //                    e.Graphics.DrawLine(pen, this.ClientRectangle.Right - 2, 4, this.ClientRectangle.Right - 2, this.ClientRectangle.Bottom - 4);
    //                    break;
    //                case ControlEdge.Top:
    //                    e.Graphics.DrawLine(pen, 4, 2, this.ClientRectangle.Right - 4, 2);
    //                    break;
    //                case ControlEdge.Bottom:
    //                    e.Graphics.DrawLine(pen, 4, this.ClientRectangle.Bottom - 2, this.ClientRectangle.Right - 4, this.ClientRectangle.Bottom - 2);
    //                    break;
    //            }
    //        }
    //    }
    //    private void UpdateSize()
    //    {
    //        this.Size = ParentForm.Size;
    //        this.Width -= 8;
    //        this.Height -= 1;
    //    }
    //    private void UpdateLocation()
    //    {
    //        this.Location = new Point(ParentForm.Location.X + 4, ParentForm.Location.Y - 3);
    //        BringToFront();
    //    }
    //    private void Snap()
    //    {
    //        ParentForm.SnapTo(SnappedTo, SnapEdge);
    //        Snapped = true;
    //    }
    //}

    #endregion
};