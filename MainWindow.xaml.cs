using System;
using InsERT.Moria.Sfera;
using InsERT.Mox.Product;
using System.Text;
using System.Windows;
using InsERT.Moria.Dokumenty.Logistyka;
using InsERT.Moria.Urzadzenia;

namespace NexoTest1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string sql_host = "NUC01";
        static string podmiot = "Nexo_test_nexo sp. j. ą";
        static string @operator = "Szef";
        static string operator_haslo = "test";
        static string sql_user = "sa";
        static string sql_haslo = "Sa123456";
        static int id_drukarki = 100007;
        static int fiskalizacja_timeout = 30; // seconds

        IFiskalizacjaDokumentu iFiskalizacjaDokumentu;

        Lazy<Uchwyt> sfera = new(UruchomSfere);
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs ea)
        {
            var nr_paragonu = ParagonTextBox.Text;
            try
            {
                if (string.IsNullOrEmpty(nr_paragonu))
                {
                    throw new Exception("Podaj numer paragonu");
                }

                var dokumentySprzedazy = sfera.Value.DokumentySprzedazy();
                var dokumentEncja = dokumentySprzedazy.Dane.Wszystkie().FirstOrDefault(d => string.Compare(d.NumerWewnetrzny.PelnaSygnatura, nr_paragonu, StringComparison.OrdinalIgnoreCase) == 0);
                if (dokumentEncja == null)
                {
                    throw new Exception("Nie znaleziono paragonu o podanym numerze");
                }

                using (var dokument = dokumentySprzedazy.Znajdz(dokumentEncja))
                {
                    IWynikFiskalizacji wynik = dokument.ObslugaFiskalizacji.Fiskalizuj(id_drukarki);
                    if (wynik.Status != StatusWykonywaniaFiskalizacji.OK)
                    {
                        throw new Exception("Błąd fiskalizacji: " + string.Join(". ", wynik.Bledy));
                    }

                    MessageBox.Show("Paragon został pomyślnie fiskalizowany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        static Uchwyt UruchomSfere()
        {
            DanePolaczenia danePolaczenia = DanePolaczenia.Jawne(sql_host, podmiot, false, sql_user, sql_haslo);
            MenedzerPolaczen mp = new MenedzerPolaczen();
            mp.DostepDoUI = true;
            Uchwyt sfera = mp.Polacz(danePolaczenia, ProductId.Subiekt);
            sfera.ZalogujOperatora(@operator, operator_haslo);
            return sfera;
        }

        // Dispose the Uchwyt when the window is closed and force app shutdown.
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (sfera?.IsValueCreated == true)
                {
                    if (sfera.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch
            {
                // suppress exceptions during shutdown
            }
            finally
            {
                base.OnClosed(e);
                // Ensure the application exits when the main window is closed.
                // If you rely on application shutdown mode instead, this call is optional.
                try
                {
                    System.Windows.Application.Current?.Shutdown();
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var nr_paragonu = ParagonTextBox.Text;
            try
            {
                if (string.IsNullOrEmpty(nr_paragonu))
                {
                    throw new Exception("Podaj numer paragonu");
                }

                var dokumentySprzedazy = sfera.Value.DokumentySprzedazy();
                var dokumentEncja = dokumentySprzedazy.Dane.Wszystkie().FirstOrDefault(d => string.Compare(d.NumerWewnetrzny.PelnaSygnatura, nr_paragonu, StringComparison.OrdinalIgnoreCase) == 0);
                if (dokumentEncja == null)
                {
                    throw new Exception("Nie znaleziono paragonu o podanym numerze");
                }

                var dokument_id = dokumentEncja.Id;

                FiskalizacjaZakonczonaEventArgs fiskalizacjaZakonczona = null;
                FiskalizacjaZablokowanaEventArgs fiskalizacjaZablokowana = null;

                var fiskalizacja_start = DateTime.Now;
                if (iFiskalizacjaDokumentu == null)
                {
                    iFiskalizacjaDokumentu = sfera.Value.PodajObiektTypu<IFiskalizacjaDokumentu>();

                    iFiskalizacjaDokumentu.ZakonczonoFiskalizacje += ((o, ea) =>
                    {
                        fiskalizacjaZakonczona = ea;
                    });

                    iFiskalizacjaDokumentu.FiskalizacjaZablokowana += ((o, ea) =>
                    {
                        fiskalizacjaZablokowana = ea;
                    });
                }

                iFiskalizacjaDokumentu.Fiskalizuj(dokument_id, id_drukarki, null, false, false);
                while (fiskalizacjaZakonczona == null)
                {
                    Thread.Sleep(1000);
                    var diff = DateTime.Now - fiskalizacja_start;
                    if (diff.TotalSeconds > fiskalizacja_timeout)
                    {
                        throw new TimeoutException($"Fiskalizacja dokumentu nie zmieściła się w limicie czasu ({fiskalizacja_timeout} s)");
                    }

                    if (fiskalizacjaZakonczona?.Wyjatek != null)
                    {
                        throw new Exception($"Fiskalizacja nie powiodła się z powodu błędu.", fiskalizacjaZakonczona.Wyjatek);
                    }

                    if (fiskalizacjaZakonczona?.Wynik.Cancelled == true)
                    {
                        throw new Exception("Fiskalizacja anulowana");
                    }

                    if (fiskalizacjaZakonczona?.Wynik?.Errors?.Any() == true)
                    {
                        throw new Exception("Wystąpiły błędy przy fiskalizacji: " + string.Join("; ", fiskalizacjaZakonczona.Wynik?.Errors?.Select(e => e.GeneralComment) ?? new string[] { }));
                    }

                    if (fiskalizacjaZablokowana != null)
                    {
                        throw new Exception("Fiskalizacja zablokowana.", fiskalizacjaZablokowana.Wyjatek);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
