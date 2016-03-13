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
using System.Drawing;

namespace IPAddressControlLib
{
   internal class Utility
   {
      private Utility()
      {
      }

      public static Size CalculateStringSize( IntPtr handle, Font font, string text )
      {
         StringFormat stringFormat = new StringFormat();
         RectangleF rect = new RectangleF( 0, 0, 9999, 9999 );

         CharacterRange[] ranges = { new CharacterRange( 0, text.Length ) };

         Region[] regions = new Region[1];

         stringFormat.SetMeasurableCharacterRanges( ranges );

         Graphics g = Graphics.FromHwnd( handle );

         regions = g.MeasureCharacterRanges( text,
            font, rect, stringFormat );

         rect = regions[0].GetBounds( g );

         g.Dispose();

         float fudgeFactor = ( font.SizeInPoints / 8.25F ) * 3.0F;

         return new Size( (int)(rect.Width + fudgeFactor), (int)(rect.Height) );
      }
   }
}
