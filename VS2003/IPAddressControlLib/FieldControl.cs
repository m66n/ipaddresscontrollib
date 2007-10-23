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
using System.Globalization;
using System.Windows.Forms;

namespace IPAddressControlLib
{
   internal enum Direction
   {
      Forward,
      Reverse
   };

   internal enum Selection
   {
      None,
      All
   };

   internal enum Action
   {
      None,
      Trim,
      Home,
      End
   }

   internal delegate void CedeFocusHandler( int fieldIndex, Direction direction, Selection selection, Action action );
   internal delegate void TextChangedHandler( int fieldIndex, string newText );

	internal class FieldControl : System.Windows.Forms.TextBox
	{
      public event CedeFocusHandler CedeFocusEvent;
      public event TextChangedHandler TextChangedEvent;

      public const int MinimumValue = 0;
      public const int MaximumValue = 255;

      #region Private Data

      private int _fieldIndex = -1;

      private bool _invalidKeyDown;

      private int _rangeLower;  // using " = MinimumValue; " here is flagged by FxCop
      private int _rangeUpper = MaximumValue;

      #endregion

      #region Public Properties

      public bool Blank
      {
         get
         {
            if ( TextLength > 0 )
            {
               return false;
            }

            return true;
         }
      }

      public int FieldIndex
      {
         get { return _fieldIndex; }
         set { _fieldIndex = value; }
      }

      public int RangeLower
      {
         get {  return _rangeLower; }
         set
         {
            if ( value < MinimumValue )
            {
               _rangeLower = MinimumValue;
            }
            else
            if ( value > _rangeUpper )
            {
               _rangeLower = _rangeUpper;
            }
            else
            {
               _rangeLower = value;
            }

            if ( Value < _rangeLower )
            {
               Text = _rangeLower.ToString( CultureInfo.InvariantCulture );
            }
         }
      }

      public int RangeUpper
      {
         get { return _rangeUpper; }
         set
         {
            if ( value < _rangeLower )
            {
               _rangeUpper = _rangeLower;
            }
            else
            if ( value > MaximumValue )
            {
               _rangeUpper = MaximumValue;
            }
            else
            {
               _rangeUpper = value;
            }

            if ( Value > _rangeUpper )
            {
               Text = _rangeUpper.ToString( CultureInfo.InvariantCulture );
            }
         }
      }

      #endregion

      #region Public Functions

      public void SetFont( Font font )
      {
         Font = font;
         Size = CalculateControlSize();
      }

      public void TakeFocus( Action action )
      {
         Focus();

         switch ( action )
         {
            case Action.Trim:

               if ( TextLength > 0 )
               {
                  int newLength = TextLength - 1;
                  Text = Text.Substring( 0, newLength );
               }
               SelectionStart = TextLength;
               return;

            case Action.Home:

               SelectionStart = 0;
               SelectionLength = 0;
               return;

            case Action.End:

               SelectionStart = TextLength;
               return;
         }
      }

      public void TakeFocus( Direction direction, Selection selection )
      {
         Focus();

         if ( selection == Selection.All )
         {
            SelectionStart = 0;
            SelectionLength = TextLength;
         }
         else
         {
            SelectionStart = ( direction == Direction.Forward ) ? 0 : TextLength;
         }
      }

      public override string ToString()
      {
         return Value.ToString( CultureInfo.InvariantCulture );
      }

      #endregion

      #region Constructor

		public FieldControl()
		{
         AutoSize         = false;
         BorderStyle      = BorderStyle.None;
         MaxLength        = 3;
         TabStop          = false;
         TextAlign        = HorizontalAlignment.Center;

         Size             = CalculateControlSize();
		}

      #endregion

      #region Overrides

      protected override void OnKeyDown( KeyEventArgs e )
      {
         base.OnKeyDown( e );

         _invalidKeyDown = false;

         switch ( e.KeyCode )
         {
            case Keys.Home:
               SendCedeFocusEvent( Action.Home );
               return;

            case Keys.End:
               SendCedeFocusEvent( Action.End );
               return;
         }

         if ( IsCedeFocusKey( e ) )
         {
            SendCedeFocusEvent( Direction.Forward, Selection.All );
            _invalidKeyDown = true;
            return;
         }
         else if ( IsForwardKey( e ) )
         {
            if ( e.Control )
            {
               SendCedeFocusEvent( Direction.Forward, Selection.All );
               return;
            }
            else if ( SelectionLength == 0 && SelectionStart == TextLength )
            {
               SendCedeFocusEvent( Direction.Forward, Selection.None );
               return;
            }
         }
         else if ( IsReverseKey( e ) )
         {
            if ( e.Control )
            {
               SendCedeFocusEvent( Direction.Reverse, Selection.All );
               return;
            }
            else if ( SelectionLength == 0 && SelectionStart == 0 )
            {
               SendCedeFocusEvent( Direction.Reverse, Selection.None );
               return;
            }
         }
         else if ( IsBackspaceKey( e ) )
         {
            HandleBackspaceKey();
         }
         else if ( !IsNumericKey( e ) && !IsEditKey( e ) )
         {
            _invalidKeyDown = true;
         }
      }

      protected override void OnKeyPress( KeyPressEventArgs e )
      {
         if ( _invalidKeyDown )
         {
            e.Handled = true;
         }

         base.OnKeyPress( e );
      }

      protected override void OnTextChanged( EventArgs e )
      {
         base.OnTextChanged( e );

         if ( !Blank )
         {
            try
            {
               int value = Int32.Parse( Text, CultureInfo.InvariantCulture );

               if ( value > RangeUpper )
               {
                  base.Text = RangeUpper.ToString( CultureInfo.InvariantCulture );
                  SelectionStart = 0;
               }
               else if ( ( TextLength == MaxLength ) && ( value < RangeLower ) )
               {
                  base.Text = RangeLower.ToString( CultureInfo.InvariantCulture );
                  SelectionStart = 0;
               }
               else
               {
                  int originalLength = TextLength;
                  int newSelectionStart = SelectionStart;

                  base.Text = value.ToString( CultureInfo.InvariantCulture );

                  if ( TextLength < originalLength )
                  {
                     newSelectionStart -= ( originalLength - TextLength );
                     SelectionStart = Math.Max( 0, newSelectionStart );
                  }
               }
            }
            catch ( ArgumentNullException )
            {
               Text = String.Empty;
            }
            catch ( FormatException )
            {
               Text = String.Empty;
            }
            catch ( OverflowException )
            {
               Text = String.Empty;
            }
         }

         if ( TextChangedEvent != null )
         {
            TextChangedEvent( FieldIndex, Text );
         }

         if ( TextLength == MaxLength && Focused && SelectionStart == TextLength )
         {
            SendCedeFocusEvent( Direction.Forward, Selection.All );
         }
      }

      protected override void OnValidating( CancelEventArgs e )
      {
         if ( !Blank )
         {
            if ( Value < RangeLower )
            {
               Text = RangeLower.ToString( CultureInfo.InvariantCulture );
            }
         }

         base.OnValidating( e );
      }

      protected override void WndProc( ref Message m )
      {
         switch ( m.Msg )
         {
            case NativeMethods.WM_CONTEXTMENU:
               return;
         }

         base.WndProc( ref m );
      }

      #endregion

      #region Private Functions

      private Size CalculateControlSize()
      {
         string text = new string( '0', MaxLength );
         return Utility.CalculateStringSize( Handle, Font, text );
      }

      private void HandleBackspaceKey()
      {
         if ( !ReadOnly && ( TextLength == 0 || ( SelectionStart == 0 && SelectionLength == 0 ) ) )
         {
            SendCedeFocusEvent( Action.Trim );
            _invalidKeyDown = true;
         }
      }

      private static bool IsBackspaceKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Back )
         {
            return true;
         }

         return false;
      }

      private bool IsCedeFocusKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.OemPeriod ||
              e.KeyCode == Keys.Decimal ||
              e.KeyCode == Keys.Space )

         {
            if ( TextLength != 0 && SelectionLength == 0 && SelectionStart != 0 )
            {
               return true;
            }
         }

         return false;
      }

      private static bool IsEditKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Back ||
              e.KeyCode == Keys.Delete )
         {
            return true;
         }
         else if ( e.Modifiers == Keys.Control &&
                   ( e.KeyCode == Keys.C ||
                     e.KeyCode == Keys.V ||
                     e.KeyCode == Keys.X ) )
         {
            return true;
         }

         return false;
      }

      private static bool IsForwardKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Right ||
              e.KeyCode == Keys.Down )
         {
            return true;
         }

         return false;
      }

      private static bool IsNumericKey( KeyEventArgs e )
      {
         if ( e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9 )
         {
            if ( e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9 )
            {
               return false;
            }
         }

         return true;
      }

      private static bool IsReverseKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Left ||
              e.KeyCode == Keys.Up )
         {
            return true;
         }

         return false;
      }

      private void SendCedeFocusEvent( Action action )
      {
         if ( CedeFocusEvent != null )
         {
            CedeFocusEvent( FieldIndex, Direction.Forward, Selection.None, action );
         }
      }

      private void SendCedeFocusEvent( Direction direction, Selection selection )
      {
         if ( CedeFocusEvent != null )
         {
            CedeFocusEvent( FieldIndex, direction, selection, Action.None );
         }
      }

      #endregion

      #region Private Properties

      private int Value
      {
         get
         {
            try
            {
               return Int32.Parse( Text, CultureInfo.InvariantCulture );
            }
            catch ( ArgumentNullException )
            {
               return RangeLower;
            }
            catch ( FormatException )
            {
               return RangeLower;
            }
            catch ( OverflowException )
            {
               return RangeLower;
            }
         }
      }

      #endregion
	}
}
