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
using System.Runtime.InteropServices;

namespace IPAddressControlLib
{
	internal class NativeMethods
	{
		private NativeMethods()
		{
		}

      // GDI-related

      [DllImport("gdi32", CharSet=CharSet.Auto, SetLastError=true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool DeleteObject( IntPtr hObject );

      public static int GetRValue( uint colorref )
      {
         return (int)( colorref & 0xff );
      }

      public static int GetGValue( uint colorref )
      {
         return (int)( ( colorref >> 8 ) & 0xff );
      }

      public static int GetBValue( uint colorref )
      {
         return (int)( ( colorref >> 16 ) & 0xff );
      }

      public static uint RGB( int r, int g, int b )
      {
         return (uint)( ((uint)r) | (((uint)g)<<8) | (((uint)b)<<16) ) ;
      }

      // ComCtl-related

      [DllImport("comctl32", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern uint DllGetVersion( ref DLLVERSIONINFO pdvi );

      // Theme-related

      public const int EP_EDITTEXT = 1;
      public const int EP_CARET    = 2;

      public const int ETS_NORMAL   = 1;
      public const int ETS_HOT      = 2;
      public const int ETS_SELECTED = 3;
      public const int ETS_DISABLED = 4;
      public const int ETS_FOCUSED  = 5;
      public const int ETS_READONLY = 6;
      public const int ETS_ASSIST   = 7;

      public const uint DTBG_CLIPRECT        = 0x00000001;  
      public const uint DTBG_DRAWSOLID       = 0x00000002;  
      public const uint DTBG_OMITBORDER      = 0x00000004;  
      public const uint DTBG_OMITCONTENT     = 0x00000008;  
      public const uint DTBG_COMPUTINGREGION = 0x00000010;  
      public const uint DTBG_MIRRORDC        = 0x00000020;  

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern int CloseThemeData( IntPtr hTheme );

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern int DrawThemeBackground( IntPtr hTheme, IntPtr hdc, int iPartId,
         int iStateId, ref RECT pRect, IntPtr pClipRect );

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern int DrawThemeBackgroundEx( IntPtr hTheme, IntPtr hdc, int iPartId,
         int iStateId, ref RECT pRect, ref DTBGOPTS pOptions );

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern uint GetThemeSysColor( IntPtr hTheme, int iColorID );

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern IntPtr GetThemeSysColorBrush( IntPtr hTheme, int iColorID );

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool IsAppThemed();

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool IsThemeActive();

      [DllImport("uxtheme", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern IntPtr OpenThemeData( IntPtr hWnd, string lpString );

      public static bool IsThemed()
      {
         bool retval = false;

         if ( Environment.OSVersion.Version.Major >= 5 &&
              Environment.OSVersion.Version.Minor >= 1 )
         {
            bool appThemed   = NativeMethods.IsAppThemed();
            bool themeActive = NativeMethods.IsThemeActive();

            if ( appThemed && themeActive )
            {
               DLLVERSIONINFO dvi = new DLLVERSIONINFO();
               dvi.cbSize = (uint)Marshal.SizeOf(dvi);

               NativeMethods.DllGetVersion( ref dvi );

               retval = ( dvi.dwMajorVersion >= 6 );
            }
         }

         return retval;
      }

      // User-related

      public const int WM_WINDOWPOSCHANGING = 0x0046;
      public const int WM_CONTEXTMENU = 0x007b;

      public const uint SWP_NOMOVE = 0x0002;

      [DllImport("user32", CharSet=CharSet.Auto, SetLastError=true)]
      public static extern int FillRect( IntPtr hDC, ref RECT lprc, IntPtr hbr );

      // Win32 structs

      [StructLayout(LayoutKind.Sequential)]
      internal struct DLLVERSIONINFO
      {
         public uint cbSize;
         public uint dwMajorVersion;
         public uint dwMinorVersion;
         public uint dwBuildNumber;
         public uint dwPlatformID;
      }

      [StructLayout(LayoutKind.Sequential)]
      internal struct DTBGOPTS
      {
         public uint dwSize;
         public uint dwFlags;
         public RECT rcClip;
      }

      [StructLayout(LayoutKind.Sequential)]
      internal struct RECT
      {
         public int left;
         public int top;
         public int right;
         public int bottom;
      }

      [StructLayout(LayoutKind.Sequential)]
      internal struct WINDOWPOS
      {
         public IntPtr hwnd;
         public IntPtr hwndInsertAfter;
         public int x;
         public int y;
         public int cx;
         public int cy;
         public uint flags;
      }
	}
}
