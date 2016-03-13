// Copyright (c) 2007-2012  Michael Chapman
// http://ipaddresscontrollib.googlecode.com

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms; 
using System.Windows.Forms.VisualStyles;


namespace IPAddressControlLib
{
   [DesignerAttribute( typeof( IPAddressControlDesigner ) )]
   public class IPAddressControl : ContainerControl
   {
      #region Public Constants

      public const int FieldCount = 4;

      #endregion // Public Constants

      #region Public Events

      public event EventHandler<FieldChangedEventArgs> FieldChangedEvent;

      #endregion //Public Events

      #region Public Properties

      [Browsable( true )]
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

      [Browsable( true )]
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

      [Browsable( true )]
      public bool AutoHeight
      {
         get { return _autoHeight; }
         set
         {
            _autoHeight= value;

            if ( _autoHeight )
            {
               AdjustSize();
            }
         }
      }

      [Browsable( false )]
      public int Baseline
      {
         get 
         {
            NativeMethods.TEXTMETRIC textMetric = GetTextMetrics( Handle, Font );

            int offset = textMetric.tmAscent + 1;

            switch ( BorderStyle )
            {
               case BorderStyle.Fixed3D:
                  offset += Fixed3DOffset.Height;
                  break;
               case BorderStyle.FixedSingle:
                  offset += FixedSingleOffset.Height;
                  break;
            }

            return offset;
         }
      }

      [Browsable( true )]
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

      [Browsable( true )]
      public BorderStyle BorderStyle
      {
         get { return _borderStyle; }
         set
         {
            _borderStyle = value;
            AdjustSize();
            Invalidate();
         }
      }

      [Browsable( false )]
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

      [Browsable( false ), DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
      public IPAddress IPAddress
      {
         get { return new IPAddress( GetAddressBytes() ); }
         set
         {
            Clear();

            if ( null == value ) { return; }

            if ( value.AddressFamily == AddressFamily.InterNetwork )
            {
               SetAddressBytes( value.GetAddressBytes() );
            }
         }
      }

      [Browsable( true )]
      public override Size MinimumSize
      {
         get { return CalculateMinimumSize(); }
      }

      [Browsable( true )]
      public bool ReadOnly
      {
         get { return _readOnly; }
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

      [Bindable(true)]
      [Browsable(true)]
      [DesignerSerializationVisibility( DesignerSerializationVisibility.Visible )]
      public override string Text
      {
         get
         {
            StringBuilder sb = new StringBuilder(); ;

            for ( int index = 0; index < _fieldControls.Length; ++index )
            {
               sb.Append( _fieldControls[ index ].Text );

               if ( index < _dotControls.Length )
               {
                  sb.Append( _dotControls[ index ].Text );
               }
            }

            return sb.ToString();
         }
         set
         {
            Clear();

            if ( null == value ) { return; }

            List<string> separators = new List<string>();
            foreach ( DotControl dc in _dotControls )
            {
               separators.Add( dc.Text );
            }
               
            string[] fields = Parse( value, separators.ToArray() );

            for ( int i = 0; i < Math.Min( _fieldControls.Length, fields.Length ); ++i )
            {
               _fieldControls[ i ].Text = fields[ i ];
            }
         }
      }

      #endregion // Public Properties

      #region Public Methods

      public void Clear()
      {
         foreach ( FieldControl fc in _fieldControls )
         {
            fc.Clear();
         }
      }

      public byte[] GetAddressBytes()
      {
         byte[] bytes = new byte[FieldCount];

         for ( int index = 0; index < FieldCount; ++index )
         {
            bytes[index] = _fieldControls[index].Value;
         }

         return bytes;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Justification = "Using Bytes seems more informative than SetAddressValues." )]
      public void SetAddressBytes( byte[] bytes )
      {
         Clear();

         if ( bytes == null )
         {
            return;
         }

         int length = Math.Min( FieldCount, bytes.Length );

         for ( int i = 0; i < length; ++i )
         {
            _fieldControls[i].Text = bytes[i].ToString( CultureInfo.InvariantCulture );
         }
      }

      public void SetFieldFocus( int fieldIndex )
      {
         if ( ( fieldIndex >= 0 ) && ( fieldIndex < FieldCount ) )
         {
            _fieldControls[fieldIndex].TakeFocus( Direction.Forward, Selection.All );
         }
      }

      public void SetFieldRange( int fieldIndex, byte rangeLower, byte rangeUpper )
      {
         if ( ( fieldIndex >= 0 ) && ( fieldIndex < FieldCount ) )
         {
            _fieldControls[fieldIndex].RangeLower = rangeLower;
            _fieldControls[fieldIndex].RangeUpper = rangeUpper;
         }
      }

      public override string ToString()
      {
         StringBuilder sb = new StringBuilder();

         for ( int index = 0; index < FieldCount; ++index )
         {
            sb.Append( _fieldControls[index].ToString() );

            if ( index < _dotControls.Length )
            {
               sb.Append( _dotControls[index].ToString() );
            }
         }

         return sb.ToString();
      }

      #endregion Public Methods

      #region Constructors

      public IPAddressControl()
      {
         BackColor = SystemColors.Window;

         ResetBackColorChanged();

         for ( int index = 0; index < _fieldControls.Length; ++index )
         {
            _fieldControls[index] = new FieldControl();

            _fieldControls[index].CreateControl();

            _fieldControls[index].FieldIndex = index;
            _fieldControls[index].Name = "FieldControl" + index.ToString( CultureInfo.InvariantCulture );
            _fieldControls[index].Parent = this;

            _fieldControls[index].CedeFocusEvent += new EventHandler<CedeFocusEventArgs>( OnCedeFocus );
            _fieldControls[index].Click += new EventHandler( OnSubControlClicked );
            _fieldControls[index].DoubleClick += new EventHandler( OnSubControlDoubleClicked );
            _fieldControls[index].GotFocus += new EventHandler( OnFieldGotFocus );
            _fieldControls[index].KeyDown += new KeyEventHandler( OnFieldKeyDown );
            _fieldControls[index].KeyPress += new KeyPressEventHandler( OnFieldKeyPressed );
            _fieldControls[index].KeyUp += new KeyEventHandler( OnFieldKeyUp );
            _fieldControls[index].LostFocus += new EventHandler( OnFieldLostFocus );
            _fieldControls[index].MouseClick += new MouseEventHandler( OnSubControlMouseClicked );
            _fieldControls[index].MouseDoubleClick += new MouseEventHandler( OnSubControlMouseDoubleClicked );
            _fieldControls[index].MouseEnter += new EventHandler( OnSubControlMouseEntered );
            _fieldControls[index].MouseHover += new EventHandler( OnSubControlMouseHovered );
            _fieldControls[index].MouseLeave += new EventHandler( OnSubControlMouseLeft );
            _fieldControls[index].MouseMove += new MouseEventHandler( OnSubControlMouseMoved );
            _fieldControls[index].PasteEvent += new EventHandler<PasteEventArgs>( OnPaste );
            _fieldControls[index].PreviewKeyDown += new PreviewKeyDownEventHandler( OnFieldPreviewKeyDown );
            _fieldControls[index].TextChangedEvent += new EventHandler<TextChangedEventArgs>( OnFieldTextChanged );

            Controls.Add( _fieldControls[index] );

            if ( index < ( FieldCount - 1 ) )
            {
               _dotControls[index] = new DotControl();

               _dotControls[index].CreateControl();

               _dotControls[index].Name = "DotControl" + index.ToString( CultureInfo.InvariantCulture );
               _dotControls[index].Parent = this;

               _dotControls[index].Click += new EventHandler( OnSubControlClicked );
               _dotControls[index].DoubleClick += new EventHandler( OnSubControlDoubleClicked );
               _dotControls[index].MouseClick += new MouseEventHandler( OnSubControlMouseClicked );
               _dotControls[index].MouseDoubleClick += new MouseEventHandler( OnSubControlMouseDoubleClicked );
               _dotControls[index].MouseEnter += new EventHandler( OnSubControlMouseEntered );
               _dotControls[index].MouseHover += new EventHandler( OnSubControlMouseHovered );
               _dotControls[index].MouseLeave += new EventHandler( OnSubControlMouseLeft );
               _dotControls[index].MouseMove += new MouseEventHandler( OnSubControlMouseMoved );

               Controls.Add( _dotControls[index] );
            }
         }

         SetStyle( ControlStyles.AllPaintingInWmPaint, true );
         SetStyle( ControlStyles.ContainerControl, true );
         SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
         SetStyle( ControlStyles.ResizeRedraw, true );
         SetStyle( ControlStyles.UserPaint, true );
         SetStyle( ControlStyles.FixedWidth, true );
         SetStyle( ControlStyles.FixedHeight, true );

         Cursor = Cursors.IBeam;

         AutoScaleDimensions = new SizeF( 96F, 96F );
         AutoScaleMode = AutoScaleMode.Dpi;

         Size = MinimumSize;

         DragEnter += new DragEventHandler( IPAddressControl_DragEnter );
         DragDrop += new DragEventHandler( IPAddressControl_DragDrop );
      }

      #endregion // Constructors

      #region Protected Methods

      protected override void Dispose( bool disposing )
      {
         if ( disposing ) { Cleanup(); }
         base.Dispose( disposing );
      }

      protected override void OnBackColorChanged( EventArgs e )
      {
         base.OnBackColorChanged( e );
         _backColorChanged = true;
      }

      protected override void OnFontChanged( EventArgs e )
      {
         base.OnFontChanged( e );
         AdjustSize();
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
         if ( null == e ) { throw new ArgumentNullException( "e" ); }

         base.OnPaint( e );

         Color backColor = BackColor;

         if ( !_backColorChanged )
         {
            if ( !Enabled || ReadOnly )
            {
               backColor = SystemColors.Control;
            }
         }

         using ( SolidBrush backgroundBrush = new SolidBrush( backColor ) )
         {
            e.Graphics.FillRectangle( backgroundBrush, ClientRectangle );
         }

         Rectangle rectBorder = new Rectangle( ClientRectangle.Left, ClientRectangle.Top,
            ClientRectangle.Width - 1, ClientRectangle.Height - 1 );

         switch ( BorderStyle )
         {
            case BorderStyle.Fixed3D:

               if ( Application.RenderWithVisualStyles )
               {
                  using ( Pen pen = new Pen( VisualStyleInformation.TextControlBorder ) )
                  {
                     e.Graphics.DrawRectangle( pen, rectBorder );
                  }
                  rectBorder.Inflate( -1, -1 );
                  e.Graphics.DrawRectangle( SystemPens.Window, rectBorder );
               }
               else
               {
                  ControlPaint.DrawBorder3D( e.Graphics, ClientRectangle, Border3DStyle.Sunken );
               }
               break;

            case BorderStyle.FixedSingle:

               ControlPaint.DrawBorder( e.Graphics, ClientRectangle,
                  SystemColors.WindowFrame, ButtonBorderStyle.Solid );
               break;
         }
      }

      protected override void OnSizeChanged( EventArgs e )
      {
         base.OnSizeChanged( e );
         AdjustSize();
      }

      #endregion // Protected Methods

      #region Private Properties

      private bool HasMouse
      {
         get
         {
            return DisplayRectangle.Contains( PointToClient( MousePosition ) );
         }
      }

      #endregion  // Private Properties

      #region Private Methods

      private void AdjustSize()
      {
         Size newSize = MinimumSize;

         if ( Width > newSize.Width )
         {
            newSize.Width = Width;
         }

         if ( Height > newSize.Height )
         {
            newSize.Height = Height;
         }

         if ( AutoHeight )
         {
            Size = new Size( newSize.Width, MinimumSize.Height );
         }
         else
         {
            Size = newSize;
         }

         LayoutControls();
      }

      private Size CalculateMinimumSize()
      {
         Size minimumSize = new Size( 0, 0 );

         foreach ( FieldControl fc in _fieldControls )
         {
            minimumSize.Width += fc.Width;
            minimumSize.Height = Math.Max( minimumSize.Height, fc.Height );
         }

         foreach ( DotControl dc in _dotControls )
         {
            minimumSize.Width += dc.Width;
            minimumSize.Height = Math.Max( minimumSize.Height, dc.Height );
         }

         switch ( BorderStyle )
         {
            case BorderStyle.Fixed3D:
               minimumSize.Width += 6;
               minimumSize.Height += ( GetSuggestedHeight() - minimumSize.Height );
               break;
            case BorderStyle.FixedSingle:
               minimumSize.Width += 4;
               minimumSize.Height += ( GetSuggestedHeight() - minimumSize.Height );
               break;
         }

         return minimumSize;
      }

      private void Cleanup()
      {
         foreach ( DotControl dc in _dotControls )
         {
            Controls.Remove( dc );
            dc.Dispose();
         }

         foreach ( FieldControl fc in _fieldControls )
         {
            Controls.Remove( fc );
            fc.Dispose();
         }

         _dotControls = null;
         _fieldControls = null;
      }

      private int GetSuggestedHeight()
      {
         int height = 0;
         using ( TextBox reference = new TextBox() )
         {
            reference.AutoSize = true;
            reference.BorderStyle = BorderStyle;
            reference.Font = Font;
            height = reference.Height;
         }
         return height;
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA1806", Justification = "What should be done if ReleaseDC() doesn't work?" )]
      private static NativeMethods.TEXTMETRIC GetTextMetrics( IntPtr hwnd, Font font )
      {
         IntPtr hdc = NativeMethods.GetWindowDC( hwnd );

         NativeMethods.TEXTMETRIC textMetric;
         IntPtr hFont = font.ToHfont();

         try
         {
            IntPtr hFontPrevious = NativeMethods.SelectObject( hdc, hFont );
            NativeMethods.GetTextMetrics( hdc, out textMetric );
            NativeMethods.SelectObject( hdc, hFontPrevious );
         }
         finally
         {
            NativeMethods.ReleaseDC( hwnd, hdc );
            NativeMethods.DeleteObject( hFont );
         }

         return textMetric;
      }

      private void IPAddressControl_DragDrop( object sender, System.Windows.Forms.DragEventArgs e )
      {
         Text = e.Data.GetData( DataFormats.Text ).ToString();
      }

      private void IPAddressControl_DragEnter( object sender, System.Windows.Forms.DragEventArgs e )
      {
         if ( e.Data.GetDataPresent( DataFormats.Text ) )
         {
            e.Effect = DragDropEffects.Copy;
         }
         else
         {
            e.Effect = DragDropEffects.None;
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
               x = Fixed3DOffset.Width;
               y = Fixed3DOffset.Height;
               break;
            case BorderStyle.FixedSingle:
               x = FixedSingleOffset.Width;
               y = FixedSingleOffset.Height;
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

      private void OnCedeFocus( Object sender, CedeFocusEventArgs e )
      {
         switch ( e.Action )
         {
            case Action.Home:

               _fieldControls[0].TakeFocus( Action.Home );
               return;

            case Action.End:

               _fieldControls[FieldCount - 1].TakeFocus( Action.End );
               return;

            case Action.Trim:

               if ( e.FieldIndex == 0 )
               {
                  return;
               }

               _fieldControls[e.FieldIndex - 1].TakeFocus( Action.Trim );
               return;
         }

         if ( ( e.Direction == Direction.Reverse && e.FieldIndex == 0 ) ||
              ( e.Direction == Direction.Forward && e.FieldIndex == ( FieldCount - 1 ) ) )
         {
            return;
         }

         int fieldIndex = e.FieldIndex;

         if ( e.Direction == Direction.Forward )
         {
            ++fieldIndex;
         }
         else
         {
            --fieldIndex;
         }

         _fieldControls[fieldIndex].TakeFocus( e.Direction, e.Selection );
      }

      private void OnFieldGotFocus( Object sender, EventArgs e )
      {
         if ( !_focused )
         {
            _focused = true;
            base.OnGotFocus( EventArgs.Empty );
         }
      }

      private void OnFieldKeyDown( Object sender, KeyEventArgs e )
      {
         OnKeyDown( e );
      }

      private void OnFieldKeyPressed( Object sender, KeyPressEventArgs e )
      {
         OnKeyPress( e );
      }

      private void OnFieldPreviewKeyDown( Object sender, PreviewKeyDownEventArgs e )
      {
         OnPreviewKeyDown( e );
      }

      private void OnFieldKeyUp( Object sender, KeyEventArgs e )
      {
         OnKeyUp( e );
      }

      private void OnFieldLostFocus( Object sender, EventArgs e )
      {
         if ( !Focused )
         {
            _focused = false;
            base.OnLostFocus( EventArgs.Empty );
         }
      }

      private void OnFieldTextChanged( Object sender, TextChangedEventArgs e )
      {
         if ( null != FieldChangedEvent )
         {
            FieldChangedEventArgs args = new FieldChangedEventArgs();
            args.FieldIndex = e.FieldIndex;
            args.Text = e.Text;
            FieldChangedEvent( this, args );
         }

         OnTextChanged( EventArgs.Empty );
      }

      private void OnPaste( Object sender, PasteEventArgs e )
      {
         Text = e.Text;
      }

      private void OnSubControlClicked( object sender, EventArgs e )
      {
         OnClick( e );
      }

      private void OnSubControlDoubleClicked( object sender, EventArgs e )
      {
         OnDoubleClick( e );
      }

      private void OnSubControlMouseClicked( object sender, MouseEventArgs e )
      {
         OnMouseClick( e );
      }

      private void OnSubControlMouseDoubleClicked( object sender, MouseEventArgs e )
      {
         OnMouseDoubleClick( e );
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

      private static String[] Parse( String text, String[] separators )
      {
         List<String> result = new List<String>();

         int textIndex = 0;
         int index = 0;

         for ( index = 0; index < separators.Length; ++index )
         {
            int findIndex = text.IndexOf( separators[ index ], textIndex, StringComparison.Ordinal );

            if ( findIndex >= 0 )
            {
               result.Add( text.Substring( textIndex, findIndex - textIndex ) );
               textIndex = findIndex + separators[ index ].Length;
            }
            else
            {
               break;
            }
         }

         result.Add( text.Substring( textIndex ) );

         return result.ToArray();
      }

       // a hack to remove an FxCop warning
      private void ResetBackColorChanged()
      {
         _backColorChanged = false;
      }

      #endregion Private Methods

      #region Private Data

      private bool _autoHeight = true;
      private bool _backColorChanged;
      private BorderStyle _borderStyle = BorderStyle.Fixed3D;
      private DotControl[] _dotControls = new DotControl[FieldCount - 1];
      private FieldControl[] _fieldControls = new FieldControl[FieldCount];
      private bool _focused;
      private bool _hasMouse;
      private bool _readOnly;

      private Size Fixed3DOffset = new Size( 3, 3 );
      private Size FixedSingleOffset = new Size( 2, 2 );

      #endregion  // Private Data
   }
}