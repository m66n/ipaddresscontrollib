// Copyright (c) 2007 Michael Chapman

// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace IPAddressControlLib
{
	internal class DotControl : System.Windows.Forms.Control
	{
      private bool _readOnly;
      private bool _ignoreTheme;

      public bool IgnoreTheme
      {
         get 
         {
            return _ignoreTheme;
         }
         set 
         {
            _ignoreTheme = value;
         }
      }

      public bool ReadOnly
      {
         get
         {
            return _readOnly;
         }
         set
         {
            _readOnly = value;
            Invalidate();
         }
      }

      public void SetFont( Font font )
      {
         this.Font = font;
         this.Size = CalculateControlSize();
      }

      public override string ToString()
      {
         return this.Text;
      }

		public DotControl()
		{
         this.Font    = Control.DefaultFont;
         this.TabStop = false;

         ResourceManager rm = new ResourceManager( "IPAddressControlLib.Strings", Assembly.GetExecutingAssembly() );
         this.Text = rm.GetString( "FieldSeparator" );

         this.Size = CalculateControlSize();

         SetStyle( ControlStyles.AllPaintingInWmPaint, true );
         SetStyle( ControlStyles.DoubleBuffer, true );
         SetStyle( ControlStyles.ResizeRedraw, true );
         SetStyle( ControlStyles.UserPaint, true );
		}

      protected override void OnFontChanged(EventArgs e)
      {
         this.Size = CalculateControlSize();
         Invalidate();
      }

      private Size CalculateControlSize()
      {
         return Utility.CalculateStringSize( this.Handle, this.Font, this.Text );
      }

      protected void OnPaintStandard( PaintEventArgs e )
      {
         SolidBrush ctrlBrush = null;
         SolidBrush textBrush = null;

         if ( this.Enabled )
         {
            if ( this.ReadOnly )
            {
               if ( this.BackColor.ToKnownColor() == KnownColor.Window )
               {
                  ctrlBrush = new SolidBrush( Color.FromKnownColor( KnownColor.Control ) );
                  textBrush = new SolidBrush( Color.FromKnownColor( KnownColor.WindowText ) );
               }
               else
               {
                  ctrlBrush = new SolidBrush( this.BackColor );
                  textBrush = new SolidBrush( this.ForeColor );
               }
            }
            else
            {
               ctrlBrush = new SolidBrush( this.BackColor );
               textBrush = new SolidBrush( this.ForeColor );
            }
         }
         else
         {
            if ( this.BackColor.ToKnownColor() == KnownColor.Window )
            {
               ctrlBrush = new SolidBrush( Color.FromKnownColor( KnownColor.Control ) );
            }
            else
            {
               ctrlBrush = new SolidBrush( this.BackColor );
            }

            if ( this.ForeColor.ToKnownColor() == KnownColor.Control )
            {
               textBrush = new SolidBrush( this.ForeColor );
            }
            else
            {
               textBrush = new SolidBrush( Color.FromKnownColor( KnownColor.GrayText ) );
            }
         }

         using ( ctrlBrush )
         {
            e.Graphics.FillRectangle( ctrlBrush, ClientRectangle );
         }

         StringFormat stringFormat = new StringFormat();
         stringFormat.Alignment = StringAlignment.Center;

         using ( textBrush )
         {
            e.Graphics.DrawString( this.Text, this.Font, textBrush, this.ClientRectangle, stringFormat );
         }
      }

      protected void OnPaintThemed( PaintEventArgs e )
      {
         NativeMethods.RECT rect = new NativeMethods.RECT();

         rect.left   = ClientRectangle.Left;
         rect.top    = ClientRectangle.Top;
         rect.right  = ClientRectangle.Right;
         rect.bottom = ClientRectangle.Bottom;

         IntPtr hdc = new IntPtr();
         hdc = e.Graphics.GetHdc();

         if ( this.BackColor.ToKnownColor() != KnownColor.Window )
         {
            e.Graphics.ReleaseHdc( hdc );

            using ( SolidBrush backgroundBrush = new SolidBrush( BackColor ) )
            {
               e.Graphics.FillRectangle( backgroundBrush, this.ClientRectangle );
            }

            hdc = e.Graphics.GetHdc();
         }
         else
         if ( Enabled & !ReadOnly )
         {
            IntPtr hTheme = NativeMethods.OpenThemeData( this.Handle, "Edit" );

            NativeMethods.DTBGOPTS options = new NativeMethods.DTBGOPTS();
            options.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(options);
            options.dwFlags = NativeMethods.DTBG_OMITBORDER;

            NativeMethods.DrawThemeBackgroundEx( hTheme, hdc,
               NativeMethods.EP_EDITTEXT, NativeMethods.ETS_NORMAL, ref rect, ref options );

            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
            }
         }
         else
         {
            IntPtr hTheme = NativeMethods.OpenThemeData( this.Handle, "Globals" );

            IntPtr hBrush = NativeMethods.GetThemeSysColorBrush( hTheme, 15 );

            NativeMethods.FillRect( hdc, ref rect, hBrush );

            if ( IntPtr.Zero != hBrush )
            {
               NativeMethods.DeleteObject( hBrush );
               hBrush = IntPtr.Zero;
            }

            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
               hTheme = IntPtr.Zero;
            }
         }
         
         e.Graphics.ReleaseHdc( hdc );

         uint colorref = 0;

         if ( Enabled )
         {
            if ( this.ForeColor.ToKnownColor() != KnownColor.WindowText )
            {
               colorref = NativeMethods.RGB( this.ForeColor.R, this.ForeColor.G, this.ForeColor.B );
            }
            else
            {
               IntPtr hTheme = NativeMethods.OpenThemeData( this.Handle, "Globals" );
               colorref = NativeMethods.GetThemeSysColor( hTheme, 6 );
               if ( IntPtr.Zero != hTheme )
               {
                  NativeMethods.CloseThemeData( hTheme );
                  hTheme = IntPtr.Zero;
               }
            }
         }
         else
         {
            IntPtr hTheme = NativeMethods.OpenThemeData( this.Handle, "Globals" );
            colorref = NativeMethods.GetThemeSysColor( hTheme, 16 );
            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
               hTheme = IntPtr.Zero;
            }
         }

         int r = NativeMethods.GetRValue( colorref );
         int g = NativeMethods.GetGValue( colorref );
         int b = NativeMethods.GetBValue( colorref );

         StringFormat stringFormat = new StringFormat();
         stringFormat.Alignment = StringAlignment.Center;

         using ( SolidBrush textBrush = new SolidBrush( Color.FromArgb( r, g, b ) ) )
         {
            e.Graphics.DrawString( this.Text, this.Font, textBrush, this.ClientRectangle, stringFormat );
         }
      }

      protected override void OnPaint( PaintEventArgs e )
      {
         bool themed = NativeMethods.IsThemed();

         if ( DesignMode || !themed || ( themed && IgnoreTheme ) )
         {
            OnPaintStandard( e );
         }
         else
         {
            OnPaintThemed( e );
         }

         base.OnPaint( e );
      }
	}
}
