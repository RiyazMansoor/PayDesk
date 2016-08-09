

namespace PayDesk.Util
{
    public class MvUtil
    {

        public static string toPensionMv( int pensiontype )
        {
            switch ( pensiontype )
            {
                case 11   : return "އުމުރުން ދުވަސްވީ މީހުންގެ އަސާސީ ޕެންޝަން" ;
                case 10101: return " 20 އަހަރުގެ ޕެންޝަން" ;
                case 10102: return " 40 އަހަރުގެ ޕެންޝަން";
                case 10103: return " 60 އަހަރުގެ ޕެންޝަން";
                case 11112: return "ދައުލަތުގެ ވަޒީފާއިން ވަކިކުރެއްވުމުން އެއްފަހަރަށް ލިބޭ ފައިސާ" ;
                case 11113: // 11115
                case 11114: // 11115
                case 11115: return "ރިޓަޔަމަންޓް ޕެކޭޖް" ;
                case 11116: return "ރިޓަޔަމަންޓް ޕެކޭޖް - 2 އަހަރު މުސާރަ";
                case 22   : return "ރިޓަޔަމަންޓް ޕެންޝަން";
                case 220  : return "އާލީ ރިޓަޔަމަންޓް ޕެންޝަން" ;
                case 333  : return "އުމުރުން ދުވަސްވީ މީހުންގެ އެލަވަންސް";
                case 20104: return "ބަލިހާލު ހުރިގޮތުން ވަޒީފާއިން ވަކި ކުރެއްވުމުން ދެވޭ މުސާރަ" ;
                case 20105: return "ބަލިހާލު ބޮޑުވުމުން 1/3 މުސާރަ" ;
                case 20106: // 20111
                case 20107: // 20111
                case 20108: // 20111
                case 20109: // 20111
                case 20110: // 20111
                case 20111: return "އެންސްޕާ އަލަވަންސް";
                case -99: return "އިތުރަށް ދެވިފައިވާ ފައިސާ";
                case 223: return "މޯލްޑިވްސް ރިޓަޔަމަންޓް ޕެންޝަން ސްކީމް";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toPortfolioNameMv( int portfolioCode )
        {
            switch ( portfolioCode )
            {
                case 10: return "އިންވެސްޓްމްންޓް ޕޯޓްފޯލިއޯ";
                case 18: return "ޝަރީއާ ޕޯޓްފޯލިއޯ";
                case 20: return "ރިކޮގްނިޝަން ބޮންޑް ޕޯޓްފޯލިއޯ";
                case 30: return "ކޮންސަވެޓިވް ޕޯޓްފޯލިއޯ";
                case 31: return "ޝަރީއާ ކޮންސަވެޓިވް ޕޯޓްފޯލިއޯ";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toMonthMv( int monthindex )
        {
            switch ( monthindex )
            {
                case 0: return " ޖެނުއަރީ ";
                case 1: return " ފެބްރުއަރީ ";
                case 2: return " މާރިޗް ";
                case 3: return " އޭޕްރިލް ";
                case 4: return " މޭ ";
                case 5: return " ޖޫން ";
                case 6: return " ޖުލައި ";
                case 7: return " އޯގަސްޓް ";
                case 8: return " ސެޕްޓެމްބަރ ";
                case 9: return " އޮކްޓޯބަރ ";
                case 10: return " ނޮވެމްބަރ ";
                case 11: return " ޑިސެމްބަރ ";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toLiveTitleMv( string sex )
        {
            switch ( sex.ToCharArray()[ 0 ] )
            {
                //case 'M': return " Irwaunej ";
                //case 'F': return " rwaurcbef ";
                case 'M': return " އަލްފާޟިލް ";
                case 'F': return " އަލްފާޟިލާ ";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toDeadTitleMv( string sex )
        {
            switch ( sex.ToCharArray()[ 0 ] )
            {
                //case 'M': return " Irwaunej ";
                //case 'F': return " rwaurcbef ";
                case 'M': return " އަލްމަރްހޫމް ";
                case 'F': return " އަލްމަރްހޫމާ ";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toCourtResultMv( decimal total )
        {
            int v = ( total > 0 ? 1 : ( total < 0 ? -1 : 0 ) );
            switch ( v )
            {
                case 1: return "ހައްޤުވާ އަންދާސީ އަދަދު";
                case 0: return "ހައްޤުވާ ފައިސާއެއް ނެތް";
                case -1: return "އަނބުރާ ދައްކަވަން ޖެހޭ އަދަދު";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toStaticTotalMv( string something )
        {
            return "ޖުމްލަ ";
        }

        public static string toDateMv( System.DateTime v )
        {
            return v == null ? "--" : v.Year + toMonthMv( v.Month ) + v.Day ;
        }

        public static string toPhoneticMv( string ascii )
        {
            char[] ca = ascii.ToCharArray();
            System.Array.Reverse( ca );
            System.Text.StringBuilder bld = new System.Text.StringBuilder();
            foreach ( char c in ca )
            {
                int i = System.Array.IndexOf( AsciiMv, c );
                //System.Diagnostics.Debug.WriteLine( "i:" + i + ", c:" + c );
                bld.Append( i < 0 ? c : PhoneticHhMv[ i ] );
                //System.Diagnostics.Debug.WriteLine( "phonetic:" + bld.ToString() );
            }
            return bld.ToString();
        }

        private static char[] AsciiMv      = "qwertyuiop[]\\asdfghjkl;\'zxcvbnm,./QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?()".ToCharArray();
        private static char[] PhoneticMv = "ްއެރތޔުިޮޕ][\\ަސދފގހޖކލ؛'ޒ×ޗވބނމ،./ޤޢޭޜޓޠޫީޯ÷}{|ާށޑﷲޣޙޛޚޅ:\"ޡޘޝޥޞޏޟ><؟)(".ToCharArray();
        private static char[] PhoneticHhMv = "ޤަެރތޔުިޮޕ][\\އސދފގހޖކލ؛\'ޒޝްވބނމ،./ﷲާޭޜޓޠޫީޯޕ}{|ޢށޑޟޣޙޛޚޅ:\"ޡޘޗޥޞޏމ><؟)(".ToCharArray();
        private static char[] TypewriterMv = "ޫޮާީޭގރމތހލ[]ިުްަެވއނކފﷲޒޑސޔޅދބށޓޯ×’“/:ޤޜޣޠޙ÷{}<>.،\"ޥޢޘޚޡ؛ޖޕޏޗޟޛޝ\\ޞ؟)(".ToCharArray();
        
        public static void main(string[] args )
        {
            System.Console.WriteLine( toPhoneticMv( " Irwaunej " ) );
        }

    }

}
