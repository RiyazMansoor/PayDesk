

namespace PayDesk.Util
{
    public class MvUtil
    {

        public static string toPensionMv( int pensiontype )
        {
            switch ( pensiontype )
            {
                case 11   : return "cnwxcnep IsWswa egcnuhIm Ivcswvud cnurumua" ;
                case 10101: return "cnwxcnep egurwhwa 20" ;
                case 10102: return "cnwxcnep egurwhwa 40" ;
                case 10103: return "cnwxcnep egurwhwa 60" ;
                case 11112: return "Wsiawf Ebil cSwrwhwfcaea cnumuvcaerikwv cniaWfIzWv egurWkurws" ;
                case 11113: // 11115
                case 11114: // 11115
                case 11115: return "cjEkep cTcnemwywTir" ;
                case 11116: return "wrWsum urwhwa 2 - cnwxcnep cTcnemwywTir" ;
                case 22   : return "cnwxcnep cTcnemwywTir" ;
                case 220  : return "cnwxcnep cTcnemwywTir IlWa" ;
                case 333  : return "cscnwvwlea egcnuhIm Ivcswvud cnurumua" ;
                case 20104: return "wrWsum Eved cnumuvcaerikwv cniaWfIzWv egurWkurws cnutog iruh ulWhilwb" ;
                case 20105: return "wrWsum 1/3 cnumuvuDob ulWh ilwb" ;
                case 20106: // 20111
                case 20107: // 20111
                case 20108: // 20111
                case 20109: // 20111
                case 20110: // 20111
                case 20111: return "cscnwvwlea Wpcscnea" ;
                default: return "EgEn" ;
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
                case 0: return " Irwaunej ";
                case 1: return " rwaurcbef ";
                case 2: return " cCirWm ";
                case 3: return " clircpEa ";
                case 4: return " Em ";
                case 5: return " cnUj ";
                case 6: return " iawluj ";
                case 7: return " cTcswgOa ";
                case 8: return " urwbcneTcpes ";
                case 9: return " urwbUTckoa ";
                case 10: return " urwbcmebon ";
                case 11: return " urwbcnesiD ";
                default: return " EgEn EgEn EgEn EgEn EgEn EgEn ";
            }
        }

        public static string toLiveTitleMv( string sex )
        {
            switch ( sex.ToCharArray()[ 0 ] )
            {
                case 'M': return " Irwaunej ";
                case 'F': return " rwaurcbef ";
                default: return "ނޭގޭ ނޭގޭ ނޭގޭ ";
            }
        }

        public static string toDeadTitleMv( string sex )
        {
            switch ( sex.ToCharArray()[ 0 ] )
            {
                case 'M': return " Irwaunej ";
                case 'F': return " rwaurcbef ";
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

    }

}
