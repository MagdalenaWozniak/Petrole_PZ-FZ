using CDNBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PZ_FZ
{
    static class Program
    {
        public static bool zalogowano = false;
        public static string args0 ="";
        public static string args1 ="";

        public static CDNBase.Application Application { get; private set; }
        public static ILogin Login { get; private set; }
        public static string Operator = "ADMIN";
        public static string Haslo = "NE!w0rk12";
        public static string Firma = "SCA_Petrole";

        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]

        static void logowanie()
        {
            try
            {
                object[] hPar = new object[] {
                         0,  0,   0,  0,  0,   0,  0,    0,   0,   0,   0,   0,   0,   0,  0,  1, 1 };    // do jakich modułów się logujemy
                /* Kolejno: KP, KH, KHP, ST, FA, MAG, PK, PKXL, CRM, ANL, DET, BIU, SRW, ODB, KB, KBP, HAP*/
                // katalog, gdzie jest zainstalowana Optima (bez ustawienia tej zmiennej nie zadziała, chyba że program odpalimy z katalogu O!)
                System.Environment.CurrentDirectory = @"C:\Program Files (x86)\Comarch ERP Optima";
                // tworzymy nowy obiekt apliakcji
                Application = new CDNBase.Application();
                // Application.HASPServerCommandLine = "172.18.34.50\\OPTIMA::5000242577";
                // blokujemy
                Application.LockApp(1);//(513, 5000, null, null, null, null);
                                       // logujemy się do podanej Firmy, na danego operatora, do podanych modułów
                Login = Application.Login(vUser: Operator, vChk: Haslo, vFirm: Firma, _ModulKP: hPar[0], _ModulKH: hPar[1], _ModulKHP: hPar[2], _ModulST: hPar[3], _ModulFA: hPar[4], _ModulMAG: hPar[5], _ModulPK: hPar[6],
                    _ModulPKXL: hPar[7], _ModulCRM: hPar[8], _ModulANL: hPar[9], _ModulDET: hPar[10], _ModulBIU: hPar[11], _ModulSRW: hPar[12], _ModulOBD: hPar[13], _ModulKB: hPar[14], _ModulKBP: hPar[15], _ModulHAP: hPar[16]);
            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd logowania: " + e.Message, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void rozliczenieFZ()
        {
            try
            {

                string selectQuery = " Select TrN_TrNID, Dee_DeeID, TrN_NumerPelny, TrN_Waluta from CDN.KsiRozrachunki join cdn.TraNag on TrN_NumerObcy = Kro_Dokument join CDN.DekretyElem on TrN_DekID = DeE_DeNId where KRo_RozliczenieId is null and DeE_KontoMa = '910000'";
                CDNBase.AdoSession Sesja = Login.CreateSession();
                object dummy = Type.Missing;
                var select = Sesja.Connection.Execute(selectQuery, out dummy, 0);



                //#region Kompletowanie listy pozycji, sprawdzanie kryteriów.
                //string PierwszyDokument = "";
                //select.MoveFirst();
                //while (!select.EOF)
                //{
                  //  PierwszyDokument = select.Fields[0].Value.ToString();
                    //shell.popup("id: "+PierwszyDokument, 20, "Komunikat: ",0+48)
                   // select.MoveNext();


                //}
                //#endregion

                select.MoveFirst();
                //#region Grupowanie listy i generowanie dokumentów

                while (!select.EOF)
                {
                    try
                    {
                        var FZ = select.Fields[0].Value.ToString();
                        Sesja = Login.CreateSession();

                        //shell.popup("Po logowaniu", 20, "Komunikat: ",0+48)

                        var PZidQuery = "SELECT [TrR_FaId] FROM [CDN].[TraNagRelacje] WHERE TrR_TrNId=" + select.Fields[0].Value.ToString() + " and TrR_FaTyp=307";
                        //shell.popup("PZ id Query: "+PZidQuery, 20, "Komunikat: ",0+48)


                        var PZty = Sesja.Connection.Execute(PZidQuery, out dummy, 0);


                        PZty.MoveFirst();
                        while (!PZty.EOF)
                        {
                            try
                            {
                                var PZid = PZty.Fields[0].Value.ToString();
                                var czyKsiQuery = "if exists(Select * from CDN.KsiRozrachunki join cdn.TraNag on TrN_NumerObcy=Kro_Dokument join CDN.DekretyElem on TrN_DekID=DeE_DeNId  where KRo_RozliczenieId is null and TrN_TrNID=" + PZid + " and DeE_KontoWn='910000') select 'TAK' else select 'NIE'";
                                var czyKsi = Sesja.Connection.Execute(czyKsiQuery, out dummy, 0);
                                czyKsi.MoveFirst();
                                if (czyKsi.Fields[0].Value.ToString() == "TAK")
                                {
                                    // shell.popup("id PZ: "+PZid +" id FZ: "+FZ, 20, "Komunikat: ",0+48);

                                    var czyKwotaQuery = "if (SELECT SUM(KRo_KwotaDok)-SUM(KRo_SumRozliczen) from CDN.KsiRozrachunki join cdn.TraNag on TrN_NumerObcy=Kro_Dokument join CDN.DekretyElem on TrN_DekID=DeE_DeNId WHERE TrN_TrNID=" + FZ + " and DeE_KontoMa='910000' "
                                                        + " and KRo_RozliczenieId is null) > (SELECT SUM(KRo_KwotaDok)-SUM(KRo_SumRozliczen) from CDN.KsiRozrachunki join cdn.TraNag on TrN_NumerObcy=Kro_Dokument join CDN.DekretyElem on TrN_DekID=DeE_DeNId "
                                                        + " WHERE TrN_TrNID=" + PZid + " and KRo_RozliczenieId is null and DeE_KontoWn='910000') SELECT 'TAK' else SELECT 'NIE'";
                                    var czyKwota = Sesja.Connection.Execute(czyKwotaQuery, out dummy, 0);
                                    czyKwota.MoveFirst();
                                    if (czyKwota.Fields[0].Value.ToString() == "TAK")
                                    {
                                        //shell.popup("id PZ: "+PZid +" id FZ: "+FZ, 20, "Komunikat: ",0+48);

                                        var PZQuery = "Select Dee_DeeID, TrN_NumerPelny, TrN_Waluta, DeE_KwotaWal from CDN.KsiRozrachunki join cdn.TraNag on TrN_NumerObcy=Kro_Dokument join CDN.DekretyElem on TrN_DekID=DeE_DeNId where TrN_TrNID=" + PZid + ""
                                                                            + " and KRo_RozliczenieId is null and DeE_KontoWn='910000'";
                                        var PZtki = Sesja.Connection.Execute(PZQuery, out dummy, 0);
                                        PZtki.MoveFirst();
                                        var result = 0;
                                        var teraz = DateTime.Now;

                                        //shell.popup("Teraz: "+teraz.toString()+"  id 1: "+Recordset.Fields(1).value.toString()+"  id2: " +PZtki.Fields(0).value.toString(), 20, "Komunikat: ",0+48)
                                        var rozrachunekR2R = Sesja.CreateObject("CDN.RozrachunekR2R");
                                        rozrachunekR2R.Rozlicz(1, Convert.ToInt32(select.Fields[1].Value.ToString()), 1, Convert.ToInt32(PZtki.Fields[0].Value.ToString()), "PLN", 0, 0, 0, teraz, 0, ref result);

                                       // if (result != 0)
                                         //   MessageBox.Show("Połącznie " + PZtki.Fields[1].Value.ToString() + " i " + select.Fields[2].Value.ToString() + " Wynik: " + result, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Warning);


                                    }



                                }



                            }
                            catch (Exception e)
                            {
                                MessageBox.Show("Błąd rozliczenia  : " + e.Message, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }


                            PZty.MoveNext();
                        }




                        select.MoveNext();
                        Sesja.Save();
                        Sesja.Dispose();
                    }
                    catch(Exception n)
                    {
                        select.MoveNext();
                        Sesja.Dispose();
                        //MessageBox.Show("Błąd rozliczenia FZ : " + n.Message, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }

                

            }
            catch(Exception em)
            {
                MessageBox.Show("Błąd funkcji do rozliczania  : " + em.Message, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


               //#endregion*/



        }


        public static void wylogowanie()
        {
            // niszczymy Login
            Login = null;
            // odblokowanie (wylogowanie) O!
            Application.UnlockApp();
            // niszczymy obiekt Aplikacji
            Application = null;
        }
        static void Main(string[] args)
        { 
            if (args.Count() == 2)
            {
                args0 = args[0];
                args1 = args[1];
            }

            //MessageBox.Show(args0+" "+ args1);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Logowanie());
            //if(zalogowano)
            //MessageBox.Show(args0 +" i "+ args1);
            //Application.Run(new ONE032WIN());
            try
            {
                logowanie();
                rozliczenieFZ();
                wylogowanie();
                MessageBox.Show("Zakończono rozliczanie rozrachunków", "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch(Exception e)
            {
                MessageBox.Show("Błąd proframu: " + e.Message, "Komunikat: ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
