using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace PayDesk.temp
{
    public class Class1
    {
        public static void main( string[] args )
        {
            string[] fileEntries = Directory.GetFiles( "C:\\Users" );
            Debug.WriteLine( fileEntries );

        }
    }

}