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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;


namespace IPAddressControlLib
{
	[DesignerAttribute( typeof(IPAddressControlDesigner) )]
	public class IPAddressControl : System.Windows.Forms.Control
	{
      public const int FieldCount = 4;

      #region Private Data

      private FieldControl[] _fieldControls = new FieldControl[FieldCount];
      private DotControl[] _dotControls = new DotControl[FieldCount-1];

      private bool _autoHeight = true;
      private BorderStyle _borderStyle = BorderStyle.Fixed3D;
      private bool _focused;
      private bool _readOnly;

      private bool _hasMouse;

      #endregion

      #region Public Events

      public event FieldChangedEventHandler FieldChangedEvent;

      #endregion

      #region Public Properies

      [Browsable(true)]
      public bool AllowInternalTab
      {
         get
         {
            foreach ( FieldControl fc in _fieldControls )
            {
               return fc.TabStop;
            }

            return false;
         }
         set
         {
            foreach ( FieldControl fc in _fieldControls )
            {
               fc.TabStop = value;
            }
         }
      }

      [Browsable(true)]
      public bool AnyBlank
      {
         get
         {
            foreach ( FieldControl fc in _fieldControls )
            {
               if ( fc.Blank )
               {
                  return true;
               }
            }

            return false;
         }
      }

      [Browsable(true)]
      public bool AutoHeight
      {
         get 
         {
            return _autoHeight;
         }
         set
         {
            _autoHeight = value;
            if ( _autoHeight )
            {
               AdjustSize();
            }
         }
      }

      [Browsable(true)]
      public bool Blank
      {
         get
         {
            foreach ( FieldControl fc in _fieldControls )
            {
               if ( !fc.Blank )
               {
                  return false;
               }
            }

            return true;
         }
      }

      [Browsable(true)]
      public BorderStyle BorderStyle
      {
         get 
         {
            return _borderStyle;
         }
         set
         {
            _borderStyle = value;
            foreach ( DotControl dc in _dotControls )
            {
               dc.IgnoreTheme = ( value != BorderStyle.Fixed3D );
            }
            LayoutControls();
            Invalidate();
         }
      }

      [Browsable(false)]
      public override bool Focused
      {
         get
         {
            foreach ( FieldControl fc in _fieldControls )
            {
               if ( fc.Focused )
               {
                  return true;
               }
            }
            
            return false;
         }
      }

      [Browsable(true)]
      public bool ReadOnly
      {
         get
         {
            return _readOnly;
         }
         set
         {
            _readOnly = value;

            foreach ( FieldControl fc in _fieldControls )
            {
               fc.ReadOnly = _readOnly;
            }

            foreach ( DotControl dc in _dotControls )
            {
               dc.ReadOnly = _readOnly;
            }

            Invalidate();
         }
      }

      [Bindable(true),Browsable(true)]
      [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
      public override string Text
      {
         get
         {
            StringBuilder sb = new StringBuilder();;

            try
            {
               for ( int index = 0; index < _fieldControls.Length; ++index )
               {
                  sb.Append( _fieldControls[index].Text );
                  
                  if ( index < _dotControls.Length )
                  {
                     sb.Append( _dotControls[index].Text );
                  }
               }
            }
            catch ( ArgumentOutOfRangeException )
            {
            }

            return sb.ToString();
         }
         set
         {
            Parse( value );
         }
      }

      #endregion

      #region Public Functions

      public void Clear()
      {
         foreach( FieldControl fc in _fieldControls )
         {
            fc.Clear();
         }
      }

      public byte[] GetAddressBytes()
      {
         Byte[] bytes = new Byte[_fieldControls.Length];

         for ( int index = 0; index < bytes.Length; ++index )
         {
            if ( _fieldControls[index].TextLength > 0 )
            {
               bytes[index] = Convert.ToByte( _fieldControls[index].Text, CultureInfo.InvariantCulture );
            }
            else
            {
               bytes[index] = (byte)(_fieldControls[index].RangeLower);
            }
         }

         return bytes;
      }

      public void SetAddressBytes( Byte[] bytes )
      {
         Clear();

         if ( bytes == null )
         {
            return;
         }

         int length = Math.Min( _fieldControls.Length, bytes.Length );

         for ( int i = 0; i < length; ++i )
         {
            _fieldControls[i].Text = bytes[i].ToString( CultureInfo.InvariantCulture );
         }
      }

      public void SetFieldFocus( int field )
      {
         if ( ( field >= 0 ) && ( field < _fieldControls.Length ) )
         {
            _fieldControls[field].TakeFocus( Direction.Forward, Selection.All );
         }
      }

      public void SetFieldRange( int field, int rangeLower, int rangeUpper )
      {
         if ( ( field >= 0 ) && ( field < _fieldControls.Length ) )
         {
            _fieldControls[field].RangeLower = rangeLower;
            _fieldControls[field].RangeUpper = rangeUpper;
         }
      }

      public override string ToString()
      {
         StringBuilder sb = new StringBuilder();

         try
         {
            for ( int index = 0; index < _fieldControls.Length; ++index )
            {
               sb.Append( _fieldControls[index].ToString() );
                  
               if ( index < _dotControls.Length )
               {
                  sb.Append( _dotControls[index].ToString() );
               }
            }
         }
         catch ( ArgumentOutOfRangeException )
         {
         }

         return sb.ToString();
      }

      #endregion

      #region Constructors

		public IPAddressControl()
		{
         BackColor = Color.FromKnownColor( KnownColor.Window );

			for ( int index = 0; index < _fieldControls.Length; ++index )
         {
            _fieldControls[index] = new FieldControl();

            _fieldControls[index].CreateControl();

            _fieldControls[index].Name = "fieldControl" + index.ToString( CultureInfo.InvariantCulture );
            _fieldControls[index].Parent = this;
            _fieldControls[index].FieldIndex = index;

            _fieldControls[index].CedeFocusEvent += new CedeFocusHandler( OnCedeFocus );
            _fieldControls[index].Click += new EventHandler( OnSubControlClicked );
            _fieldControls[index].DoubleClick += new EventHandler( OnSubControlDoubleClicked );
            _fieldControls[index].GotFocus += new EventHandler( OnFieldGotFocus );
            _fieldControls[index].KeyDown += new KeyEventHandler( OnFieldKeyDown );
            _fieldControls[index].KeyPress += new KeyPressEventHandler( OnFieldKeyPressed );
            _fieldControls[index].KeyUp += new KeyEventHandler( OnFieldKeyUp );
            _fieldControls[index].LostFocus += new EventHandler( OnFieldLostFocus );
            _fieldControls[index].MouseEnter += new EventHandler( OnSubControlMouseEntered );
            _fieldControls[index].MouseHover += new EventHandler( OnSubControlMouseHovered );
            _fieldControls[index].MouseLeave += new EventHandler( OnSubControlMouseLeft );
            _fieldControls[index].MouseMove += new MouseEventHandler( OnSubControlMouseMoved );
            _fieldControls[index].TextChangedEvent += new TextChangedHandler( OnFieldTextChanged );

            Controls.Add( _fieldControls[index] );
         }
         
         for ( int index = 0; index < _dotControls.Length; ++index )
         {
            _dotControls[index] = new DotControl();

            _dotControls[index].CreateControl();

            _dotControls[index].Name = "dotControl" + index.ToString( CultureInfo.InvariantCulture );
            _dotControls[index].Parent = this;
            _dotControls[index].IgnoreTheme = ( BorderStyle != BorderStyle.Fixed3D );

            _dotControls[index].Click += new EventHandler( OnSubControlClicked );
            _dotControls[index].DoubleClick += new EventHandler( OnSubControlDoubleClicked );
            _dotControls[index].MouseEnter += new EventHandler( OnSubControlMouseEntered );
            _dotControls[index].MouseHover += new EventHandler( OnSubControlMouseHovered );
            _dotControls[index].MouseLeave += new EventHandler( OnSubControlMouseLeft );
            _dotControls[index].MouseMove += new MouseEventHandler( OnSubControlMouseMoved );

            Controls.Add( _dotControls[index] );
         }

         Cursor = Cursors.IBeam;
         Size = MinimumSize;

         SetStyle( ControlStyles.AllPaintingInWmPaint, true );
         SetStyle( ControlStyles.ContainerControl, true );
         SetStyle( ControlStyles.DoubleBuffer, true );
         SetStyle( ControlStyles.ResizeRedraw, true );
         SetStyle( ControlStyles.UserPaint, true );

         LayoutControls();
		}

      #endregion

      #region Protected Overrides

      protected override void OnBackColorChanged( EventArgs e )
      {
         foreach ( FieldControl fc in _fieldControls )
         {
            if ( fc != null )
            {
               fc.BackColor = BackColor;
            }
         }

         foreach ( DotControl dc in _dotControls )
         {
            if ( dc != null )
            {
               dc.BackColor = BackColor;
               dc.Invalidate();
            }
         }

         base.OnBackColorChanged( e );

         Invalidate();
      }

      protected override void OnFontChanged(EventArgs e)
      {
         foreach ( FieldControl fc in _fieldControls )
         {
            fc.SetFont( Font );
         }

         foreach ( DotControl dc in _dotControls )
         {
            dc.SetFont( Font );
         }

         AdjustSize();
         LayoutControls();

         base.OnFontChanged( e );

         Invalidate();
      }

      protected override void OnForeColorChanged( EventArgs e )
      {
         foreach ( FieldControl fc in _fieldControls )
         {
            fc.ForeColor = ForeColor;
         }

         foreach ( DotControl dc in _dotControls )
         {
            dc.ForeColor = ForeColor;
         }

         base.OnForeColorChanged( e );

         Invalidate( true );
      }

      protected override void OnGotFocus( EventArgs e )
      {
         base.OnGotFocus( e );
         _focused = true;
         _fieldControls[0].TakeFocus( Direction.Forward, Selection.All );
      }

      protected override void OnLostFocus( EventArgs e )
      {
         if ( !Focused )
         {
            _focused = false;
            base.OnLostFocus( e );
         }
      }

      protected override void OnMouseEnter( EventArgs e )
      {
         if ( !_hasMouse )
         {
            _hasMouse = true;
            base.OnMouseEnter( e );
         }
      }

      protected override void OnMouseLeave( EventArgs e )
      {
         if ( !HasMouse )
         {
            base.OnMouseLeave( e );
            _hasMouse = false;
         }
      }

      protected override void OnPaint( PaintEventArgs e )
      {
         bool themed = NativeMethods.IsThemed();

         if ( DesignMode || !themed || ( themed && BorderStyle != BorderStyle.Fixed3D ) )
         {
            OnPaintStandard( e );
         }
         else
         {
            OnPaintThemed( e );
         }

         base.OnPaint( e );
      }

      protected override void OnSizeChanged( EventArgs e )
      {
         LayoutControls();
         base.OnSizeChanged( e );
      }

      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)] 
      protected override void WndProc( ref Message m )
      {
         switch ( m.Msg )
         {
            case NativeMethods.WM_WINDOWPOSCHANGING:
               
               NativeMethods.WINDOWPOS lParam = (NativeMethods.WINDOWPOS)m.GetLParam( typeof( NativeMethods.WINDOWPOS ) );

               if ( lParam.cx < MinimumSize.Width )
               {
                  lParam.flags |= NativeMethods.SWP_NOMOVE;
                  lParam.cx = MinimumSize.Width;
               }

               if ( lParam.cy < MinimumSize.Height )
               {
                  lParam.flags |= NativeMethods.SWP_NOMOVE;
                  lParam.cy = MinimumSize.Height;
               }

               if ( AutoHeight && lParam.cy != MinimumSize.Height )
               {
                  lParam.flags |= NativeMethods.SWP_NOMOVE;
                  lParam.cy = MinimumSize.Height;
               }
                 
               Marshal.StructureToPtr( lParam, m.LParam, true );

               break;
         }

         base.WndProc( ref m );
      }

      #endregion

      #region Private Properties

      private bool HasMouse
      {
         get
         {
            return DisplayRectangle.Contains( PointToClient( MousePosition ) );
         }
      }

      private Size MinimumSize
      {
         get
         {
            Size retVal = new Size( 0, 0 );

            foreach ( FieldControl fc in _fieldControls )
            {
               retVal.Width += fc.Width;
               retVal.Height = Math.Max( retVal.Height, fc.Height );
            }

            foreach ( DotControl dc in _dotControls )
            {
               retVal.Width += dc.Width;
               retVal.Height = Math.Max( retVal.Height, dc.Height );
            }

            switch ( BorderStyle )
            {
               case BorderStyle.Fixed3D:
                  retVal.Width  += 6;
                  retVal.Height += 7;
                  break;
               case BorderStyle.FixedSingle:
                  retVal.Width  += 4;
                  retVal.Height += 7;
                  break;
            }

            return retVal;
         }
      }

      #endregion

      #region Private Functions

      private void AdjustSize()
      {
         Size newSize = MinimumSize;

         if ( Width > newSize.Width )
         {
            newSize = new Size( Width, newSize.Height );
         }

         if ( Height > newSize.Height )
         {
            newSize = new Size( Height, newSize.Width );
         }

         if ( AutoHeight )
         {
            Size = new Size( newSize.Width, MinimumSize.Height );
         }
         else
         {
            Size = newSize;
         }
      }

      private void LayoutControls()
      {
         SuspendLayout();

         int difference = Width - MinimumSize.Width;

         Debug.Assert( difference >= 0 );

         int numOffsets = _fieldControls.Length + _dotControls.Length + 1;

         int div = difference / ( numOffsets );
         int mod = difference % ( numOffsets );

         int[] offsets = new int[numOffsets];

         for ( int index = 0; index < numOffsets; ++index )
         {
            offsets[index] = div;

            if ( index < mod )
            {
               ++offsets[index];
            }
         }

         int x = 0;
         int y = 0;

         switch ( BorderStyle )
         {
            case BorderStyle.Fixed3D:
               x = 3;
               y = 3;
               break;
            case BorderStyle.FixedSingle:
               x = 2;
               y = 2;
               break;
         }

         int offsetIndex = 0;

         x += offsets[offsetIndex++];

         for ( int i = 0; i < _fieldControls.Length; ++i )
         {
            _fieldControls[i].Location = new Point( x, y );

            x += _fieldControls[i].Width;      

            if ( i < _dotControls.Length )
            {
               x += offsets[offsetIndex++];
               _dotControls[i].Location = new Point( x, y );
               x += _dotControls[i].Width;
               x += offsets[offsetIndex++];
            }
         }

         ResumeLayout( false );
      }

      private void OnCedeFocus( int fieldIndex, Direction direction, Selection selection, Action action )
      {
         switch ( action )
         {
            case Action.Home:
               _fieldControls[0].TakeFocus( Action.Home );
               return;

            case Action.End:
               _fieldControls[FieldCount-1].TakeFocus( Action.End );
               return;

            case Action.Trim:
               if ( fieldIndex == 0 )
               {
                  return;
               }
               _fieldControls[fieldIndex-1].TakeFocus( Action.Trim );
               return;
         }

         if ( ( direction == Direction.Reverse && fieldIndex == 0 ) ||
              ( direction == Direction.Forward && fieldIndex == ( FieldCount - 1 ) ) )
         {
            return;
         }

         if ( direction == Direction.Forward )
         {
            ++fieldIndex;
         }
         else
         {
            --fieldIndex;
         }

         _fieldControls[fieldIndex].TakeFocus( direction, selection );
      }

      private void OnFieldGotFocus( object sender, EventArgs e )
      {
         if ( !_focused )
         {
            _focused = true;
            base.OnGotFocus( e );
         }
      }

      private void OnFieldLostFocus( object sender, EventArgs e )
      {
         if ( !Focused )
         {
            base.OnLostFocus( e );
            _focused = false;
         }
      }

      private void OnFieldKeyDown( object sender, KeyEventArgs e )
      {
         OnKeyDown( e );
      }

      private void OnFieldKeyPressed( object sender, KeyPressEventArgs e )
      {
         OnKeyPress( e );
      }

      private void OnFieldKeyUp( object sender, KeyEventArgs e )
      {
         OnKeyUp( e );
      }

      private void OnFieldTextChanged( int fieldIndex, string text )
      {
         if ( FieldChangedEvent != null )
         {
            FieldChangedEventArgs args = new FieldChangedEventArgs();
            args.FieldIndex = fieldIndex;
            args.Text = text;
            FieldChangedEvent( this, args );
         }

         OnTextChanged( EventArgs.Empty );
      }

      private void OnPaintStandard( PaintEventArgs e )
      {
         SolidBrush ctrlBrush = null;

         if ( Enabled )
         {
            if ( ReadOnly )
            {
               if ( BackColor.ToKnownColor() == KnownColor.Window )
               {
                  ctrlBrush = new SolidBrush( Color.FromKnownColor( KnownColor.Control ) );
               }
               else
               {
                  ctrlBrush = new SolidBrush( BackColor );
               }
            }
            else
            {
               ctrlBrush = new SolidBrush( BackColor );
            }
         }
         else
         {
            if ( BackColor.ToKnownColor() == KnownColor.Window )
            {
               ctrlBrush = new SolidBrush( Color.FromKnownColor( KnownColor.Control ) );
            }
            else
            {
               ctrlBrush = new SolidBrush( BackColor );
            }
         }

         using ( ctrlBrush )
         {
            e.Graphics.FillRectangle( ctrlBrush, ClientRectangle );
         }

         switch ( BorderStyle )
         {
            case BorderStyle.Fixed3D:
               ControlPaint.DrawBorder3D( e.Graphics, ClientRectangle, Border3DStyle.Sunken );
               break;
            case BorderStyle.FixedSingle:
               ControlPaint.DrawBorder( e.Graphics, ClientRectangle,
                  Color.FromKnownColor( KnownColor.WindowFrame ), ButtonBorderStyle.Solid );
               break;
         }
      }

      private void OnPaintThemed( PaintEventArgs e )
      {
         NativeMethods.RECT rect = new NativeMethods.RECT();

         rect.left   = ClientRectangle.Left;
         rect.top    = ClientRectangle.Top;
         rect.right  = ClientRectangle.Right;
         rect.bottom = ClientRectangle.Bottom;

         IntPtr hdc = new IntPtr();
         hdc = e.Graphics.GetHdc();

         if ( BackColor.ToKnownColor() != KnownColor.Window )
         {
            e.Graphics.ReleaseHdc( hdc );

            using ( SolidBrush backgroundBrush = new SolidBrush( BackColor ) )
            {
               e.Graphics.FillRectangle( backgroundBrush, ClientRectangle );
            }

            hdc = e.Graphics.GetHdc();

            IntPtr hTheme = NativeMethods.OpenThemeData( Handle, "Edit" );

            NativeMethods.DTBGOPTS options = new NativeMethods.DTBGOPTS();
            options.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(options);
            options.dwFlags = NativeMethods.DTBG_OMITCONTENT;
            
            int state = NativeMethods.ETS_NORMAL;

            if ( !Enabled )
            {
               state = NativeMethods.ETS_DISABLED;
            }
            else
               if ( ReadOnly )
            {
               state = NativeMethods.ETS_READONLY;
            }

            NativeMethods.DrawThemeBackgroundEx( hTheme, hdc,
               NativeMethods.EP_EDITTEXT, state, ref rect, ref options );

            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
            }
         }
         else if ( Enabled & !ReadOnly )
         {
            IntPtr hTheme = NativeMethods.OpenThemeData( Handle, "Edit" );

            NativeMethods.DrawThemeBackground( hTheme, hdc, NativeMethods.EP_EDITTEXT,
               NativeMethods.ETS_NORMAL, ref rect, IntPtr.Zero );

            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
            }
         }
         else
         {
            IntPtr hTheme = NativeMethods.OpenThemeData( Handle, "Globals" );

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

            hTheme = NativeMethods.OpenThemeData( Handle, "Edit" );

            NativeMethods.DTBGOPTS options = new NativeMethods.DTBGOPTS();
            options.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(options);
            options.dwFlags = NativeMethods.DTBG_OMITCONTENT;
            
            NativeMethods.DrawThemeBackgroundEx( hTheme, hdc,
               NativeMethods.EP_EDITTEXT, NativeMethods.ETS_DISABLED, ref rect, ref options );

            if ( IntPtr.Zero != hTheme )
            {
               NativeMethods.CloseThemeData( hTheme );
            }
         }
         
         e.Graphics.ReleaseHdc( hdc );
      }

      private void Parse( string text )
      {
         Clear();

         if ( null == text )
         {
            return;
         }

         int textIndex = 0;

         int index = 0;

         for ( index = 0; index < _dotControls.Length; ++index )
         {
            int findIndex = text.IndexOf( _dotControls[index].Text, textIndex );

            if ( findIndex >= 0 )
            {
               _fieldControls[index].Text = text.Substring( textIndex, findIndex - textIndex );
               textIndex = findIndex + _dotControls[index].Text.Length;
            }
            else
            {
               break;
            }
         }

         _fieldControls[index].Text = text.Substring( textIndex );
      }

      private void OnSubControlClicked( object sender, EventArgs e )
      {
         OnClick( e );
      }

      private void OnSubControlDoubleClicked( object sender, EventArgs e )
      {
         OnDoubleClick( e );
      }

      private void OnSubControlMouseEntered( object sender, EventArgs e )
      {
         OnMouseEnter( e );
      }

      private void OnSubControlMouseHovered( object sender, EventArgs e )
      {
         OnMouseHover( e );
      }

      private void OnSubControlMouseLeft( object sender, EventArgs e )
      {
         OnMouseLeave( e );
      }

      private void OnSubControlMouseMoved( object sender, MouseEventArgs e )
      {
         OnMouseMove( e );
      }

      #endregion
   }
}
